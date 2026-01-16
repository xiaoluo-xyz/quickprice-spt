using System;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using QuickPrice.Services;
using UnityEngine;
using QuickPrice.Config;
using QuickPrice.Utils;
using QuickPrice.Logging;

namespace QuickPrice.Patches
{
    public static class SearchPatch
    {
        private static int? InstantSearchMaxLevel;

        private static int GetMaxUnknownItemPriceLevel(GClass3515 instance)
        {
            int maxLevel = 1;
            foreach (Item item in instance.Item.GetFirstLevelItems())
            {
                if (instance.IplayerSearchController_0.IsItemKnown(item, null))
                    continue;

                int level = SearchSoundPatch.GetItemPriceLevel(item);
                if (level > maxLevel)
                    maxLevel = level;
            }

            return maxLevel;
        }

        /// <summary>
        /// 搜索音效补丁 - 根据物品价格等级播放不同音效
        /// </summary>
        [HarmonyPatch(typeof(GClass3517), "PlayDiscoverSound")]
        public static class SearchSoundPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(GClass3517 __instance, Item item)
            {
                if (!Settings.EnableSearchSound.Value)
                {
                    ClientLog.Debug("🔊 SearchSoundPatch skipped: EnableSearchSound=false");
                    return true;
                }

                try
                {
                    ClientLog.Debug($"🔊 SearchSoundPatch: item={item?.TemplateId ?? "null"}");
                    return !TryPlayCustomSound(item); // 播放成功则跳过原始方法
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"搜索音效播放失败: {ex.Message}");
                    return true; // 出错时使用原始方法
                }
            }

            /// <summary>
            /// 第二个搜索音效补丁 - 确保所有搜索路径都被覆盖
            /// </summary>
            [HarmonyPatch(typeof(SearchContentOperationResultClass), "smethod_5")]
            public static class SearchSoundPatch2
            {
                [HarmonyPrefix]
                public static bool Prefix(SearchContentOperationResultClass __instance, Item item)
                {
                    if (!Settings.EnableSearchSound.Value)
                    {
                        ClientLog.Debug("🔊 SearchSoundPatch2 skipped: EnableSearchSound=false");
                        return true;
                    }

                    try
                    {
                        if (item == null)
                            return true;

                        int priceLevel = SearchSoundPatch.GetItemPriceLevel(item);
                        bool hasCustom = SearchSoundCustomAudio.HasCustomSound(priceLevel);
                        ClientLog.Debug($"🔊 SearchSoundPatch2: item={item.TemplateId}, level={priceLevel}, hasCustom={hasCustom}");
                        return !hasCustom; // 有自定义音效则跳过原始方法
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogError($"搜索音效播放失败: {ex.Message}");
                        return true; // 出错时使用原始方法
                    }
                }
            }

            /// <summary>
            /// 根据价格等级获取对应的音效类型
            /// </summary>
            public static EUISoundType GetSoundTypeByPriceLevel(int priceLevel)
            {
                return priceLevel switch
                {
                    6 => EUISoundType.AchievementCompleted,  // 最高价值 - 成就完成音效
                    5 => EUISoundType.InsuranceInsured,      // 高价值 - 保险音效
                    4 => EUISoundType.MenuInspectorWindowClose, // 较高价值 - 菜单关闭音效
                    3 => EUISoundType.ButtonClick,           // 中等价值 - 按钮点击音效
                    2 => EUISoundType.ButtonOver,            // 低价值 - 按钮悬停音效
                    _ => EUISoundType.ButtonOver             // 最低价值 - 按钮悬停音效
                };
            }

            /// <summary>
            /// 尝试播放自定义搜索音效
            /// </summary>
            public static bool TryPlayCustomSound(Item item)
            {
                if (item == null)
                    return false;

                // 获取物品价格等级
                int priceLevel = GetItemPriceLevel(item);

                // 仅在存在自定义音效时播放；否则交给原版逻辑
                return SearchSoundCustomAudio.TryPlayCustomSound(priceLevel);
            }

