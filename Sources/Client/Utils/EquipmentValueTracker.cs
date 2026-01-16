using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using QuickPrice.Config;
using QuickPrice.Logging;
using QuickPrice.Services;

namespace QuickPrice.Utils
{
    /// <summary>
    /// 玩家装备价值追踪器
    /// 基于库存/槽位事件实时刷新带入价值（含背包等容器内物品）
    /// </summary>
    public static class EquipmentValueTracker
    {
        private sealed class ItemSnapshot
        {
            public ItemSnapshot(string itemId, double baseUnitPrice, int stackCount, double relativeValue)
            {
                ItemId = itemId;
                BaseUnitPrice = baseUnitPrice;
                StackCount = stackCount;
                RelativeValue = relativeValue;
                BaselineValue = baseUnitPrice * stackCount * relativeValue;
            }

            public string ItemId { get; }
            public double BaseUnitPrice { get; }
            public int StackCount { get; }
            public double RelativeValue { get; }
            public double BaselineValue { get; }
        }

        private sealed class ItemState
        {
            public ItemState(string itemId, Item item, int stackCount, double relativeValue)
            {
                ItemId = itemId;
                Item = item;
                StackCount = stackCount;
                RelativeValue = relativeValue;
            }

            public string ItemId { get; }
            public Item Item { get; }
            public int StackCount { get; }
            public double RelativeValue { get; }
        }

        private enum PricePolicy
        {
            Default,
            TraderOnly
        }

        private readonly struct SlotTraversal
        {
            public SlotTraversal(Slot slot, bool skipRootItem)
            {
                Slot = slot;
                SkipRootItem = skipRootItem;
            }

            public Slot Slot { get; }
            public bool SkipRootItem { get; }
        }

        private static Player _player;
        private static InventoryController _inventoryController;
        private static InventoryEquipment _equipment;
        private static readonly List<Slot> _subscribedSlots = new List<Slot>();
        private static bool _isInitialized;
        private static bool _isInRaid;
        private static bool _raidSnapshotCaptured;
        private static readonly Dictionary<string, ItemSnapshot> _broughtSnapshot = new Dictionary<string, ItemSnapshot>();
        private static readonly HashSet<string> _initialItemIds = new HashSet<string>();

        private static readonly System.Reflection.FieldInfo ItemUiContextInventoryField =
            AccessTools.Field(typeof(ItemUiContext), "inventory_0");
        private static readonly System.Reflection.FieldInfo ItemUiContextEquipmentField =
            AccessTools.Field(typeof(ItemUiContext), "inventoryEquipment_0");
        private static readonly System.Reflection.FieldInfo ItemUiContextControllerField =
            AccessTools.Field(typeof(ItemUiContext), "inventoryController_0");

        public static void Initialize(Player player)
        {
            if (player == null)
                return;

            if (ReferenceEquals(_player, player) && _isInitialized)
                return;

            Unsubscribe();

            _player = player;
            _inventoryController = player.InventoryController;
            _equipment = player.Equipment ?? player.Inventory?.Equipment;
            _isInitialized = true;
            _isInRaid = true;
            _raidSnapshotCaptured = false;
            RaidSummaryMetrics.IsInRaid = true;

            Subscribe();
            RecalculateValues();
        }

        public static void InitializeFromItemUiContext()
        {
            if (_isInRaid && _player != null)
                return;

            var context = ItemUiContext.Instance;
            if (context == null)
                return;

            var equipment = ItemUiContextEquipmentField?.GetValue(context) as InventoryEquipment;
            var inventory = ItemUiContextInventoryField?.GetValue(context) as Inventory;

            if (equipment == null)
            {
                equipment = inventory?.Equipment;
            }

            if (equipment == null)
                return;

            Unsubscribe();

            _player = null;
            _inventoryController = ItemUiContextControllerField?.GetValue(context) as InventoryController;
            _equipment = equipment;
            _isInitialized = true;
            _isInRaid = false;
            _raidSnapshotCaptured = false;
            RaidSummaryMetrics.IsInRaid = false;

            Subscribe();
            RecalculateValues();
        }

