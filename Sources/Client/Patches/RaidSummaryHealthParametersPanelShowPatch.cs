using System;
using System.Reflection;
using EFT.HealthSystem;
using EFT.UI.Health;
using HarmonyLib;
using SPT.Reflection.Patching;
using QuickPrice.UI;
using QuickPrice.Utils;
using TMPro;

namespace QuickPrice.Patches
{
    /// <summary>
    /// 血条信息面板显示时挂载战局结算信息 UI
    /// </summary>
    public class RaidSummaryHealthParametersPanelShowPatch : ModulePatch
    {
        private static readonly FieldInfo CurrentValueField =
            AccessTools.Field(typeof(HealthParameterPanel), "_currentValue");

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HealthParametersPanel), nameof(HealthParametersPanel.Show));
        }

        [PatchPostfix]
        public static void PatchPostfix(
            HealthParametersPanel __instance,
            HealthParameterPanel ____weight,
            IHealthController ___ihealthController_0)
        {
            try
            {
                if (___ihealthController_0 is not GClass3010 and not HealthControllerClass)
                    return;

                if (__instance == null || ____weight == null)
                    return;

                EquipmentValueTracker.InitializeFromItemUiContext();

                var template = CurrentValueField?.GetValue(____weight) as TMP_Text;
                var overlay = RaidSummaryHealthOverlayComponent.Attach(__instance, template);
                overlay?.Show();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"❌ 绑定血条结算 UI 失败: {ex.Message}");
            }
        }
    }
}
