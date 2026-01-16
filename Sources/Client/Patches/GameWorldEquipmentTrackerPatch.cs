using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using QuickPrice.Utils;

namespace QuickPrice.Patches
{
    /// <summary>
    /// 进入战局时绑定装备价值追踪器
    /// </summary>
    public class GameWorldEquipmentTrackerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), "OnGameStarted");
        }

        [PatchPostfix]
        public static void Postfix(GameWorld __instance)
        {
            try
            {
                if (__instance?.MainPlayer == null)
                    return;

                EquipmentValueTracker.Initialize(__instance.MainPlayer);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"❌ 初始化装备追踪失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 退出战局时清理装备价值追踪器
    /// </summary>
    public class GameWorldEquipmentTrackerCleanupPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), "OnDestroy");
        }

        [PatchPrefix]
        public static void Prefix()
        {
            EquipmentValueTracker.FinalizeRaidSummary();
            EquipmentValueTracker.Clear(resetMetrics: false);
        }
    }
}