        public static void Clear()
        {
            Unsubscribe();
            _player = null;
            _inventoryController = null;
            _equipment = null;
            _isInitialized = false;
            _isInRaid = false;
            _raidSnapshotCaptured = false;
            _broughtSnapshot.Clear();
            _initialItemIds.Clear();
            RaidSummaryMetrics.Reset();
        }

        private static void Subscribe()
        {
            if (_inventoryController != null)
            {
                _inventoryController.AddItemEvent += OnInventoryChanged;
                _inventoryController.RemoveItemEvent += OnInventoryChanged;
                _inventoryController.RefreshItemEvent += OnInventoryChanged;
            }

            var equipment = _equipment;
            if (equipment == null)
                return;

            foreach (var slot in equipment.AllSlots)
            {
                if (slot == null)
                    continue;

                slot.OnAddOrRemoveItem += OnSlotChanged;
                _subscribedSlots.Add(slot);
            }
        }

        private static void Unsubscribe()
        {
            if (_inventoryController != null)
            {
                _inventoryController.AddItemEvent -= OnInventoryChanged;
                _inventoryController.RemoveItemEvent -= OnInventoryChanged;
                _inventoryController.RefreshItemEvent -= OnInventoryChanged;
            }

            foreach (var slot in _subscribedSlots)
            {
                if (slot == null)
                    continue;
                slot.OnAddOrRemoveItem -= OnSlotChanged;
            }
            _subscribedSlots.Clear();
        }

        private static void OnInventoryChanged(object _)
        {
            RecalculateValues();
        }

        private static void OnSlotChanged(Item _)
        {
            RecalculateValues();
        }

        private static void RecalculateValues()
        {
            try
            {
                var equipment = _equipment;
                if (equipment == null)
                    return;

                bool excludeSecure = Settings.ExcludeSecuredContainerFromBroughtValue?.Value ?? true;
                bool excludeKnife = Settings.ExcludeKnifeFromBroughtValue?.Value ?? true;
                bool excludeArmBand = Settings.ExcludeArmBandFromBroughtValue?.Value ?? true;
                bool excludeDogtag = Settings.ExcludeDogtagFromBroughtValue?.Value ?? true;
                bool excludeSpecialSlots = Settings.ExcludeSpecialSlotsFromBroughtValue?.Value ?? true;

                if (!_isInRaid)
                {
                    long broughtValue = CalculateEquipmentValue(
                        equipment,
                        _ => true,
                        excludeSecure,
                        excludeKnife,
                        excludeArmBand,
                        excludeDogtag,
                        excludeSpecialSlots,
                        PricePolicy.Default);
                    RaidSummaryMetrics.BroughtValue = broughtValue;
                    RaidSummaryMetrics.LossValue = 0;
                    RaidSummaryMetrics.LootValue = 0;
                    RaidSummaryMetrics.SettlementValue = 0;
                    return;
                }

                if (!_raidSnapshotCaptured)
                {
                    CaptureRaidSnapshot(
                        equipment,
                        excludeSecure,
                        excludeKnife,
                        excludeArmBand,
                        excludeDogtag,
                        excludeSpecialSlots);
                }

                var currentStates = BuildCurrentItemStates(equipment);
                var secureContainerItemIds = BuildSecureContainerItemIds(equipment);
                long lossValue = CalculateLossValue(currentStates);
                long lootValue = CalculateLootValue(currentStates, secureContainerItemIds);

                RaidSummaryMetrics.LossValue = lossValue;
                RaidSummaryMetrics.LootValue = lootValue;
                RaidSummaryMetrics.SettlementValue = lootValue - lossValue;
            }
            catch (Exception ex)
            {
                ClientLog.Warning($"⚠️ 计算带入价值失败: {ex.Message}");
            }
        }