            /// <summary>
            /// 根据物品价格计算等级（1-6）
            /// </summary>
            public static int GetItemPriceLevel(Item item)
            {
                var price = PriceDataService.Instance.GetPrice(item.TemplateId);
                if (!price.HasValue) return 1;

                // 计算单格价值
                int slots = item.Width * item.Height;
                double pricePerSlot = slots > 0 ? price.Value / slots : price.Value;

                // 使用与背景色相同的等级划分逻辑
                if (pricePerSlot <= Settings.GetPriceThreshold1()) return 1;
                if (pricePerSlot <= Settings.GetPriceThreshold2()) return 2;
                if (pricePerSlot <= Settings.GetPriceThreshold3()) return 3;
                if (pricePerSlot <= Settings.GetPriceThreshold4()) return 4;
                if (pricePerSlot <= Settings.GetPriceThreshold5()) return 5;
                return 6;
            }
        }

        /// <summary>
        /// 秒搜音效上下文补丁 - 计算本次秒搜的最高价值等级
        /// </summary>
        [HarmonyPatch(typeof(SearchContentOperationResultClass), "ExecuteInternal")]
        public static class InstantSearchContextPatch
        {
            [HarmonyPrefix]
            public static void Prefix(SearchContentOperationResultClass __instance)
            {
                if (!Settings.EnableSearchSound.Value)
                {
                    ClientLog.Debug("🔊 InstantSearchContext skipped: EnableSearchSound=false");
                    return;
                }

                if (!__instance.Bool_0)
                {
                    ClientLog.Debug("🔊 InstantSearchContext skipped: Bool_0=false");
                    return;
                }

                try
                {
                    InstantSearchMaxLevel = GetMaxUnknownItemPriceLevel(__instance);
                    ClientLog.Debug($"🔊 InstantSearchContext: maxLevel={InstantSearchMaxLevel}");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"搜索音效播放失败: {ex.Message}");
                    InstantSearchMaxLevel = null;
                }
            }

            [HarmonyPostfix]
            public static void Postfix(SearchContentOperationResultClass __instance)
            {
                if (__instance.Bool_0)
                    InstantSearchMaxLevel = null;
            }
        }

        /// <summary>
        /// 秒搜音效补丁 - 优先播放自定义音效
        /// </summary>
        [HarmonyPatch(typeof(SearchContentOperationResultClass), "smethod_4")]
        public static class InstantSearchSoundPatch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (!Settings.EnableSearchSound.Value)
                {
                    ClientLog.Debug("🔊 InstantSearchSound skipped: EnableSearchSound=false");
                    return true;
                }

                if (!InstantSearchMaxLevel.HasValue)
                {
                    ClientLog.Debug("🔊 InstantSearchSound skipped: maxLevel missing");
                    return true;
                }

                try
                {
                    int level = InstantSearchMaxLevel.Value;
                    InstantSearchMaxLevel = null;
                    ClientLog.Debug($"🔊 InstantSearchSound: level={level}");
                    return !SearchSoundCustomAudio.TryPlayCustomSound(level);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"搜索音效播放失败: {ex.Message}");
                    return true;
                }
            }
        }

        /// <summary>
        /// 主搜索执行补丁 - 在 method_6 中引入自定义延迟
        /// </summary>
        [HarmonyPatch(typeof(GClass3515), "method_6")]
        public static class MainSearchExecutionPatch
        {
            private static bool IsProcessing = false;

            [HarmonyPrefix]
            public static bool Prefix(ref Task __result, GClass3515 __instance)
            {
                if (!Settings.GetEnableSearchTimeAdjustment())
                    return true;

                if (IsProcessing)
                    return true;

                try
                {
                    IsProcessing = true;
                    __result = ExecuteCustomSearch(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"搜索执行失败: {ex.Message}");
                    IsProcessing = false;
                    return true;
                }
            }

            /// <summary>
            /// 执行自定义搜索逻辑 - 为每个物品单独计算搜索时间
            /// </summary>
            private static async Task ExecuteCustomSearch(GClass3515 __instance)
            {
                try
                {
                    if (!__instance.IplayerSearchController_0.ContainsUnknownItems(__instance.Item))
                        return;

                    if (__instance.Bool_0)
                    {
                        int maxPriceLevel = 1;
                        bool hasUnknownItems = false;

                        Item instantItem;
                        while (GetUnknownItem(__instance, out instantItem))
                        {
                            if (__instance.Boolean_0)
                                return;

                            hasUnknownItems = true;
                            int priceLevel = GetItemPriceLevel(instantItem);
                            if (priceLevel > maxPriceLevel)
                                maxPriceLevel = priceLevel;

                            __instance.DiscoverItem(instantItem);
                        }

                        __instance.IplayerSearchController_0.OnItemFullySearched();

                        if (Settings.EnableSearchSound.Value && hasUnknownItems)
                        {
                            SearchSoundCustomAudio.TryPlayCustomSound(maxPriceLevel);
                        }

                        return;
                    }

                    // 获取技能系数（与原代码相同的计算方式）
                    bool isEquipment = __instance.Item.Parent.GetOwner().RootItem is InventoryEquipment;
                    IInventoryProfileSkillInfo skillsInfo = __instance.Profile_0.SkillsInfo;
                    float skillFactor = isEquipment ?
                        (1f + skillsInfo.AttentionLootSpeedValue + skillsInfo.SearchBuffSpeedValue) :
                        (1f + skillsInfo.AttentionLootSpeedValue);

                    Item unknownItem;
                    while (GetUnknownItem(__instance, out unknownItem))
                    {
                        // 为当前未知物品计算价格等级和搜索时间
                        int priceLevel = GetItemPriceLevel(unknownItem); // 注意：这里使用unknownItem而不是__instance.Item
                        float baseSearchTime = CalculateSearchTime(priceLevel);
                        float actualSearchTime = baseSearchTime / skillFactor;
                        if (actualSearchTime < 0.01f)
                            actualSearchTime = 0.01f;

                        // 使用计算后的延迟（可包含随机范围）
                        try
                        {
                            await Task.Delay((int)(actualSearchTime * 1000f), __instance.CancellationTokenSource_0.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }

                        if (__instance.Boolean_0)
                            return;

                        // 再次检查并发现物品
                        Item currentItem;
                        if (GetUnknownItem(__instance, out currentItem))
                        {
                            __instance.DiscoverItem(currentItem);
                        }
                    }

                    __instance.IplayerSearchController_0.OnItemFullySearched();
                }
                finally
                {
                    IsProcessing = false;
                }
            }

            /// <summary>
            /// 获取未知物品（替换原method_7）
            /// </summary>
            private static bool GetUnknownItem(GClass3515 __instance, out Item unknownItem)
            {
                foreach (Item item in __instance.Item.GetFirstLevelItems())
                {
                    if (!__instance.IplayerSearchController_0.IsItemKnown(item, null))
                    {
                        unknownItem = item;
                        return true;
                    }
                }
                unknownItem = null;
                return false;
            }

            private static float CalculateSearchTime(int priceLevel)
            {
                float baseTime = priceLevel switch
                {
                    1 => Settings.GetSearchTimeLevel1(),
                    2 => Settings.GetSearchTimeLevel2(),
                    3 => Settings.GetSearchTimeLevel3(),
                    4 => Settings.GetSearchTimeLevel4(),
                    5 => Settings.GetSearchTimeLevel5(),
                    6 => Settings.GetSearchTimeLevel6(),
                    _ => Settings.GetSearchTimeLevel1()
                };

                float randomMin = Settings.GetSearchTimeRandomMin();
                float randomMax = Settings.GetSearchTimeRandomMax();

                if (randomMax < randomMin)
                {
                    var temp = randomMin;
                    randomMin = randomMax;
                    randomMax = temp;
                }

                float randomDelay = 0f;
                if (randomMax > 0f || randomMin > 0f)
                {
                    randomDelay = UnityEngine.Random.Range(randomMin, randomMax);
                }

                return baseTime + randomDelay;
            }

            /// <summary>
            /// 发现物品时播放自定义搜索音效（非秒搜）
            /// </summary>
            [HarmonyPatch(typeof(GClass3515), "DiscoverItem")]
            public static class DiscoverItemSoundPatch
            {
                [HarmonyPostfix]
                public static void Postfix(GClass3515 __instance, Item item)
                {
                    if (!Settings.EnableSearchSound.Value)
                    {
                        ClientLog.Debug("🔊 DiscoverItemSound skipped: EnableSearchSound=false");
                        return;
                    }

                    if (__instance.Bool_0)
                    {
                        ClientLog.Debug("🔊 DiscoverItemSound skipped: Bool_0=true");
                        return;
                    }

                    try
                    {
                        ClientLog.Debug($"🔊 DiscoverItemSound: item={item?.TemplateId ?? "null"}");
                        SearchSoundPatch.TryPlayCustomSound(item);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogError($"搜索音效播放失败: {ex.Message}");
                    }
                }
            }

            /// <summary>
            /// 根据物品价格计算等级（1-6）- 复用你已有的逻辑
            /// </summary>
            public static int GetItemPriceLevel(Item item)
            {
                var price = PriceDataService.Instance.GetPrice(item.TemplateId);
                if (!price.HasValue) return 1;

                // 计算单格价值
                int slots = item.Width * item.Height;
                double pricePerSlot = slots > 0 ? price.Value / slots : price.Value;

                // 使用与背景色相同的等级划分逻辑
                if (pricePerSlot <= Settings.GetPriceThreshold1()) return 1;
                if (pricePerSlot <= Settings.GetPriceThreshold2()) return 2;
                if (pricePerSlot <= Settings.GetPriceThreshold3()) return 3;
                if (pricePerSlot <= Settings.GetPriceThreshold4()) return 4;
                if (pricePerSlot <= Settings.GetPriceThreshold5()) return 5;
                return 6;
            }
        }
    }
}
