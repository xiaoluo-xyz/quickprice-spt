using System.Runtime.CompilerServices;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using QuickPrice.Config;
using QuickPrice.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace QuickPrice.Patches
{
    [HarmonyPatch(typeof(ItemViewStats), nameof(ItemViewStats.SetStaticInfo))]
    internal static class ArmorClassBadgePatch
    {
        private sealed class IconStyle
        {
            public IconStyle(Color color) => Color = color;
            public Color Color { get; }
        }

        private static readonly ConditionalWeakTable<ItemViewStats, IconStyle> Styles =
            new ConditionalWeakTable<ItemViewStats, IconStyle>();

        [HarmonyPostfix]
        private static void Postfix(ItemViewStats __instance, Item item, bool examined, Image ____armorClassIcon)
        {
            var icon = ____armorClassIcon;
            if (icon == null)
                return;

            var style = Styles.GetValue(__instance, _ => new IconStyle(icon.color));
            icon.color = style.Color;
            if (!Settings.EnableColorCoding.Value || !Settings.EnableArmorClassColoring.Value)
                return;

            var originalClass = ArmorHelper.GetOriginalArmorClass(item);
            if (!originalClass.HasValue || originalClass.Value == 0)
                return;

            // Preserve the native suppression of locked inserts and unexamined plates.
            if (item is ArmorPlateItemClass plate && !__instance.method_0(plate))
                return;

            icon.sprite = CacheResourcesPopAbstractClass.Pop<Sprite>(
                $"Mod Types/icon_type_mod_armor_plate_{originalClass.Value}");
            ColorUtility.TryParseHtmlString("#" + ArmorColorCoding.GetColorForArmorClass(originalClass.Value), out var color);
            icon.color = color;
            icon.gameObject.SetActive(examined);
        }
    }

    [HarmonyPatch(typeof(GridItemView), nameof(GridItemView.UpdateInfo))]
    internal static class ArmorProtectionRefreshPatch
    {
        [HarmonyPrefix]
        private static void Prefix(GridItemView __instance, ref Color ___OriginalBackgroundColor,
            ItemViewAnimation ___Animator)
        {
            if (!ArmorHelper.IsArmor(__instance.Item))
                return;

            // GridItemView caches this at creation; update it before native status colors run.
            var color = GClass1409.ToColor(__instance.Item.BackgroundColor);
            color.a = 77f / 255f;
            ___OriginalBackgroundColor = color;
            ___Animator.Init(color);
        }

        [HarmonyPostfix]
        private static void Postfix(GridItemView __instance)
        {
            if (ArmorHelper.IsArmor(__instance.Item) && __instance.ItemViewStats != null)
                __instance.ItemViewStats.SetStaticInfo(__instance.Item, __instance.Examined);
        }
    }
}