        private static void CaptureRaidSnapshot(
            InventoryEquipment equipment,
            bool excludeSecureContainer,
            bool excludeKnife,
            bool excludeArmBand,
            bool excludeDogtag,
            bool excludeSpecialSlots)
        {
            _broughtSnapshot.Clear();
            _initialItemIds.Clear();

            var visited = new HashSet<string>();
            foreach (var slot in equipment.AllSlots)
            {
                if (slot?.ContainedItem == null)
                    continue;

                TraverseItemGraph(slot.ContainedItem, visited, item =>
                {
                    if (!string.IsNullOrEmpty(item?.Id))
                    {
                        _initialItemIds.Add(item.Id);
                    }
                });
            }

            visited.Clear();
            foreach (var traversal in EnumerateEquipmentSlots(
                equipment,
                excludeSecureContainer,
                excludeKnife,
                excludeArmBand,
                excludeDogtag,
                excludeSpecialSlots))
            {
                var slot = traversal.Slot;
                if (slot?.ContainedItem == null)
                    continue;

                TraverseItemGraph(slot.ContainedItem, visited, item =>
                {
                    if (item == null || string.IsNullOrEmpty(item.Id))
                        return;

                    if (_broughtSnapshot.ContainsKey(item.Id))
                        return;

                    double? unitPrice = GetItemUnitPrice(item, PricePolicy.Default);
                    int stackCount = GetItemStackCount(item);
                    double relativeValue = GetItemRelativeValue(item);
                    double priceValue = unitPrice ?? 0;
                    _broughtSnapshot[item.Id] = new ItemSnapshot(item.Id, priceValue, stackCount, relativeValue);
                }, includeRoot: !traversal.SkipRootItem);
            }

            RaidSummaryMetrics.BroughtValue = CalculateSnapshotTotal(_broughtSnapshot);
            _raidSnapshotCaptured = true;
        }

        private static Dictionary<string, ItemState> BuildCurrentItemStates(InventoryEquipment equipment)
        {
            var currentStates = new Dictionary<string, ItemState>();
            var visited = new HashSet<string>();

            foreach (var slot in equipment.AllSlots)
            {
                if (slot?.ContainedItem == null)
                    continue;

                TraverseItemGraph(slot.ContainedItem, visited, item =>
                {
                    if (item == null || string.IsNullOrEmpty(item.Id))
                        return;

                    int stackCount = GetItemStackCount(item);
                    double relativeValue = GetItemRelativeValue(item);
                    currentStates[item.Id] = new ItemState(item.Id, item, stackCount, relativeValue);
                });
            }

            return currentStates;
        }

        private static HashSet<string> BuildSecureContainerItemIds(InventoryEquipment equipment)
        {
            var result = new HashSet<string>();
            if (equipment == null)
                return result;

            Slot secureSlot = null;
            try
            {
                secureSlot = equipment.GetSlot(EquipmentSlot.SecuredContainer);
            }
            catch
            {
                secureSlot = null;
            }

            if (secureSlot?.ContainedItem == null)
                return result;

            var visited = new HashSet<string>();
            TraverseItemGraph(secureSlot.ContainedItem, visited, item =>
            {
                if (!string.IsNullOrEmpty(item?.Id))
                {
                    result.Add(item.Id);
                }
            });

            return result;
        }

        private static long CalculateLossValue(Dictionary<string, ItemState> currentStates)
        {
            long total = 0;
            foreach (var snapshot in _broughtSnapshot.Values)
            {
                double baseline = snapshot.BaselineValue;
                if (baseline <= 0)
                    continue;

                if (currentStates.TryGetValue(snapshot.ItemId, out var current))
                {
                    double currentValue = snapshot.BaseUnitPrice * current.StackCount * current.RelativeValue;
                    double delta = baseline - currentValue;
                    if (delta > 0)
                    {
                        total += (long)Math.Round(delta, MidpointRounding.AwayFromZero);
                    }
                }
                else
                {
                    total += (long)Math.Round(baseline, MidpointRounding.AwayFromZero);
                }
            }

            return total;
        }

