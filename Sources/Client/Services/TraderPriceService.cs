using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using QuickPrice.Models;
using QuickPrice.Extensions;
using QuickPrice.Logging;

namespace QuickPrice.Services
{
    /// <summary>
    /// 商人价格服务
    /// 负责获取商人收购价格
    /// v2.1: 优化容器克隆性能，添加价格缓存
    /// </summary>
    public class TraderPriceService
    {
        private static TraderPriceService _instance;
        public static TraderPriceService Instance => _instance ??= new TraderPriceService();

        private bool _hasShownInitTip = false;  // 是否已显示初始化提示
        private static readonly MongoID RoubleCurrencyId = new MongoID("5449016a4bdc2d6f028b456f");

        // ===== 性能优化：缓存系统 =====

        /// <summary>
        /// 商人价格缓存（按物品TemplateId缓存）
        /// 因为同一物品的商人价格是固定的，无需重复计算
        /// </summary>
        private Dictionary<string, TraderPrice> _priceCache = new Dictionary<string, TraderPrice>();

        /// <summary>
        /// 已确认「无任何商人收购」的物品缓存（防止每次悬停都重复计算）
        /// 仅在商人数据已就绪且遍历后仍无人收购时才会写入；
        /// 服务端回收价表 miss 时会先尝试本地计算，该表不会遮挡服务端数据
        /// </summary>
        private readonly HashSet<string> _noPriceCache = new HashSet<string>();

        /// <summary>
        /// 已记录过「服务端回收价表缺失」日志的物品（防止日志刷屏）
        /// </summary>
        private readonly HashSet<string> _serverMissLogged = new HashSet<string>();

        /// <summary>
        /// 反射属性缓存（避免重复反射）
        /// Key: 物品类型 (Type), Value: IsContainer 属性信息
        /// </summary>
        private static Dictionary<Type, PropertyInfo> _containerPropertyCache = new Dictionary<Type, PropertyInfo>();

        private TraderPriceService() { }

