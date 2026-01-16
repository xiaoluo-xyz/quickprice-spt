using System;
using System.Linq;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using UnityEngine;
using QuickPrice.Config;
using QuickPrice.Logging;
using QuickPrice.Patches;
using QuickPrice.Services;
using QuickPrice.Extensions;
using QuickPrice.Utils;
using EFT.Communications;
using EFT.UI;
using HarmonyLib;

namespace QuickPrice
{
    [BepInPlugin("com.quickprice.spt", "QuickPrice", BuildInfo.Version)]
    [BepInDependency("com.SPT.custom", "4.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log => Instance.Logger;
        public static EFT.InventoryLogic.Item HoveredItem { get; set; }

        // v2.0: 异步初始化标志
        private static bool _isInitializing = false;
        public static bool IsInitializing => _isInitializing;

        // 价格刷新标志
        private static bool _isRefreshingPrices = false;
        private static DateTime _lastManualRefresh = DateTime.MinValue;
        private static bool _isSyncingServerConfig = false;
        private static DateTime _lastServerConfigSync = DateTime.MinValue;
        private static readonly TimeSpan ServerConfigSyncInterval = TimeSpan.FromMinutes(5);

        private void Awake()
        {
            Instance = this;

            try
            {
                // Log.LogInfo("===========================================");
                // Log.LogInfo("  QuickPrice - Tarkov SPT 4.0.0");
                // Log.LogInfo($"  版本: {BuildInfo.Version}");
                // Log.LogInfo("===========================================");

                // 初始化配置
                Settings.Init(Config);
                // Log.LogInfo("✅ 中文配置系统初始化成功");

                // 预加载搜索音效（仅 .mp3）
                SearchSoundCustomAudio.PreloadAll();

                // 启动异步获取服务端配置（不阻塞游戏启动）
                _ = InitializeServerConfigAsync();

                // v2.0: 启动异步价格数据加载（不阻塞游戏启动）
                _ = InitializePricesAsync();

                // v2.0: 启动异步商人数据检查（不阻塞游戏启动）
                _ = InitializeTradersAsync();

                // 启动异步商人回收价格加载（不阻塞游戏启动）
                _ = InitializeTraderBuybackPricesAsync();

                // v2.0: 启动异步跳蚤禁售物品列表加载（不阻塞游戏启动）
                _ = InitializeRagfairBannedItemsAsync();

                // 注册所有补丁（立即启用）
                EnableAllPatches();
                new Harmony("com.QuickPrice.Patches").PatchAll();
                // Log.LogInfo("===========================================");
                // Log.LogInfo("  🎉 插件启动完成！");
                // Log.LogInfo("  ⏳ 价格数据正在后台加载...");
                // Log.LogInfo("  ⏳ 跳蚤禁售列表正在后台加载...");
                // Log.LogInfo("  🎮 进入游戏查看物品价格");
                // Log.LogInfo("  ⚙️  按 F12 打开配置管理器");
                // Log.LogInfo("===========================================");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"❌ 插件启动失败: {ex.Message}");
                Log.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// v2.0: 异步初始化价格数据
        /// 使用 Fire-and-Forget 模式，不阻塞主线程
        /// </summary>
        private async Task InitializePricesAsync()
        {
            if (_isInitializing)
            {
                // Log.LogDebug("价格数据已在初始化中，跳过");
                return;
            }

            _isInitializing = true;

            try
            {
                // Log.LogInfo("🔄 开始异步加载价格数据...");

                // 使用新增的异步方法
                var success = await PriceDataService.Instance.UpdatePricesAsync();

                if (success)
                {
                    var count = PriceDataService.Instance.GetCachedPriceCount();
                    // Log.LogInfo($"✅ 价格数据异步加载成功: {count} 个物品");
                    // Log.LogDebug($"📊 {PriceDataService.Instance.GetCacheStatus()}");
                }
                else
                {
                    ClientLog.Warning("⚠️ 价格数据加载失败，将在下次使用时重试");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"❌ 异步初始化失败: {ex.Message}");
                // Log.LogDebug(ex.StackTrace);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>
        /// v2.0: 异步检查和等待商人数据加载
        /// 尝试在后台等待商人数据被游戏加载
        /// </summary>
        private async Task InitializeTradersAsync()
        {
            await Task.Delay(3000); // 等待 3 秒让游戏完成初始化

            try
            {
                // Log.LogInfo("🔄 开始检查商人数据...");

                // 使用反射调用 TraderPriceService.GetAllTraders()
                var getAllTradersMethod = TraderPriceService.Instance.GetType()
                    .GetMethod("GetAllTraders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (getAllTradersMethod == null)
                {
                    // Log.LogDebug("⚠️ 无法找到 GetAllTraders 方法");
                    return;
                }

                var tradersObj = getAllTradersMethod.Invoke(TraderPriceService.Instance, null);
                if (tradersObj == null)
                {
                    // Log.LogDebug("⚠️ 商人列表为空或未加载");
                    return;
                }

                var tradersList = new System.Collections.Generic.List<object>();
                foreach (var trader in (System.Collections.IEnumerable)tradersObj)
                {
                    tradersList.Add(trader);
                }

                if (tradersList.Count == 0)
                {
                    // Log.LogDebug("⚠️ 商人列表为空");
                    return;
                }

                int traderCount = tradersList.Count;
                int readyCount = 0;

                // 获取 TraderClass 类型
                var traderType = tradersList[0].GetType();
                var getSupplyDataMethod = traderType.GetMethod("GetSupplyData",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var localizedNameProperty = traderType.GetProperty("LocalizedName");

                foreach (var traderObj in tradersList)
                {
                    if (traderObj == null)
                        continue;

                    // 使用扩展方法 GetSupplyData (通过反射)
                    var supplyDataType = typeof(Extensions.TraderClassExtensions);
                    var getSupplyDataExtension = supplyDataType.GetMethod("GetSupplyData",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    object supplyData = null;
                    if (getSupplyDataExtension != null)
                    {
                        supplyData = getSupplyDataExtension.Invoke(null, new[] { traderObj });
                    }

                    if (supplyData != null)
                    {
                        readyCount++;
                    }
                    else
                    {
                        // string traderName = "Unknown";
                        // if (localizedNameProperty != null)
                        // {
                        //     traderName = localizedNameProperty.GetValue(traderObj)?.ToString() ?? "Unknown";
                        // }
                        // Log.LogDebug("   商人 " + traderName + " 的 SupplyData 未加载");
                    }
                }

                // Log.LogInfo($"✅ 找到 {traderCount} 个商人，{readyCount} 个商人数据已就绪");

                if (readyCount < traderCount)
                {
                    // Log.LogInfo("💡 部分商人数据未加载");
                    // Log.LogInfo("   首次使用商人价格功能时，请先打开任意商人界面");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"❌ 检查商人数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// v2.0: 异步初始化跳蚤禁售物品列表
        /// 在游戏启动时后台加载，不阻塞主线程
        /// </summary>
        private async Task InitializeRagfairBannedItemsAsync()
        {
            try
            {
                // Log.LogInfo("🔄 开始异步加载跳蚤禁售物品列表...");

                // 使用 PriceDataService 的异步方法
                var success = await PriceDataService.Instance.UpdateRagfairBannedItemsAsync();

                if (success)
                {
                    var status = PriceDataService.Instance.GetRagfairBannedCacheStatus();
                    // Log.LogInfo($"✅ 跳蚤禁售列表加载成功: {status}");
                }
                else
                {
                    ClientLog.Warning("⚠️ 跳蚤禁售列表加载失败或为空，默认所有物品可售");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"❌ 加载跳蚤禁售列表失败: {ex.Message}");
                ClientLog.Warning("   所有物品将默认显示跳蚤价格");
            }
        }

        /// <summary>
        /// 异步获取服务端配置并应用客户端覆盖
        /// </summary>
        private async Task InitializeServerConfigAsync()
        {
            _isSyncingServerConfig = true;
            try
            {
                var success = await ServerConfigService.Instance.UpdateServerConfigAsync();
                if (!success)
                {
                    ClientLog.Warning("⚠️ 服务端配置加载失败，继续使用本地配置");
                    return;
                }

                var config = ServerConfigService.Instance.GetConfig();
                if (config == null)
                {
                    ClientLog.Warning("⚠️ 服务端配置为空，继续使用本地配置");
                    return;
                }

                if (Settings.ApplyServerConfigOverrides(config))
                {
                    ClientLog.Debug("✅ 已应用服务端配置覆盖");
                }
                else
                {
                    ClientLog.Debug("ℹ️ 服务端未启用配置覆盖，使用本地配置");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"❌ 获取服务端配置失败: {ex.Message}");
            }
            finally
            {
                _lastServerConfigSync = DateTime.Now;
                _isSyncingServerConfig = false;
            }
        }

        /// <summary>
        /// 异步初始化商人回收价格数据
        /// 在游戏启动时后台加载，不阻塞主线程
        /// </summary>
        private async Task InitializeTraderBuybackPricesAsync()
        {
            try
            {
                var success = await PriceDataService.Instance.UpdateTraderBuybackPricesAsync();

                if (!success)
                {
                    ClientLog.Warning("⚠️ 商人回收价格加载失败，将在使用时重试");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"❌ 加载商人回收价格失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启用所有补丁
        /// </summary>
        private void EnableAllPatches()
        {
            try
            {
                // 注册物品捕获补丁
                new GridItemOnPointerEnterPatch().Enable();
                new GridItemOnPointerExitPatch().Enable();
                // Log.LogInfo("✅ 物品捕获补丁已启用");

                // 注册拖拽状态捕获补丁
                new ItemViewBeginDragPatch().Enable();
                new ItemViewEndDragPatch().Enable();

                // 注册价格显示补丁
                new PriceTooltipPatch().Enable();
                // Log.LogInfo("✅ 价格显示补丁已启用");

                // 注册撤离结算文本注入测试补丁
                new SessionResultExitStatusPatch().Enable();
                // Log.LogInfo("✅ 结算文本注入测试补丁已启用");

                // 注册战局装备价值追踪补丁
                new GameWorldEquipmentTrackerPatch().Enable();
                new GameWorldEquipmentTrackerCleanupPatch().Enable();
                // Log.LogInfo("✅ 装备价值追踪补丁已启用");

                // 注册血条结算信息 UI 补丁
                new RaidSummaryHealthParametersPanelShowPatch().Enable();
                // Log.LogInfo("✅ 血条结算 UI 补丁已启用");

                // 注册自定义颜色转换补丁（必须在背景色补丁之前启用）
                new CustomColorConverterPatch().Enable();
                // Log.LogInfo("✅ 自定义颜色转换补丁已启用");

                // 注册物品背景色自动着色补丁（如果启用）
                if (Settings.EnablePriceBasedBackgroundColor.Value)
                {
                    new ItemBackgroundColorPatch().Enable();
                    // Log.LogInfo("✅ 物品背景色自动着色已启用");
                }
                else
                {
                    // Log.LogInfo("ℹ️  物品背景色自动着色已禁用（可在配置中启用）");
                }

                // v2.0: 注册打开物品栏自动刷新补丁（如果启用）
                if (Settings.AutoRefreshOnOpenInventory.Value)
                {
                    new InventoryScreenShowPatch().Enable();
                    // Log.LogInfo("✅ 自动刷新补丁已启用（打开物品栏时刷新）");
                }
                else
                {
                    // Log.LogInfo("ℹ️  打开物品栏自动刷新已禁用（可在配置中启用）");
                }

                // 注册地面物品名称着色补丁（如果启用物品名称着色）
                if (Settings.ColorItemName.Value && Settings.EnableColorCoding.Value)
                {
                    new LootItemLabelPatch().Enable();
                    // Log.LogInfo("✅ 地面物品名称着色补丁已启用");
                }
                else
                {
                    // Log.LogInfo("ℹ️  地面物品名称着色已禁用（可在配置中启用）");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"❌ 启用补丁失败: {ex.Message}");
                Log.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Unity Update 方法 - 每帧调用
        /// 用于检测快捷键输入
        /// </summary>
        private void Update()
        {
            try
            {
                EquipmentValueTracker.ProcessPending();

                if (!_isSyncingServerConfig &&
                    DateTime.Now - _lastServerConfigSync >= ServerConfigSyncInterval)
                {
                    _ = SyncServerConfigAsync();
                }

                // 检测刷新价格快捷键
                if (Input.GetKeyDown(Settings.RefreshPricesKey.Value))
                {
                    // 启动异步刷新（Fire-and-Forget）
                    _ = RefreshPricesManuallyAsync();
                }

            }
            catch (System.Exception ex)
            {
                Log.LogError($"❌ 快捷键检测失败: {ex.Message}");
            }
        }

        private async Task SyncServerConfigAsync()
        {
            if (_isSyncingServerConfig)
                return;

            _isSyncingServerConfig = true;
            try
            {
                var success = await ServerConfigService.Instance.UpdateServerConfigAsync();
                if (!success)
                {
                    ClientLog.Debug("⚠️ 服务端配置同步失败，将在下次周期重试");
                    return;
                }

                var config = ServerConfigService.Instance.GetConfig();
                if (config == null)
                {
                    ClientLog.Debug("⚠️ 服务端配置为空，跳过同步");
                    return;
                }

                Settings.ApplyServerConfigOverrides(config);
            }
            catch (Exception ex)
            {
                Log.LogError($"❌ 服务端配置同步失败: {ex.Message}");
            }
            finally
            {
                _lastServerConfigSync = DateTime.Now;
                _isSyncingServerConfig = false;
            }
        }

        /// <summary>
        /// 手动刷新价格缓存（快捷键触发）
        /// </summary>
        private async Task RefreshPricesManuallyAsync()
        {
            // 防止重复刷新
            if (_isRefreshingPrices)
            {
                ClientLog.Warning("⚠️ 价格刷新已在进行中，请稍候...");
                // 显示游戏内通知
                NotificationManagerClass.DisplayMessageNotification("QuickPrice: 价格刷新已在进行中...", ENotificationDurationType.Default);
                return;
            }

            // 防抖：距离上次手动刷新至少5秒
            var timeSinceLastRefresh = (DateTime.Now - _lastManualRefresh).TotalSeconds;
            if (timeSinceLastRefresh < 5)
            {
                ClientLog.Warning($"⚠️ 刷新过于频繁，请等待 {5 - (int)timeSinceLastRefresh} 秒后再试");
                // 显示游戏内通知
                NotificationManagerClass.DisplayMessageNotification(
                    $"QuickPrice: 请等待 {5 - (int)timeSinceLastRefresh} 秒后再试",
                    ENotificationDurationType.Default);
                return;
            }

            _isRefreshingPrices = true;
            _lastManualRefresh = DateTime.Now;

            try
            {
                Log.LogInfo("===========================================");
                Log.LogInfo("  🔄 开始手动刷新跳蚤市场价格...");
                Log.LogInfo($"  模式: {(Settings.UseDynamicPrices.Value ? "动态价格" : "静态价格")}");
                Log.LogInfo("===========================================");

                // 显示开始刷新的游戏内通知
                string priceMode = Settings.UseDynamicPrices.Value ? "跳蚤市场" : "静态";
                NotificationManagerClass.DisplayMessageNotification(
                    $"QuickPrice: 正在获取{priceMode}报价...",
                    ENotificationDurationType.Long);

                var startTime = DateTime.Now;

                // 强制刷新价格数据
                var success = await PriceDataService.Instance.UpdatePricesAsync(force: true);

                var duration = (DateTime.Now - startTime).TotalSeconds;

                if (success)
                {
                    var count = PriceDataService.Instance.GetCachedPriceCount();
                    Log.LogInfo("===========================================");
                    Log.LogInfo($"  ✅ 价格刷新成功！");
                    Log.LogInfo($"  📊 物品数量: {count:N0} 个");
                    Log.LogInfo($"  ⏱️  耗时: {duration:F1} 秒");
                    Log.LogInfo($"  📅 更新时间: {DateTime.Now:HH:mm:ss}");
                    Log.LogInfo("===========================================");

                    // 清理地面物品颜色缓存（价格已更新，颜色需要重新计算）
                    try
                    {
                        Patches.LootItemLabelPatch.ClearColorCache();
                        Log.LogInfo("  🎨 地面物品颜色缓存已清理");
                    }
                    catch
                    {
                        // 忽略错误（补丁可能未启用）
                    }

                    Log.LogInfo("  💡 提示: 重新悬停物品即可看到最新价格");
                    Log.LogInfo("===========================================");

                    // 显示成功的游戏内通知
                    NotificationManagerClass.DisplayMessageNotification(
                        $"QuickPrice: 报价同步完成！已更新 {count:N0} 个物品 (耗时 {duration:F1}秒)",
                        ENotificationDurationType.Long);
                }
                else
                {
                    ClientLog.Warning("===========================================");
                    ClientLog.Warning("  ⚠️ 价格刷新失败");
                    ClientLog.Warning("  💡 请检查服务端是否正常运行");
                    ClientLog.Warning("===========================================");

                    // 显示失败的游戏内通知
                    NotificationManagerClass.DisplayMessageNotification(
                        "QuickPrice: 价格刷新失败，请检查服务端",
                        ENotificationDurationType.Default);
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError("===========================================");
                Log.LogError($"  ❌ 价格刷新异常: {ex.Message}");
                Log.LogError("===========================================");

                // 显示异常的游戏内通知
                NotificationManagerClass.DisplayMessageNotification(
                    $"QuickPrice: 刷新异常 - {ex.Message}",
                    ENotificationDurationType.Default);
            }
            finally
            {
                _isRefreshingPrices = false;
            }
        }

        private void OnDestroy()
        {
            // Log.LogInfo("QuickPrice 卸载");
        }
    }
}
