using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT.InventoryLogic;

namespace QuickPrice.Utils
{
    public static class ArmorHelper
    {
        public static int? GetArmorClass(Item item) => GetProtection(item)?.CurrentClass;
        public static int? GetOriginalArmorClass(Item item) => GetProtection(item)?.OriginalClass;

        public static bool IsArmor(Item item) => item != null &&
            (item.GetItemComponent<ArmorComponent>() != null ||
             item.GetItemComponent<ArmorHolderComponent>() != null);

        private static ArmorProtection? GetProtection(Item item)
        {
            if (!IsArmor(item) || !Singleton<BackendConfigSettingsClass>.Instantiated)
                return null;

            var armorConfig = Singleton<BackendConfigSettingsClass>.Instance.Armor;
            var thresholds = armorConfig.ArmorClass.Select(level => (double)level.Resistance).ToArray();
            var parts = new List<ArmorProtectionPart>();
            ArmorRegion supportedRegions = ArmorRegion.None;

            // A loose plate retains its own rating regardless of the slot it last occupied.
            if (item is ArmorPlateItemClass plate)
            {
                parts.Add(GetPart(plate.Armor, ArmorRegion.None));
            }
            else
            {
                var holder = item.GetItemComponent<ArmorHolderComponent>();
                if (holder != null)
                {
                    foreach (var slot in holder.ArmorSlots)
                        supportedRegions |= GetRegions(slot.ArmorPlateColliderMask, slot.ArmorColliders);
                }

                foreach (var armor in GClass3380.GetItemComponentsInChildren<ArmorComponent>(item))
                {
                    if (armor is CompositeArmorComponent composite &&
                        composite.Togglable != null && !composite.Togglable.On)
                        continue;

                    var regions = GetRegions(armor.ArmorPlateCollidersMask, armor.ArmorColliders);
                    supportedRegions |= regions;
                    parts.Add(GetPart(armor, regions));
                }
            }

            return ArmorProtectionCalculation.Calculate(parts, supportedRegions, thresholds);
        }

        private static ArmorProtectionPart GetPart(ArmorComponent armor, ArmorRegion regions)
        {
            double original = armor.ArmorClass > 0
                ? Singleton<BackendConfigSettingsClass>.Instance.Armor.GetArmorClass(armor.ArmorClass).Resistance
                : 0;
            double current = 0;
            if (original > 0 && armor.Repairable.Durability > 0 && armor.Repairable.TemplateDurability > 0)
            {
                current = GClass659.RealResistance(armor.Repairable.Durability,
                    armor.Repairable.TemplateDurability, armor.ArmorClass, 0).RealResistance;
                current = Math.Min(current, original);
            }

            return new ArmorProtectionPart(regions, current, original);
        }

        internal static ArmorRegion GetRegions(EArmorPlateCollider plates, IEnumerable<EBodyPartColliderType> body)
        {
            ArmorRegion regions = ArmorRegion.None;
            const EArmorPlateCollider front = EArmorPlateCollider.Plate_Granit_SAPI_chest |
                EArmorPlateCollider.Plate_Korund_chest;
            const EArmorPlateCollider back = EArmorPlateCollider.Plate_Granit_SAPI_back |
                EArmorPlateCollider.Plate_6B13_back;
            const EArmorPlateCollider left = EArmorPlateCollider.Plate_Granit_SSAPI_side_left_high |
                EArmorPlateCollider.Plate_Granit_SSAPI_side_left_low |
                EArmorPlateCollider.Plate_Korund_side_left_high |
                EArmorPlateCollider.Plate_Korund_side_left_low;
            const EArmorPlateCollider right = EArmorPlateCollider.Plate_Granit_SSAPI_side_right_high |
                EArmorPlateCollider.Plate_Granit_SSAPI_side_right_low |
                EArmorPlateCollider.Plate_Korund_side_right_high |
                EArmorPlateCollider.Plate_Korund_side_right_low;

            if ((plates & front) != 0) regions |= ArmorRegion.Front;
            if ((plates & back) != 0) regions |= ArmorRegion.Back;
            if ((plates & left) != 0) regions |= ArmorRegion.Left;
            if ((plates & right) != 0) regions |= ArmorRegion.Right;

            foreach (var collider in body)
            {
                switch (collider)
                {
                    case EBodyPartColliderType.RibcageUp:
                    case EBodyPartColliderType.RibcageLow:
                    case EBodyPartColliderType.Pelvis:
                        regions |= ArmorRegion.Front;
                        break;
                    case EBodyPartColliderType.SpineTop:
                    case EBodyPartColliderType.SpineDown:
                    case EBodyPartColliderType.PelvisBack:
                        regions |= ArmorRegion.Back;
                        break;
                    case EBodyPartColliderType.LeftSideChestUp:
                    case EBodyPartColliderType.LeftSideChestDown:
                        regions |= ArmorRegion.Left;
                        break;
                    case EBodyPartColliderType.RightSideChestUp:
                    case EBodyPartColliderType.RightSideChestDown:
                        regions |= ArmorRegion.Right;
                        break;
                }
            }

            return regions;
        }
    }
}