        /// <summary>
        /// 获取最佳商人收购价格
        /// </summary>
        /// <param name="item">物品</param>
        /// <returns>最高收购价格，如果没有商人收购则返回 null</returns>
        public TraderPrice GetBestTraderPrice(Item item)
        {
            try
            {
                // ===== 优化1: 先查缓存 =====
                string cacheKey = item.TemplateId;

                // 服务端回收价表优先（数据完整时最准确、性能最好）
                if (TryGetServerTraderPrice(cacheKey, out var serverPrice, out var serverReady))
                {
                    return serverPrice;
                }

                // 服务端表已就绪但缺少该物品时不再直接返回 null（常见于模组配件：
                // 服务端缓存构建早于模组数据注入），改为回退到本地商人实时计算，
                // 避免模组物品错误地显示跳蚤价格。
                // 本地计算结果会写入 _priceCache，且服务端表刷新后仍优先命中服务端数据。

                if (serverReady && _serverMissLogged.Add(cacheKey))
                {
                    ClientLog.Debug($"⚠️ 服务端回收价表缺少 {item.LocalizedName()} ({cacheKey})，回退本地商人计算");
                }

                if (_priceCache.TryGetValue(cacheKey, out var cachedPrice))
                {
                    // Plugin.Log.LogDebug($"💾 命中缓存: {item.LocalizedName()} = {cachedPrice.PriceInRoubles:N0}₽");
                    return cachedPrice;
                }

                // 已确认无人收购（且商人数据曾就绪），直接返回
                if (_noPriceCache.Contains(cacheKey))
                {
                    return null;
                }

                TraderPrice highestPrice = null;

                // 获取所有商人
                var traders = GetAllTraders();
                if (traders == null || !traders.Any())
                {
                    if (!_hasShownInitTip)
                    {
                        // Plugin.Log.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        // Plugin.Log.LogInfo("💡 首次使用商人价格功能");
                        // Plugin.Log.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        // Plugin.Log.LogInfo("   请打开任意商人界面（如 Prapor、Therapist）");
                        // Plugin.Log.LogInfo("   然后关闭界面，商人价格功能即可正常使用");
                        // Plugin.Log.LogInfo("   💡 此步骤每次游戏启动只需执行一次");
                        // Plugin.Log.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        _hasShownInitTip = true;
                    }
                    return null;
                }

                // 遍历所有商人
                int availableTraders = 0;
                foreach (TraderClass trader in traders)
                {
                    // 检查商人是否可用
                    if (!IsTraderAvailable(trader))
                        continue;

                    availableTraders++;
                    try
                    {
                        Item itemToPrice;

                        // ===== 优化2: 容器检测，避免深拷贝 =====
                        // 商人只关心容器本体价格，不关心内部物品
                        // 大容器克隆会复制所有内部物品，造成严重性能问题
                        if (IsContainer(item))
                        {
                            // 容器：直接使用原物品
                            // ✅ 避免克隆100+个物品，性能提升100倍
                            itemToPrice = item;
                            // Plugin.Log.LogDebug($"🚀 容器优化: {item.LocalizedName()} - 跳过克隆");
                        }
                        else
                        {
                            // 非容器：克隆并设置数量为1（获取单价）
                            itemToPrice = item.CloneItem();
                            itemToPrice.StackObjectsCount = 1;
                        }

                        // 获取商人收购价格
                        var priceStruct = trader.GetUserItemPrice(itemToPrice);
                        if (!priceStruct.HasValue)
                            continue;

                        // 获取价格金额和货币ID
                        int amount = priceStruct.Value.Amount;
                        MongoID? currencyIdNullable = priceStruct.Value.CurrencyId;

                        // 如果货币ID为null，跳过此商人
                        if (!currencyIdNullable.HasValue)
                            continue;

                        MongoID currencyId = currencyIdNullable.Value;

                        // 获取货币汇率
                        double currencyCourse = GetCurrencyCourse(trader, currencyId);

                        // 计算卢布价格（用于对比）
                        double priceInRoubles = amount * currencyCourse;

                        // 保存最高价格
                        if (highestPrice == null || priceInRoubles > highestPrice.PriceInRoubles)
                        {
                            highestPrice = new TraderPrice(
                                trader.Id,
                                trader.LocalizedName,
                                amount,
                                currencyId,
                                currencyCourse,
                                priceInRoubles
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录失败原因（Debug级别），便于排查模组物品估价失败问题
                        ClientLog.Debug($"   商人 {GetTraderNameSafe(trader)} 估价异常（{item.LocalizedName()}）: {ex.Message}");
                        continue;
                    }
                }

                // 如果第一次使用时没有找到价格，给出友好提示
                if (highestPrice == null && !_hasShownInitTip)
                {
                    // Plugin.Log.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    // Plugin.Log.LogInfo("💡 首次使用商人价格功能");
                    // Plugin.Log.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    // Plugin.Log.LogInfo("   请打开任意商人界面（如 Prapor、Therapist）");
                    // Plugin.Log.LogInfo("   然后关闭界面，商人价格功能即可正常使用");
                    // Plugin.Log.LogInfo("   此步骤每次游戏启动只需执行一次");
                    // Plugin.Log.LogInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    _hasShownInitTip = true;
                }

                // ===== 优化3: 保存到缓存 =====
                if (highestPrice != null)
                {
                    _priceCache[cacheKey] = highestPrice;
                    ClientLog.Debug($"✅ 本地商人估价: {item.LocalizedName()} = {highestPrice.PriceInRoubles:N0}₽（{highestPrice.TraderName}）");
                    // Plugin.Log.LogDebug($"💾 保存缓存: {item.LocalizedName()} = {highestPrice.PriceInRoubles:N0}₽");
                }
                else if (availableTraders > 0)
                {
                    // 商人数据已就绪且遍历后仍无人收购：缓存负结果，避免每次悬停重复计算
                    // 若商人数据尚未加载（availableTraders == 0），不缓存以便下次重试
                    _noPriceCache.Add(cacheKey);
                    ClientLog.Debug($"❌ 本地商人估价: {item.LocalizedName()} 无商人收购（已遍历 {availableTraders} 个可用商人），加入负缓存");
                }
                else
                {
                    ClientLog.Debug($"⏳ 本地商人估价: {item.LocalizedName()} 商人数据未就绪，下次悬停重试");
                }

                return highestPrice;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"❌ 获取商人价格失败: {ex.Message}");
                return null;
            }
        }

        private bool TryGetServerTraderPrice(string templateId, out TraderPrice price, out bool serverReady)
        {
            price = null;
            serverReady = PriceDataService.Instance.IsTraderBuybackCacheReady();

            if (!serverReady)
                return false;

            if (!PriceDataService.Instance.TryGetTraderBuybackPrice(templateId, out var entry))
                return false;

            var amount = (int)Math.Round(entry.PriceRoubles, 0);
            price = new TraderPrice(
                entry.TraderId,
                entry.TraderName,
                amount,
                RoubleCurrencyId,
                1.0,
                entry.PriceRoubles
            );

            return true;
        }

        /// <summary>
        /// 检查物品是否是容器（带反射缓存）
        /// </summary>
        /// <param name="item">物品</param>
        /// <returns>true = 容器, false = 非容器</returns>
        private bool IsContainer(Item item)
        {
            try
            {
                var itemType = item.GetType();

                // ===== 优化4: 从缓存获取反射的 PropertyInfo =====
                // 避免重复反射，每个类型只反射一次
                if (!_containerPropertyCache.TryGetValue(itemType, out var isContainerProperty))
                {
                    // 首次访问：反射获取并缓存
                    isContainerProperty = itemType.GetProperty("IsContainer");
                    _containerPropertyCache[itemType] = isContainerProperty;
                }

                if (isContainerProperty != null)
                {
                    var isContainer = isContainerProperty.GetValue(item);
                    return isContainer is bool boolValue && boolValue;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 清除价格缓存（价格更新时调用）
        /// </summary>
        public void ClearCache()
        {
            _priceCache.Clear();
            _noPriceCache.Clear();
            // Plugin.Log.LogInfo("🔄 商人价格缓存已清除");
        }

        /// <summary>
        /// 获取缓存统计信息（用于调试）
        /// </summary>
        public string GetCacheStats()
        {
            return $"商人价格缓存: {_priceCache.Count} 项";
        }

        /// <summary>
        /// 获取所有商人
        /// </summary>
        private System.Collections.Generic.IEnumerable<TraderClass> GetAllTraders()
        {
            try
            {
                // 使用 Singleton 获取 ClientApplication
                var clientApp = Singleton<ClientApplication<ISession>>.Instance;
                if (clientApp == null)
                    return null;

                var session = clientApp.GetClientBackEndSession();
                if (session != null && session.Traders != null)
                    return session.Traders;

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 检查商人是否可用
        /// </summary>
        private bool IsTraderAvailable(TraderClass trader)
        {
            try
            {
                return trader != null
                    && trader.Info != null
                    && trader.Info.Available
                    && !trader.Info.Disabled
                    && trader.Info.Unlocked;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 安全地获取商人名称（用于日志）
        /// </summary>
        private static string GetTraderNameSafe(TraderClass trader)
        {
            try
            {
                if (trader == null)
                    return "?";

                var name = trader.LocalizedName;
                return string.IsNullOrWhiteSpace(name) ? trader.Id?.ToString() ?? "?" : name;
            }
            catch
            {
                return "?";
            }
        }

        /// <summary>
        /// 获取货币汇率
        /// </summary>
        private double GetCurrencyCourse(TraderClass trader, MongoID currencyId)
        {
            try
            {
                var supplyData = trader.GetSupplyData();
                if (supplyData?.CurrencyCourses != null
                    && supplyData.CurrencyCourses.ContainsKey(currencyId))
                {
                    return supplyData.CurrencyCourses[currencyId];
                }

                // 默认汇率为 1（卢布）
                return 1.0;
            }
            catch
            {
                return 1.0;
            }
        }
    }
}
