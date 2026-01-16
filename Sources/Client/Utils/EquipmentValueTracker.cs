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
using UnityEngine;

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
            public ItemSnapshot(
                string itemId,
                double baseUnitPrice,
                int stackCount,
                double relativeValue,
                double? baselineDurability,
                double? maxDurability,
                double? durabilityCostPerPoint)
            {
                ItemId = itemId;
                BaseUnitPrice = baseUnitPrice;
                StackCount = stackCount;
                RelativeValue = relativeValue;
                BaselineValue = baseUnitPrice * stackCount * relativeValue;
                BaselineDurability = baselineDurability;
                MaxDurability = maxDurability;
                DurabilityCostPerPoint = durabilityCostPerPoint;
            }

            public string ItemId { get; }
            public double BaseUnitPrice { get; }
            public int StackCount { get; }
            public double RelativeValue { get; }
            public double BaselineValue { get; }
            public double? BaselineDurability { get; }
            public double? MaxDurability { get; }
            public double? DurabilityCostPerPoint { get; }
        }

        private sealed class StackableSnapshot
        {
            public StackableSnapshot(string templateId, double unitPrice, int baselineCount)
            {
                TemplateId = templateId;
                UnitPrice = unitPrice;
                BaselineCount = baselineCount;
            }

            public string TemplateId { get; }
            public double UnitPrice { get; set; }
            public int BaselineCount { get; set; }
        }

        private sealed class ItemState
        {
            public ItemState(string itemId, Item item, int stackCount, double relativeValue, double? currentDurability)
            {
                ItemId = itemId;
                Item = item;
                StackCount = stackCount;
                RelativeValue = relativeValue;
                CurrentDurability = currentDurability;
            }

            public string ItemId { get; }
            public Item Item { get; }
            public int StackCount { get; }
            public double RelativeValue { get; }
            public double? CurrentDurability { get; }
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
        private static readonly Dictionary<string, StackableSnapshot> _broughtStackableSnapshot = new Dictionary<string, StackableSnapshot>();
        private static readonly Dictionary<string, StackableSnapshot> _broughtSecureStackableSnapshot = new Dictionary<string, StackableSnapshot>();
        private static readonly HashSet<string> _initialItemIds = new HashSet<string>();
        private static readonly Dictionary<string, ItemState> _lastKnownStates = new Dictionary<string, ItemState>();
        private static readonly Dictionary<string, int> _lastKnownStackableCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> _lastKnownProtectedStackableCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> _lastKnownSecureStackableCounts = new Dictionary<string, int>();
        private static readonly HashSet<string> _lastKnownProtectedItemIds = new HashSet<string>();
        private static readonly HashSet<string> _lastKnownSecureContainerItemIds = new HashSet<string>();
        private static bool _pendingRecalculate;
        private static float _lastRecalculateTime;
        private const float RecalculateDebounceSeconds = 0.2f;
        private static bool _isUiDragging;

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
            RaidSummaryMetrics.HasRaidSummary = false;

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
            Clear(true);
        }

        public static void Clear(bool resetMetrics)
        {
            Unsubscribe();
            _player = null;
            _inventoryController = null;
            _equipment = null;
            _isInitialized = false;
            _isInRaid = false;
            if (resetMetrics)
            {
                _raidSnapshotCaptured = false;
            }
            _pendingRecalculate = false;
            _lastRecalculateTime = 0f;
            _isUiDragging = false;
            if (resetMetrics)
            {
                ClearSnapshots();
                RaidSummaryMetrics.Reset();
            }
        }

        public static void FinalizeRaidSummary()
        {
            if (!_isInitialized || !_isInRaid)
                return;

            RecalculateValues();
        }

        public static void PrepareForExitStatus()
        {
            if (!_raidSnapshotCaptured)
                return;

            _pendingRecalculate = false;
            if (_isInitialized && _isInRaid)
            {
                RecalculateValues();
            }
        }

        public static void ApplyExitStatus(ExitStatus exitStatus)
        {
            if (!_raidSnapshotCaptured)
                return;

            var currentStates = _lastKnownStates;
            if (currentStates == null || currentStates.Count == 0)
            {
                if (_equipment != null)
                {
                    currentStates = BuildCurrentItemStates(_equipment);
                }
            }

            if (currentStates == null || currentStates.Count == 0)
                return;

            var secureContainerItemIds = _lastKnownSecureContainerItemIds;
            var protectedItemIds = _lastKnownProtectedItemIds;
            var stackableCounts = _lastKnownStackableCounts;
            var secureStackableCounts = _lastKnownSecureStackableCounts;
            var protectedStackableCounts = _lastKnownProtectedStackableCounts;

            if (_equipment != null)
            {
                if (secureContainerItemIds == null || secureContainerItemIds.Count == 0)
                {
                    secureContainerItemIds = BuildSecureContainerItemIds(_equipment);
                }

                if (protectedItemIds == null || protectedItemIds.Count == 0)
                {
                    protectedItemIds = BuildProtectedItemIds(_equipment);
                }
            }

            if (stackableCounts == null || stackableCounts.Count == 0)
            {
                stackableCounts = BuildCurrentStackableCounts(currentStates);
            }

            if (secureStackableCounts == null || secureStackableCounts.Count == 0)
            {
                secureStackableCounts = BuildStackableCountsForItemIds(currentStates, secureContainerItemIds);
            }

            if (protectedStackableCounts == null || protectedStackableCounts.Count == 0)
            {
                protectedStackableCounts = BuildStackableCountsForItemIds(currentStates, protectedItemIds);
            }

            bool isDeathOutcome = IsDeathExitStatus(exitStatus);
            long lossValue = CalculateLossValue(
                currentStates,
                stackableCounts,
                isDeathOutcome,
                protectedItemIds,
                protectedStackableCounts);

            long lootValue = CalculateLootValue(
                currentStates,
                secureContainerItemIds,
                isDeathOutcome ? secureStackableCounts : stackableCounts,
                isDeathOutcome);

            RaidSummaryMetrics.LossValue = lossValue;
            RaidSummaryMetrics.LootValue = lootValue;
            RaidSummaryMetrics.SettlementValue = lootValue - lossValue;
            RaidSummaryMetrics.HasRaidSummary = true;
        }

        public static void ClearRaidSnapshots()
        {
            ClearSnapshots();
        }

        private static void ClearSnapshots()
        {
            _broughtSnapshot.Clear();
            _broughtStackableSnapshot.Clear();
            _broughtSecureStackableSnapshot.Clear();
            _initialItemIds.Clear();
            _lastKnownStates.Clear();
            _lastKnownStackableCounts.Clear();
            _lastKnownProtectedStackableCounts.Clear();
            _lastKnownSecureStackableCounts.Clear();
            _lastKnownProtectedItemIds.Clear();
            _lastKnownSecureContainerItemIds.Clear();
        }

        private static bool IsDeathExitStatus(ExitStatus exitStatus)
        {
            return exitStatus != ExitStatus.Survived &&
                   exitStatus != ExitStatus.Runner &&
                   exitStatus != ExitStatus.Transit;
        }

        private static void Subscribe()
        {
            if (_inventoryController != null)
            {
                _inventoryController.AddItemEvent += OnInventoryChanged;
                _inventoryController.RemoveItemEvent += OnInventoryChanged;
                _inventoryController.RefreshItemEvent += OnInventoryChanged;
                _inventoryController.OnAmmoLoaded += OnAmmoChanged;
                _inventoryController.OnAmmoUnloaded += OnAmmoChanged;
                _inventoryController.OnProfileUpdate += OnProfileUpdated;
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
                _inventoryController.OnAmmoLoaded -= OnAmmoChanged;
                _inventoryController.OnAmmoUnloaded -= OnAmmoChanged;
                _inventoryController.OnProfileUpdate -= OnProfileUpdated;
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
            RequestRecalculate();
        }

        private static void OnSlotChanged(Item _)
        {
            RequestRecalculate();
        }

        private static void OnAmmoChanged(int _)
        {
            RequestRecalculate();
        }

        private static void OnProfileUpdated()
        {
            RequestRecalculate();
        }

        public static void ProcessPending()
        {
            if (!_pendingRecalculate)
                return;

            if (IsItemDragInProgress())
                return;

            float now = Time.realtimeSinceStartup;
            if (now - _lastRecalculateTime < RecalculateDebounceSeconds)
                return;

            _pendingRecalculate = false;
            _lastRecalculateTime = now;
            RecalculateValues();
        }

        private static void RequestRecalculate()
        {
            _pendingRecalculate = true;
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
                    if (!RaidSummaryMetrics.HasRaidSummary)
                    {
                        RaidSummaryMetrics.LossValue = 0;
                        RaidSummaryMetrics.LootValue = 0;
                        RaidSummaryMetrics.SettlementValue = 0;
                    }
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
                var protectedItemIds = BuildProtectedItemIds(equipment);
                var stackableCounts = BuildCurrentStackableCounts(currentStates);
                var secureStackableCounts = BuildStackableCountsForItemIds(currentStates, secureContainerItemIds);
                var protectedStackableCounts = BuildStackableCountsForItemIds(currentStates, protectedItemIds);
                UpdateLastKnownSnapshots(
                    currentStates,
                    stackableCounts,
                    secureStackableCounts,
                    protectedStackableCounts,
                    secureContainerItemIds,
                    protectedItemIds);
                long lossValue = CalculateLossValue(currentStates, stackableCounts, false, protectedItemIds, protectedStackableCounts);
                long lootValue = CalculateLootValue(currentStates, secureContainerItemIds, stackableCounts, false);

                RaidSummaryMetrics.LossValue = lossValue;
                RaidSummaryMetrics.LootValue = lootValue;
                RaidSummaryMetrics.SettlementValue = lootValue - lossValue;
                RaidSummaryMetrics.HasRaidSummary = true;
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
            ClearSnapshots();

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

                    var policy = GetRaidSummaryPricePolicy(item, isDeathOutcome: false, inSecureContainer: false);
                    double? unitPrice = GetItemUnitPrice(item, policy);
                    int stackCount = GetItemStackCount(item);
                    double relativeValue = GetItemRelativeValue(item);
                    double priceValue = unitPrice ?? 0;
                    double? baselineDurability = null;
                    double? maxDurability = null;
                    double? durabilityCostPerPoint = null;

                    if (IsStackableItem(item))
                    {
                        AddStackableSnapshot(item, priceValue, stackCount);
                        return;
                    }

                    if (ShouldTrackDurabilityLoss(item))
                    {
                        if (TryGetRepairableDurability(item, out var currentDurability, out var maxDurabilityValue))
                        {
                            baselineDurability = currentDurability;
                            maxDurability = maxDurabilityValue;
                            if (!TryGetRepairCostPerPoint(item, out var costPerPoint) &&
                                maxDurabilityValue > 0)
                            {
                                durabilityCostPerPoint = ApplyRepairCostModifiers(priceValue / maxDurabilityValue);
                            }
                            else if (costPerPoint > 0)
                            {
                                durabilityCostPerPoint = costPerPoint;
                            }
                        }
                    }

                    _broughtSnapshot[item.Id] = new ItemSnapshot(
                        item.Id,
                        priceValue,
                        stackCount,
                        relativeValue,
                        baselineDurability,
                        maxDurability,
                        durabilityCostPerPoint);
                }, includeRoot: !traversal.SkipRootItem);
            }

            CaptureSecureContainerStackableSnapshot(equipment);

            foreach (var slot in equipment.AllSlots)
            {
                if (slot?.ContainedItem is Weapon weapon)
                {
                    AddWeaponShellTemplateSnapshot(weapon);
                }
            }

            RaidSummaryMetrics.BroughtValue = CalculateSnapshotTotal(_broughtSnapshot) +
                                              CalculateStackableSnapshotTotal(_broughtStackableSnapshot);
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
                    double? currentDurability = null;
                    if (ShouldTrackDurabilityLoss(item) &&
                        TryGetRepairableDurability(item, out var durabilityValue, out _))
                    {
                        currentDurability = durabilityValue;
                    }

                    currentStates[item.Id] = new ItemState(item.Id, item, stackCount, relativeValue, currentDurability);
                });
            }

            return currentStates;
        }

        private static Dictionary<string, int> BuildCurrentStackableCounts(Dictionary<string, ItemState> currentStates)
        {
            var totals = new Dictionary<string, int>(StringComparer.Ordinal);
            if (currentStates == null || currentStates.Count == 0)
                return totals;

            foreach (var state in currentStates.Values)
            {
                var item = state.Item;
                if (item == null)
                    continue;

                if (!IsStackableItem(item))
                    continue;

                if (IsSpentAmmo(item))
                    continue;

                var templateId = item.TemplateId;
                if (string.IsNullOrEmpty(templateId))
                    continue;

                totals.TryGetValue(templateId, out var count);
                totals[templateId] = count + Math.Max(1, state.StackCount);
            }

            foreach (var state in currentStates.Values)
            {
                if (state?.Item is Weapon weapon)
                {
                    AddWeaponShellTemplateCounts(totals, weapon);
                }
            }

            return totals;
        }

        private static Dictionary<string, int> BuildStackableCountsForItemIds(
            Dictionary<string, ItemState> currentStates,
            HashSet<string> itemIds)
        {
            var totals = new Dictionary<string, int>(StringComparer.Ordinal);
            if (currentStates == null || currentStates.Count == 0 || itemIds == null || itemIds.Count == 0)
                return totals;

            foreach (var state in currentStates.Values)
            {
                if (state?.Item == null)
                    continue;

                if (string.IsNullOrEmpty(state.ItemId) || !itemIds.Contains(state.ItemId))
                    continue;

                if (!IsStackableItem(state.Item))
                    continue;

                if (IsSpentAmmo(state.Item))
                    continue;

                var templateId = state.Item.TemplateId;
                if (string.IsNullOrEmpty(templateId))
                    continue;

                totals.TryGetValue(templateId, out var count);
                totals[templateId] = count + Math.Max(1, state.StackCount);
            }

            foreach (var state in currentStates.Values)
            {
                if (state?.Item is Weapon weapon && itemIds.Contains(state.ItemId))
                {
                    AddWeaponShellTemplateCounts(totals, weapon);
                }
            }

            return totals;
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

        private static void CaptureSecureContainerStackableSnapshot(InventoryEquipment equipment)
        {
            _broughtSecureStackableSnapshot.Clear();

            if (equipment == null)
                return;

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
                return;

            var visited = new HashSet<string>();
            TraverseItemGraph(secureSlot.ContainedItem, visited, item =>
            {
                if (item == null)
                    return;

                if (IsStackableItem(item))
                {
                    var policy = GetRaidSummaryPricePolicy(item, isDeathOutcome: false, inSecureContainer: true);
                    double? unitPrice = GetItemUnitPrice(item, policy);
                    int stackCount = GetItemStackCount(item);
                    AddStackableSnapshot(_broughtSecureStackableSnapshot, item, unitPrice ?? 0, stackCount);
                }

                if (item is Weapon weapon)
                {
                    AddWeaponShellTemplateSnapshot(_broughtSecureStackableSnapshot, weapon);
                }
            });
        }

        private static HashSet<string> BuildProtectedItemIds(InventoryEquipment equipment)
        {
            var result = new HashSet<string>();
            if (equipment == null)
                return result;

            var secureSlot = GetSlotSafe(equipment, EquipmentSlot.SecuredContainer);
            var armBandSlot = GetSlotSafe(equipment, EquipmentSlot.ArmBand);
            var scabbardSlot = GetSlotSafe(equipment, EquipmentSlot.Scabbard);
            var visited = new HashSet<string>();

            foreach (var slot in equipment.AllSlots)
            {
                if (slot?.ContainedItem == null)
                    continue;

                bool isProtected =
                    (secureSlot != null && ReferenceEquals(slot, secureSlot)) ||
                    (armBandSlot != null && ReferenceEquals(slot, armBandSlot)) ||
                    (scabbardSlot != null && ReferenceEquals(slot, scabbardSlot)) ||
                    IsSpecialSlot(slot);

                if (!isProtected)
                    continue;

                TraverseItemGraph(slot.ContainedItem, visited, item =>
                {
                    if (!string.IsNullOrEmpty(item?.Id))
                    {
                        result.Add(item.Id);
                    }
                });
            }

            return result;
        }

        private static void UpdateLastKnownSnapshots(
            Dictionary<string, ItemState> currentStates,
            Dictionary<string, int> stackableCounts,
            Dictionary<string, int> secureStackableCounts,
            Dictionary<string, int> protectedStackableCounts,
            HashSet<string> secureContainerItemIds,
            HashSet<string> protectedItemIds)
        {
            if (currentStates == null || currentStates.Count == 0)
                return;

            if (ShouldSkipSnapshotUpdate(secureContainerItemIds))
                return;

            CopyDictionary(_lastKnownStates, currentStates);
            CopyDictionary(_lastKnownStackableCounts, stackableCounts);
            CopyDictionary(_lastKnownSecureStackableCounts, secureStackableCounts);
            CopyDictionary(_lastKnownProtectedStackableCounts, protectedStackableCounts);
            CopyHashSet(_lastKnownSecureContainerItemIds, secureContainerItemIds);
            CopyHashSet(_lastKnownProtectedItemIds, protectedItemIds);
        }

        private static bool ShouldSkipSnapshotUpdate(HashSet<string> secureContainerItemIds)
        {
            if (!_isInRaid || !_raidSnapshotCaptured)
                return false;

            if (_lastKnownSecureContainerItemIds.Count == 0)
                return false;

            return secureContainerItemIds == null || secureContainerItemIds.Count == 0;
        }

        private static long CalculateLossValue(
            Dictionary<string, ItemState> currentStates,
            Dictionary<string, int> stackableCounts,
            bool isDeathOutcome,
            HashSet<string> protectedItemIds,
            Dictionary<string, int> protectedStackableCounts)
        {
            if (isDeathOutcome)
            {
                return CalculateDeathLossValue(currentStates, protectedItemIds, protectedStackableCounts);
            }

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

                    if (snapshot.DurabilityCostPerPoint.HasValue &&
                        snapshot.BaselineDurability.HasValue &&
                        current.CurrentDurability.HasValue)
                    {
                        double durabilityDelta = snapshot.BaselineDurability.Value - current.CurrentDurability.Value;
                        if (durabilityDelta > 0)
                        {
                            double durabilityLoss = durabilityDelta * snapshot.DurabilityCostPerPoint.Value;
                            if (durabilityLoss > 0)
                            {
                                total += (long)Math.Round(durabilityLoss, MidpointRounding.AwayFromZero);
                            }
                        }
                    }
                }
                else
                {
                    total += (long)Math.Round(baseline, MidpointRounding.AwayFromZero);
                }
            }

            total += CalculateStackableLoss(stackableCounts);
            return total;
        }

        private static long CalculateDeathLossValue(
            Dictionary<string, ItemState> currentStates,
            HashSet<string> protectedItemIds,
            Dictionary<string, int> protectedStackableCounts)
        {
            long total = 0;
            protectedItemIds ??= new HashSet<string>();

            foreach (var snapshot in _broughtSnapshot.Values)
            {
                double baseline = snapshot.BaselineValue;
                if (baseline <= 0)
                    continue;

                bool isProtected = protectedItemIds.Contains(snapshot.ItemId);
                if (isProtected && currentStates != null &&
                    currentStates.TryGetValue(snapshot.ItemId, out var current))
                {
                    double currentValue = snapshot.BaseUnitPrice * current.StackCount * current.RelativeValue;
                    double delta = baseline - currentValue;
                    if (delta > 0)
                    {
                        total += (long)Math.Round(delta, MidpointRounding.AwayFromZero);
                    }

                    if (snapshot.DurabilityCostPerPoint.HasValue &&
                        snapshot.BaselineDurability.HasValue &&
                        current.CurrentDurability.HasValue)
                    {
                        double durabilityDelta = snapshot.BaselineDurability.Value - current.CurrentDurability.Value;
                        if (durabilityDelta > 0)
                        {
                            double durabilityLoss = durabilityDelta * snapshot.DurabilityCostPerPoint.Value;
                            if (durabilityLoss > 0)
                            {
                                total += (long)Math.Round(durabilityLoss, MidpointRounding.AwayFromZero);
                            }
                        }
                    }
                }
                else
                {
                    total += (long)Math.Round(baseline, MidpointRounding.AwayFromZero);
                }
            }

            total += CalculateStackableLoss(protectedStackableCounts);
            return total;
        }

        private static long CalculateLootValue(
            Dictionary<string, ItemState> currentStates,
            HashSet<string> secureContainerItemIds,
            Dictionary<string, int> stackableCounts,
            bool isDeathOutcome)
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
                if (isDeathOutcome && !inSecureContainer)
                    continue;

                if (IsStackableItem(current.Item))
                    continue;

                var policy = GetRaidSummaryPricePolicy(current.Item, isDeathOutcome, inSecureContainer);
                double? unitPrice = GetItemUnitPrice(current.Item, policy);
                if (!unitPrice.HasValue || unitPrice.Value <= 0)
                    continue;

                double value = unitPrice.Value * current.StackCount * current.RelativeValue;
                total += (long)Math.Round(value, MidpointRounding.AwayFromZero);
            }

            total += CalculateStackableLoot(
                currentStates,
                secureContainerItemIds,
                stackableCounts,
                isDeathOutcome ? _broughtSecureStackableSnapshot : _broughtStackableSnapshot,
                isDeathOutcome,
                isDeathOutcome);
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

            if (ShouldUseRepairableRelativeValue(item) &&
                TryGetRelativeValue(item.GetItemComponent<RepairableComponent>(), out var relative))
            {
                return ClampRelativeValue(relative);
            }
            if (TryGetRelativeValue(item.GetItemComponent<ResourceComponent>(), out relative))
                return ClampRelativeValue(relative);
            if (TryGetRelativeValue(item.GetItemComponent<MedKitComponent>(), out relative))
                return ClampRelativeValue(relative);
            if (TryGetRelativeValue(item.GetItemComponent<FoodDrinkComponent>(), out relative))
                return ClampRelativeValue(relative);
            if (TryGetRelativeValue(item.GetItemComponent<KeyComponent>(), out relative))
            {
                var keyRelative = ClampRelativeValue(relative);
                if (TryGetRelativeValueFromItemUpd(item, out var updRelative))
                {
                    updRelative = ClampRelativeValue(updRelative);
                    return Math.Min(keyRelative, updRelative);
                }
                return keyRelative;
            }
            if (TryGetRelativeValue(item.GetItemComponent<RepairKitComponent>(), out relative))
                return ClampRelativeValue(relative);

            if (TryGetRelativeValueFromItemUpd(item, out relative))
                return ClampRelativeValue(relative);

            return 1.0;
        }

        private static bool ShouldUseRepairableRelativeValue(Item item)
        {
            if (item == null)
                return false;

            if (item is Weapon)
                return false;

            if (IsArmorItem(item))
                return false;

            return true;
        }

        private static bool ShouldTrackDurabilityLoss(Item item)
        {
            if (item == null)
                return false;

            if (item is Weapon)
                return Settings.IncludeWeaponDurabilityLoss?.Value ?? true;

            if (IsArmorItem(item))
                return Settings.IncludeArmorDurabilityLoss?.Value ?? true;

            return false;
        }

        private static bool IsArmorItem(Item item)
        {
            if (item == null)
                return false;

            if (ArmorHelper.IsArmor(item))
                return true;

            var typeName = item.GetType().Name;
            return typeName == "ArmorPlateItemClass" ||
                   typeName == "BuiltInInsertsItemClass" ||
                   typeName.Contains("Plate") ||
                   typeName.Contains("plate") ||
                   typeName.Contains("Insert") ||
                   typeName.Contains("insert");
        }

        private static bool TryGetRepairableDurability(Item item, out double current, out double max)
        {
            current = 0;
            max = 0;
            if (item == null)
                return false;

            var repairable = item.GetItemComponent<RepairableComponent>();
            if (repairable == null)
                return false;

            if (!TryGetNumberProperty(repairable, "Durability", out current))
                return false;

            if (!TryGetNumberProperty(repairable, "MaxDurability", out max))
                return false;

            return max > 0;
        }

        private static bool TryGetRepairCostPerPoint(Item item, out double costPerPoint)
        {
            costPerPoint = 0;
            if (item == null)
                return false;

            var repairable = item.GetItemComponent<RepairableComponent>();
            if (repairable != null && TryGetNumberProperty(repairable, "RepairCost", out var baseCostPerPoint))
            {
                costPerPoint = ApplyRepairCostModifiers(baseCostPerPoint);
                return costPerPoint > 0;
            }

            var template = GetTemplateObject(repairable) ?? GetTemplateObject(item);
            if (template != null)
            {
                if (TryGetNumberProperty(template, "RepairCost", out baseCostPerPoint))
                {
                    costPerPoint = ApplyRepairCostModifiers(baseCostPerPoint);
                    return costPerPoint > 0;
                }

                var repairableTemplate = GetNestedTemplate(template, "Repairable");
                if (repairableTemplate != null && TryGetNumberProperty(repairableTemplate, "RepairCost", out baseCostPerPoint))
                {
                    costPerPoint = ApplyRepairCostModifiers(baseCostPerPoint);
                    return costPerPoint > 0;
                }
            }

            return false;
        }

        private static double ApplyRepairCostModifiers(double baseCostPerPoint)
        {
            if (baseCostPerPoint <= 0)
                return baseCostPerPoint;

            double multiplier = Settings.RepairCostPriceMultiplier?.Value ?? 1f;
            double coefficientPercent = Settings.RepairPriceCoefficientPercent?.Value ?? 0f;
            double coefficientMultiplier = coefficientPercent <= 0 ? 1.0 : (coefficientPercent / 100.0 + 1.0);

            return baseCostPerPoint * multiplier * coefficientMultiplier;
        }

        private static object GetNestedTemplate(object template, string propertyName)
        {
            if (template == null || string.IsNullOrEmpty(propertyName))
                return null;

            try
            {
                var prop = template.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(template);
            }
            catch
            {
                return null;
            }
        }

        private static object GetTemplateObject(object instance)
        {
            if (instance == null)
                return null;

            try
            {
                var prop = instance.GetType().GetProperty("Template", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(instance);
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetNumberProperty(object instance, string propertyName, out double value)
        {
            value = 0;
            if (instance == null || string.IsNullOrEmpty(propertyName))
                return false;

            try
            {
                var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop == null)
                    return false;

                return TryConvertNumber(prop.GetValue(instance), out value);
            }
            catch
            {
                return false;
            }
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

        private static long CalculateStackableSnapshotTotal(Dictionary<string, StackableSnapshot> snapshot)
        {
            long total = 0;
            foreach (var item in snapshot.Values)
            {
                if (item.UnitPrice <= 0 || item.BaselineCount <= 0)
                    continue;

                double value = item.UnitPrice * item.BaselineCount;
                if (value > 0)
                {
                    total += (long)Math.Round(value, MidpointRounding.AwayFromZero);
                }
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

                foreach (var chamberItem in GetWeaponChamberItems(weapon))
                {
                    TraverseItemGraph(chamberItem, visited, action);
                }
            }
            else if (item is Mod mod)
            {
                if (mod.Slots != null)
                {
                    foreach (var slot in mod.Slots)
                    {
                        // Slots can contain non-mod items (e.g., keychain keys).
                        var slotItem = slot?.ContainedItem;
                        if (slotItem != null)
                        {
                            TraverseItemGraph(slotItem, visited, action);
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
                if (ShouldTraverseAmmoBoxCartridges(ammoBox) && ammoBox.Cartridges?.Items != null)
                {
                    foreach (var cartridge in ammoBox.Cartridges.Items)
                    {
                        TraverseItemGraph(cartridge, visited, action);
                    }
                }
            }
        }

        private static bool ShouldTraverseAmmoBoxCartridges(AmmoBox ammoBox)
        {
            if (ammoBox == null)
                return false;

            return !_isInRaid;
        }

        private static IEnumerable<Item> GetWeaponChamberItems(Weapon weapon)
        {
            if (weapon == null)
                yield break;

            Slot[] chambers = null;
            try
            {
                chambers = weapon.Chambers;
            }
            catch
            {
                chambers = null;
            }

            if (chambers == null || chambers.Length == 0)
                yield break;

            foreach (var chamberSlot in chambers)
            {
                if (chamberSlot?.ContainedItem != null)
                    yield return chamberSlot.ContainedItem;
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
            Slot armBandSlot = null;

            if (excludeSecureContainer)
                secureSlot = GetSlotSafe(equipment, EquipmentSlot.SecuredContainer);
            if (excludeKnife)
                AddSlot(excludedSlots, equipment, EquipmentSlot.Scabbard);
            if (excludeArmBand)
                armBandSlot = GetSlotSafe(equipment, EquipmentSlot.ArmBand);
            if (excludeDogtag)
                AddSlot(excludedSlots, equipment, EquipmentSlot.Dogtag);

            foreach (var slot in equipment.AllSlots)
            {
                if (slot == null)
                    continue;
                bool isSecureSlot = secureSlot != null && ReferenceEquals(slot, secureSlot);
                bool isArmBandSlot = armBandSlot != null && ReferenceEquals(slot, armBandSlot);
                if (excludeSpecialSlots && IsSpecialSlot(slot) && !isSecureSlot && !isArmBandSlot)
                    continue;
                if (excludedSlots.Contains(slot))
                    continue;

                bool skipRootItem = isSecureSlot || isArmBandSlot;
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

        private static void AddStackableSnapshot(Item item, double unitPrice, int stackCount)
        {
            AddStackableSnapshot(_broughtStackableSnapshot, item, unitPrice, stackCount);
        }

        private static void AddStackableSnapshot(string templateId, double unitPrice, int stackCount)
        {
            AddStackableSnapshot(_broughtStackableSnapshot, templateId, unitPrice, stackCount);
        }

        private static void AddStackableSnapshot(
            Dictionary<string, StackableSnapshot> snapshotMap,
            Item item,
            double unitPrice,
            int stackCount)
        {
            if (snapshotMap == null || item == null)
                return;

            if (IsSpentAmmo(item))
                return;

            var templateId = item.TemplateId;
            if (string.IsNullOrEmpty(templateId))
                return;

            AddStackableSnapshot(snapshotMap, templateId, unitPrice, stackCount);
        }

        private static void AddStackableSnapshot(
            Dictionary<string, StackableSnapshot> snapshotMap,
            string templateId,
            double unitPrice,
            int stackCount)
        {
            if (snapshotMap == null || string.IsNullOrEmpty(templateId))
                return;

            if (!snapshotMap.TryGetValue(templateId, out var snapshot))
            {
                snapshot = new StackableSnapshot(templateId, unitPrice, Math.Max(1, stackCount));
                snapshotMap[templateId] = snapshot;
                return;
            }

            snapshot.BaselineCount += Math.Max(1, stackCount);
            if (snapshot.UnitPrice <= 0 && unitPrice > 0)
                snapshot.UnitPrice = unitPrice;
        }

        private static long CalculateStackableLoss(Dictionary<string, int> stackableCounts)
        {
            if (_broughtStackableSnapshot.Count == 0)
                return 0;

            long total = 0;
            foreach (var snapshot in _broughtStackableSnapshot.Values)
            {
                if (snapshot.UnitPrice <= 0 || snapshot.BaselineCount <= 0)
                    continue;

                int currentCount = 0;
                if (stackableCounts != null)
                {
                    stackableCounts.TryGetValue(snapshot.TemplateId, out currentCount);
                }
                int delta = snapshot.BaselineCount - currentCount;
                if (delta <= 0)
                    continue;

                double value = snapshot.UnitPrice * delta;
                if (value > 0)
                    total += (long)Math.Round(value, MidpointRounding.AwayFromZero);
            }

            return total;
        }

        private static long CalculateStackableLoot(
            Dictionary<string, ItemState> currentStates,
            HashSet<string> secureContainerItemIds,
            Dictionary<string, int> stackableCounts,
            Dictionary<string, StackableSnapshot> baselineSnapshot,
            bool restrictToSecureContainer,
            bool forceTraderOnly)
        {
            if (currentStates == null || currentStates.Count == 0 || stackableCounts == null)
                return 0;

            var remaining = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var kvp in stackableCounts)
            {
                baselineSnapshot ??= _broughtStackableSnapshot;
                baselineSnapshot.TryGetValue(kvp.Key, out var baseline);
                int baselineCount = baseline?.BaselineCount ?? 0;
                int delta = kvp.Value - baselineCount;
                if (delta > 0)
                    remaining[kvp.Key] = delta;
            }

            if (remaining.Count == 0)
                return 0;

            long total = 0;
            secureContainerItemIds ??= new HashSet<string>();

            var stackableStates = currentStates.Values
                .Where(state => state?.Item != null && IsStackableItem(state.Item))
                .Where(state => !restrictToSecureContainer || secureContainerItemIds.Contains(state.ItemId))
                .OrderByDescending(state => !_initialItemIds.Contains(state.ItemId))
                .ToList();

            foreach (var current in stackableStates)
            {
                if (string.IsNullOrEmpty(current.ItemId))
                    continue;

                var item = current.Item;
                if (item == null)
                    continue;

                var templateId = item.TemplateId;
                if (string.IsNullOrEmpty(templateId))
                    continue;

                if (!remaining.TryGetValue(templateId, out var remainingCount) || remainingCount <= 0)
                    continue;

                int takeCount = Math.Min(Math.Max(1, current.StackCount), remainingCount);
                bool inSecureContainer = secureContainerItemIds.Contains(current.ItemId);
                var policy = GetRaidSummaryPricePolicy(item, forceTraderOnly, inSecureContainer);

                double? unitPrice = GetItemUnitPrice(item, policy);
                if (unitPrice.HasValue && unitPrice.Value > 0)
                {
                    double value = unitPrice.Value * takeCount * current.RelativeValue;
                    total += (long)Math.Round(value, MidpointRounding.AwayFromZero);
                }

                remaining[templateId] = remainingCount - takeCount;
            }

            return total;
        }

        private static bool IsStackableItem(Item item)
        {
            if (item == null)
                return false;

            if (item is AmmoItemClass)
                return true;

            if (TryGetStackMaxCount(item, out var maxCount) && maxCount > 1)
                return true;

            return item.StackObjectsCount > 1;
        }

        private static bool IsSpentAmmo(Item item)
        {
            if (item is AmmoItemClass ammo)
                return ammo.IsUsed;

            return false;
        }

        private static bool TryGetStackMaxCount(Item item, out int maxCount)
        {
            maxCount = 0;
            if (item == null)
                return false;

            if (TryGetNumberProperty(item, "StackMaxCount", out var value) ||
                TryGetNumberProperty(item, "StackMaxSize", out value) ||
                TryGetNumberProperty(item, "StackMax", out value) ||
                TryGetNumberProperty(item, "MaxStackCount", out value))
            {
                maxCount = (int)Math.Round(value, MidpointRounding.AwayFromZero);
                return maxCount > 0;
            }

            var template = GetTemplateObject(item);
            if (template != null)
            {
                if (TryGetNumberProperty(template, "StackMaxCount", out value) ||
                    TryGetNumberProperty(template, "StackMaxSize", out value) ||
                    TryGetNumberProperty(template, "StackMax", out value) ||
                    TryGetNumberProperty(template, "MaxStackCount", out value))
                {
                    maxCount = (int)Math.Round(value, MidpointRounding.AwayFromZero);
                    return maxCount > 0;
                }
            }

            return false;
        }

        private static bool TryGetRelativeValueFromItemUpd(Item item, out double relativeValue)
        {
            relativeValue = 1.0;
            if (item == null)
                return false;

            var upd = GetUpdateObject(item);
            if (upd == null)
                return false;

            var template = GetTemplateObject(item);

            if (TryGetRatioFromUpdateAndTemplate(
                    upd,
                    template,
                    "Key",
                    new[] { "NumberOfUsages", "NumberOfUses", "Uses" },
                    "Key",
                    new[] { "MaxNumberOfUsages", "MaximumNumberOfUsages", "MaxUsages", "MaximumUsages" },
                    out relativeValue))
            {
                return true;
            }

            if (TryGetRatioFromUpdateAndTemplate(
                    upd,
                    template,
                    "KeyCard",
                    new[] { "NumberOfUsages", "NumberOfUses", "Uses" },
                    "KeyCard",
                    new[] { "MaxNumberOfUsages", "MaximumNumberOfUsages", "MaxUsages", "MaximumUsages" },
                    out relativeValue))
            {
                return true;
            }

            if (TryGetRatioFromUpdateAndTemplate(
                    upd,
                    template,
                    "Keycard",
                    new[] { "NumberOfUsages", "NumberOfUses", "Uses" },
                    "Keycard",
                    new[] { "MaxNumberOfUsages", "MaximumNumberOfUsages", "MaxUsages", "MaximumUsages" },
                    out relativeValue))
            {
                return true;
            }

            return false;
        }

        public static void SetUiDragging(bool isDragging)
        {
            _isUiDragging = isDragging;
        }

        private static bool TryGetRatioFromUpdateAndTemplate(
            object upd,
            object template,
            string updSectionName,
            string[] currentPropertyNames,
            string templateSectionName,
            string[] maxPropertyNames,
            out double ratio)
        {
            ratio = 1.0;
            if (upd == null || string.IsNullOrEmpty(updSectionName))
                return false;

            var updSection = GetNestedTemplate(upd, updSectionName);
            if (updSection == null)
                return false;

            if (!TryGetNumberPropertyAny(updSection, currentPropertyNames, out var current))
                return false;

            object templateSection = null;
            if (template != null)
            {
                templateSection = GetNestedTemplate(template, templateSectionName) ?? template;
            }

            if (templateSection == null || !TryGetNumberPropertyAny(templateSection, maxPropertyNames, out var max))
                return false;

            if (max <= 0)
                return false;

            ratio = current / max;
            return true;
        }

        private static bool TryGetNumberPropertyAny(object instance, string[] propertyNames, out double value)
        {
            value = 0;
            if (instance == null || propertyNames == null)
                return false;

            foreach (var name in propertyNames)
            {
                if (string.IsNullOrEmpty(name))
                    continue;

                if (TryGetNumberProperty(instance, name, out value))
                    return true;
            }

            value = 0;
            return false;
        }

        private static object GetUpdateObject(Item item)
        {
            if (item == null)
                return null;

            try
            {
                var prop = item.GetType().GetProperty("Upd", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(item);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsItemDragInProgress()
        {
            if (_isUiDragging)
                return true;

            var context = ItemUiContext.Instance;
            if (context == null)
                return false;

            var boolMembers = new[]
            {
                "IsDragging",
                "Dragging",
                "IsDragInProgress",
                "IsItemDragging"
            };

            foreach (var name in boolMembers)
            {
                if (TryGetMemberValue(context, name, out var raw) && raw is bool flag)
                {
                    if (flag)
                        return true;
                }
            }

            var referenceMembers = new[]
            {
                "DraggedItem",
                "DraggedItemView",
                "CurrentDraggedItem",
                "CurrentDraggedItemView",
                "DraggedItemContext",
                "DragItem",
                "DragItemView",
                "DraggingItemView"
            };

            foreach (var name in referenceMembers)
            {
                if (TryGetMemberValue(context, name, out var raw) && raw != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddWeaponShellTemplateCounts(Dictionary<string, int> totals, Weapon weapon)
        {
            if (totals == null || weapon == null)
                return;

            AmmoTemplate[] shells = null;
            try
            {
                shells = weapon.ShellsInChambers;
            }
            catch
            {
                shells = null;
            }

            if (shells == null || shells.Length == 0)
                return;

            foreach (var shell in shells)
            {
                if (shell == null)
                    continue;

                var templateId = shell.StringId;
                if (string.IsNullOrEmpty(templateId))
                    continue;

                totals.TryGetValue(templateId, out var count);
                totals[templateId] = count + 1;
            }
        }

        private static void AddWeaponShellTemplateSnapshot(Weapon weapon)
        {
            AddWeaponShellTemplateSnapshot(_broughtStackableSnapshot, weapon);
        }

        private static void AddWeaponShellTemplateSnapshot(
            Dictionary<string, StackableSnapshot> snapshotMap,
            Weapon weapon)
        {
            if (snapshotMap == null || weapon == null)
                return;

            AmmoTemplate[] shells = null;
            try
            {
                shells = weapon.ShellsInChambers;
            }
            catch
            {
                shells = null;
            }

            if (shells == null || shells.Length == 0)
                return;

            foreach (var shell in shells)
            {
                if (shell == null)
                    continue;

                var templateId = shell.StringId;
                if (string.IsNullOrEmpty(templateId))
                    continue;

                double unitPrice = GetTemplateUnitPrice(templateId);
                AddStackableSnapshot(snapshotMap, templateId, unitPrice, 1);
            }
        }

        private static PricePolicy GetRaidSummaryPricePolicy(
            Item item,
            bool isDeathOutcome,
            bool inSecureContainer)
        {
            if (item == null)
                return PricePolicy.Default;

            if (isDeathOutcome)
                return PricePolicy.TraderOnly;

            bool useTraderForNonFir = Settings.UseTraderPriceForNonFirInRaidSummary?.Value ?? true;
            if (!item.SpawnedInSession && useTraderForNonFir)
                return PricePolicy.TraderOnly;

            if (!item.SpawnedInSession && inSecureContainer && useTraderForNonFir)
                return PricePolicy.TraderOnly;

            return PricePolicy.Default;
        }

        private static double GetTemplateUnitPrice(string templateId)
        {
            if (string.IsNullOrEmpty(templateId))
                return 0;

            double? fleaPrice = PriceDataService.Instance.GetPrice(templateId);
            if (fleaPrice.HasValue && fleaPrice.Value > 0)
                return fleaPrice.Value;

            return 0;
        }

        private static void CopyDictionary<TKey, TValue>(
            Dictionary<TKey, TValue> target,
            Dictionary<TKey, TValue> source)
        {
            target.Clear();
            if (source == null)
                return;

            foreach (var kvp in source)
            {
                target[kvp.Key] = kvp.Value;
            }
        }

        private static void CopyHashSet(HashSet<string> target, HashSet<string> source)
        {
            target.Clear();
            if (source == null)
                return;

            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private static bool TryGetMemberValue(object instance, string memberName, out object value)
        {
            value = null;
            if (instance == null || string.IsNullOrEmpty(memberName))
                return false;

            try
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var prop = instance.GetType().GetProperty(memberName, flags);
                if (prop != null)
                {
                    value = prop.GetValue(instance);
                    return true;
                }

                var field = instance.GetType().GetField(memberName, flags);
                if (field != null)
                {
                    value = field.GetValue(instance);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
