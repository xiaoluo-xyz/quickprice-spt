using System;
using System.Reflection;
using System.Text;
using EFT.UI.SessionEnd;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using QuickPrice.Utils;

namespace QuickPrice.Patches
{
    /// <summary>
    /// 撤离结算页面文本注入测试补丁
    /// </summary>
    public class SessionResultExitStatusPatch : ModulePatch
    {
        private static readonly FieldInfo RaidTimeField = AccessTools.Field(typeof(SessionResultExitStatus), "_raidTime");
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(
                typeof(SessionResultExitStatus),
                method => method.Name == "Show" && method.GetParameters().Length == 7
            );
        }

        [PatchPostfix]
        public static void Postfix(SessionResultExitStatus __instance)
        {
            try
            {
                if (__instance == null)
                {
                    return;
                }

                var raidTimeText = RaidTimeField?.GetValue(__instance) as TMP_Text;
                if (raidTimeText == null)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(raidTimeText.text) && raidTimeText.text.Contains("本局收获"))
                {
                    return;
                }

                var builder = new StringBuilder();
                if (!string.IsNullOrEmpty(raidTimeText.text))
                {
                    builder.Append(raidTimeText.text);
                    builder.AppendLine();
                }
                builder.Append(RaidSummaryMetrics.BuildSettlementText());
                raidTimeText.text = builder.ToString();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"❌ 结算文本注入失败: {ex.Message}");
            }
        }
    }
}