        private static long CalculateLootValue(Dictionary<string, ItemState> currentStates, HashSet<string> secureContainerItemIds)
        {
            secureContainerItemIds ??= new HashSet<string>();
            long total = 0;
            foreach (var current in currentStates.Values)
            {
                if (string.IsNullOrEmpty(current.ItemId))
                    continue;

                if (_initialItemIds.Contains(current.ItemId))
                    continue;

                bool inSecureContainer = secureContainerItemIds.Contains(current.ItemId);
                var policy = current.Item.SpawnedInSession ? PricePolicy.Default : PricePolicy.TraderOnly;
                if (!current.Item.SpawnedInSession && inSecureContainer)
                {
                    policy = PricePolicy.TraderOnly;
                }
                double? unitPrice = GetItemUnitPrice(current.Item, policy);
                if (!unitPrice.HasValue || unitPrice.Value <= 0)
                    continue;

                double value = unitPrice.Value * current.StackCount * current.RelativeValue;
                total += (long)Math.Round(value, MidpointRounding.AwayFromZero);
            }

            return total;
        }

        private static long CalculateEquipmentValue(
            InventoryEquipment equipment,
            System.Func<Item, bool> includeItem,
            bool excludeSecureContainer,
            bool excludeKnife,
            bool excludeArmBand,
            bool excludeDogtag,
            bool excludeSpecialSlots,
            PricePolicy pricePolicy)
        {
            var visited = new HashSet<string>();
            long total = 0;

            foreach (var traversal in EnumerateEquipmentSlots(
                equipment,
                excludeSecureContainer,
                excludeKnife,
                excludeArmBand,
                excludeDogtag,
                excludeSpecialSlots))
            {
                var slot = traversal.Slot;
                if (slot?.ContainedItem == null)
                    continue;

                TraverseItemGraph(slot.ContainedItem, visited, item =>
                {
                    if (item == null)
                        return;

                    if (includeItem != null && !includeItem(item))
                        return;

                    total += GetItemValue(item, pricePolicy);
                }, includeRoot: !traversal.SkipRootItem);
            }

            return total;
        }

        private static long GetItemValue(Item item, PricePolicy pricePolicy)
        {
            if (item == null)
                return 0;

            double? unitPrice = GetItemUnitPrice(item, pricePolicy);
            if (!unitPrice.HasValue || unitPrice.Value <= 0)
                return 0;

            int stackCount = GetItemStackCount(item);
            double relativeValue = GetItemRelativeValue(item);
            double total = unitPrice.Value * stackCount * relativeValue;
            return (long)Math.Round(total, MidpointRounding.AwayFromZero);
        }

        private static double? GetItemUnitPrice(Item item, PricePolicy policy)
        {
            if (item == null)
                return null;

            double? fleaPrice = PriceDataService.Instance.GetPrice(item.TemplateId);
            double? traderPrice = TraderPriceService.Instance.GetBestTraderPrice(item)?.PriceInRoubles;

            if (policy == PricePolicy.TraderOnly)
            {
                return traderPrice ?? fleaPrice;
            }

            bool? canSell = RagfairHelper.CanSellOnRagfair(item);
            if (canSell.HasValue)
            {
                return canSell.Value ? (fleaPrice ?? traderPrice) : (traderPrice ?? fleaPrice);
            }

            return fleaPrice ?? traderPrice;
        }

        private static int GetItemStackCount(Item item)
        {
            if (item == null)
                return 1;

            return item.StackObjectsCount > 0 ? item.StackObjectsCount : 1;
        }

        private static double GetItemRelativeValue(Item item)
        {
            if (item == null)
                return 1.0;

            if (TryGetRelativeValue(item.GetItemComponent<RepairableComponent>(), out var relative))
                return ClampRelativeValue(relative);
            if (TryGetRelativeValue(item.GetItemComponent<ResourceComponent>(), out relative))
                return ClampRelativeValue(relative);
            if (TryGetRelativeValue(item.GetItemComponent<MedKitComponent>(), out relative))
                return ClampRelativeValue(relative);
            if (TryGetRelativeValue(item.GetItemComponent<FoodDrinkComponent>(), out relative))
                return ClampRelativeValue(relative);
            if (TryGetRelativeValue(item.GetItemComponent<KeyComponent>(), out relative))
                return ClampRelativeValue(relative);
            if (TryGetRelativeValue(item.GetItemComponent<RepairKitComponent>(), out relative))
                return ClampRelativeValue(relative);

            return 1.0;
        }

