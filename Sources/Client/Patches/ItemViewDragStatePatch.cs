using System.Reflection;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using QuickPrice.Utils;

namespace QuickPrice.Patches
{
    /// <summary>
    /// 物品拖拽状态捕获（避免拖拽中误判损耗）
    /// </summary>
    public class ItemViewBeginDragPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemView), "OnBeginDrag");
        }

        [PatchPostfix]
        public static void Postfix(ItemView __instance)
        {
            if (__instance != null && __instance.BeingDragged)
            {
                EquipmentValueTracker.SetUiDragging(true);
            }
        }
    }

    public class ItemViewEndDragPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemView), "OnEndDrag");
        }

        [PatchPostfix]
        public static void Postfix()
        {
            EquipmentValueTracker.SetUiDragging(false);
        }
    }
}