        private static bool TryGetRelativeValue(object component, out double relativeValue)
        {
            relativeValue = 1.0;
            if (component == null)
                return false;

            var type = component.GetType();
            var relativeProperty = type.GetProperty("RelativeValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (relativeProperty != null)
            {
                if (TryConvertNumber(relativeProperty.GetValue(component), out relativeValue))
                {
                    return true;
                }
            }

            if (TryGetRatio(component, "Durability", "MaxDurability", out relativeValue))
                return true;
            if (TryGetRatio(component, "Value", "MaxResource", out relativeValue))
                return true;
            if (TryGetRatio(component, "HpResource", "MaxHpResource", out relativeValue))
                return true;
            if (TryGetRatio(component, "Resource", "MaxResource", out relativeValue))
                return true;
            if (TryGetRatio(component, "NumberOfUsages", "MaxNumberOfUsages", out relativeValue))
                return true;
            if (TryGetRatio(component, "NumberOfUsages", "MaxUsages", out relativeValue))
                return true;

            var hpPercentProperty = type.GetProperty("HpPercent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (hpPercentProperty != null && TryConvertNumber(hpPercentProperty.GetValue(component), out relativeValue))
            {
                if (relativeValue > 1)
                {
                    relativeValue /= 100.0;
                }
                return true;
            }

            return false;
        }

        private static bool TryGetRatio(object component, string currentPropertyName, string maxPropertyName, out double ratio)
        {
            ratio = 1.0;
            if (component == null)
                return false;

            var type = component.GetType();
            var currentProperty = type.GetProperty(currentPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var maxProperty = type.GetProperty(maxPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (currentProperty == null || maxProperty == null)
                return false;

            if (!TryConvertNumber(currentProperty.GetValue(component), out var current))
                return false;
            if (!TryConvertNumber(maxProperty.GetValue(component), out var max))
                return false;

            if (max <= 0)
                return false;

            ratio = current / max;
            return true;
        }

        private static bool TryConvertNumber(object value, out double result)
        {
            result = 0;
            if (value == null)
                return false;

            switch (value)
            {
                case float floatValue:
                    result = floatValue;
                    return true;
                case double doubleValue:
                    result = doubleValue;
                    return true;
                case int intValue:
                    result = intValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return true;
                case short shortValue:
                    result = shortValue;
                    return true;
                case byte byteValue:
                    result = byteValue;
                    return true;
                case decimal decimalValue:
                    result = (double)decimalValue;
                    return true;
                default:
                    return double.TryParse(value.ToString(), out result);
            }
        }

        private static double ClampRelativeValue(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1.0;

            if (value < 0)
                return 0;
            if (value > 1)
                return 1;
            return value;
        }

        private static long CalculateSnapshotTotal(Dictionary<string, ItemSnapshot> snapshot)
        {
            long total = 0;
            foreach (var item in snapshot.Values)
            {
                if (item.BaselineValue <= 0)
                    continue;

                total += (long)Math.Round(item.BaselineValue, MidpointRounding.AwayFromZero);
            }

            return total;
        }

        private static void TraverseItemGraph(Item item, HashSet<string> visited, Action<Item> action, bool includeRoot = true)
        {
            if (item == null)
                return;

            string itemId = item.Id;
            if (!string.IsNullOrEmpty(itemId) && !visited.Add(itemId))
                return;

            if (includeRoot)
            {
                action?.Invoke(item);
            }

            if (item is Weapon weapon)
            {
                if (weapon.Mods != null)
                {
                    foreach (var mod in weapon.Mods)
                    {
                        TraverseItemGraph(mod, visited, action);
                    }
                }
            }
            else if (item is Mod mod)
            {
                if (mod.Slots != null)
                {
                    foreach (var slot in mod.Slots)
                    {
                        if (slot?.ContainedItem is Mod childMod)
                        {
                            TraverseItemGraph(childMod, visited, action);
                        }
                    }
                }
            }

            foreach (var child in GetContainerItems(item))
            {
                TraverseItemGraph(child, visited, action);
            }

            if (item is MagazineItemClass magazine)
            {
                if (magazine.Cartridges?.Items != null)
                {
                    foreach (var cartridge in magazine.Cartridges.Items)
                    {
                        TraverseItemGraph(cartridge, visited, action);
                    }
                }
            }
            else if (item is AmmoBox ammoBox)
            {
                if (ammoBox.Cartridges?.Items != null)
                {
                    foreach (var cartridge in ammoBox.Cartridges.Items)
                    {
                        TraverseItemGraph(cartridge, visited, action);
                    }
                }
            }
        }

        private static IEnumerable<Item> GetContainerItems(Item item)
        {
            if (item == null)
                yield break;

            var itemType = item.GetType();

            var isContainerProperty = itemType.GetProperty("IsContainer");
            if (isContainerProperty != null)
            {
                var isContainer = isContainerProperty.GetValue(item);
                if (!(isContainer is bool boolValue && boolValue))
                    yield break;
            }

            string[] gridNames = { "Grids", "Grid", "Containers", "Container" };
            System.Reflection.PropertyInfo gridsProperty = null;
            foreach (var name in gridNames)
            {
                gridsProperty = itemType.GetProperty(name);
                if (gridsProperty != null)
                    break;
            }

            if (gridsProperty == null)
                yield break;

            var grids = gridsProperty.GetValue(item) as System.Collections.IEnumerable;
            if (grids == null)
                yield break;

            foreach (var grid in grids)
            {
                if (grid == null)
                    continue;

                var itemsProperty = grid.GetType().GetProperty("Items");
                if (itemsProperty == null)
                    continue;

                var items = itemsProperty.GetValue(grid) as System.Collections.IEnumerable;
                if (items == null)
                    continue;

                foreach (var gridItem in items.Cast<object>())
                {
                    if (gridItem is Item child)
                        yield return child;
                }
            }
        }

        private static IEnumerable<SlotTraversal> EnumerateEquipmentSlots(
            InventoryEquipment equipment,
            bool excludeSecureContainer,
            bool excludeKnife,
            bool excludeArmBand,
            bool excludeDogtag,
            bool excludeSpecialSlots)
        {
            if (equipment == null)
                yield break;

            var excludedSlots = new HashSet<Slot>();
            Slot secureSlot = null;

            if (excludeSecureContainer)
                secureSlot = GetSlotSafe(equipment, EquipmentSlot.SecuredContainer);
            if (excludeKnife)
                AddSlot(excludedSlots, equipment, EquipmentSlot.Scabbard);
            if (excludeArmBand)
                AddSlot(excludedSlots, equipment, EquipmentSlot.ArmBand);
            if (excludeDogtag)
                AddSlot(excludedSlots, equipment, EquipmentSlot.Dogtag);

            foreach (var slot in equipment.AllSlots)
            {
                if (slot == null)
                    continue;
                if (excludeSpecialSlots && IsSpecialSlot(slot))
                    continue;
                if (excludedSlots.Contains(slot))
                    continue;

                bool skipRootItem = secureSlot != null && ReferenceEquals(slot, secureSlot);
                yield return new SlotTraversal(slot, skipRootItem);
            }
        }

        private static void AddSlot(HashSet<Slot> slots, InventoryEquipment equipment, EquipmentSlot slotType)
        {
            try
            {
                var slot = equipment.GetSlot(slotType);
                if (slot != null)
                {
                    slots.Add(slot);
                }
            }
            catch
            {
                // ignore missing slots
            }
        }

        private static Slot GetSlotSafe(InventoryEquipment equipment, EquipmentSlot slotType)
        {
            try
            {
                return equipment.GetSlot(slotType);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSpecialSlot(Slot slot)
        {
            if (slot == null)
                return false;

            if (slot.IsSpecial)
                return true;

            var slotId = slot.ID;
            return !string.IsNullOrEmpty(slotId) &&
                   slotId.IndexOf("SpecialSlot", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
