// ----------------------------------------------------------------------------
// QuickPrice - Custom Static Router
// 处理HTTP路由注册和请求处理
// ----------------------------------------------------------------------------

using System.Text.Json;
using System.Diagnostics;
using System.Reflection;
using IoPath = System.IO.Path;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Services;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QuickPrice.Server
{
    /// <summary>
    /// QuickPrice 自定义静态路由器
    /// 继承自StaticRouter，注册HTTP端点
    /// </summary>
    [Injectable]
    public class QuickPriceStaticRouter : StaticRouter
    {
        private static DatabaseService? _databaseServiceStatic;
        private static RagfairOfferService? _ragfairOfferServiceStatic;
        private static ISptLogger<QuickPriceStaticRouter>? _loggerStatic;

        // 价格缓存
        private static Dictionary<string, double>? _cachedDynamicPrices;
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly object _cacheLock = new object();
        private static bool _isUpdatingCache = false;
        private static System.Threading.Timer? _autoRefreshTimer; // 自动刷新定时器
        private static bool _isPreloadStarted = false; // 防止重复预加载
        private static bool _isTraderBuybackPreloadStarted = false; // 防止重复预加载

        // 商人回收价格缓存
        private static Dictionary<string, TraderBuybackPrice>? _cachedTraderBuybackPrices;
        private static DateTime _traderBuybackCacheTime = DateTime.MinValue;
        private static readonly object _traderBuybackCacheLock = new object();
        private static bool _isUpdatingTraderBuybackCache = false;

        // 配置
        private static QuickPriceConfig? _config;
        private static bool _isInitialized = false; // 防止重复初始化
        private static readonly object _configLock = new object();
        private static string? _configPath;
        private static DateTime? _configLastWriteUtc;
        private static readonly object _ragfairBlacklistLogLock = new object();
        private static bool _ragfairBlacklistLogged = false;
        private static readonly object _ragfairDynamicSettingsLock = new object();
        private static bool _ragfairDynamicSettingsLoaded = false;
        private static HashSet<string> _ragfairCustomBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static HashSet<string> _ragfairCustomCategoryBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _ragfairEnableBsgList = false;
        private static bool _ragfairEnableQuestList = false;
        private static bool _ragfairEnableCustomItemCategoryList = false;
        private static bool _ragfairTraderItems = false;
        private static bool _ragfairDamagedAmmoPacks = false;
        private static readonly object _ragfairFullBlacklistLogLock = new object();
        private static bool _ragfairFullBlacklistLogged = false;
        private static readonly object _itemNameLock = new object();
        private static Dictionary<string, string>? _itemNameCache;
        private static readonly Dictionary<string, PropertyInfo?> _itemPropertyCache = new Dictionary<string, PropertyInfo?>(StringComparer.Ordinal);
        private static readonly Dictionary<string, FieldInfo?> _itemFieldCache = new Dictionary<string, FieldInfo?>(StringComparer.Ordinal);
        private static readonly object _itemConfigLock = new object();
        private static bool _itemConfigBlacklistLoaded = false;
        private static HashSet<string> _itemConfigBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _ragfairBannedCacheLock = new object();
        private static Dictionary<string, RagfairBannedItemInfo>? _ragfairBannedItemInfoCache;
        private static List<string>? _ragfairBannedItemIdCache;
        private static DateTime _ragfairBannedCacheTime = DateTime.MinValue;

        public QuickPriceStaticRouter(
            JsonUtil jsonUtil,
            DatabaseService databaseService,
            RagfairOfferService ragfairOfferService,
            ISptLogger<QuickPriceStaticRouter> logger) : base(
            jsonUtil,
            GetCustomRoutes()
        )
        {
            // 使用锁防止重复初始化整个模组
            lock (_cacheLock)
            {
                if (_isInitialized)
                {
                    // 已经初始化过了，只更新服务引用
                    _databaseServiceStatic = databaseService;
                    _ragfairOfferServiceStatic = ragfairOfferService;
                    _loggerStatic = logger;
                    return;
                }

                // 标记为已初始化
                _isInitialized = true;

                // 保存到静态变量供路由处理方法使用
                _databaseServiceStatic = databaseService;
                _ragfairOfferServiceStatic = ragfairOfferService;
                _loggerStatic = logger;

                logger.Info("[QuickPrice] 自定义路由已初始化", null);

                // 加载配置文件
                LoadConfig(force: true);
                TryLogRagfairDynamicBlacklistAfterInit();

                if (IsEnabled())
                {
                    // 启动时预加载动态价格缓存（异步，不阻塞启动）
                    if (!_isPreloadStarted)
                    {
                        _isPreloadStarted = true;
                        _ = PreloadDynamicPriceCacheAsync();
                    }

                    if (!_isTraderBuybackPreloadStarted)
                    {
                        _isTraderBuybackPreloadStarted = true;
                        _ = PreloadTraderBuybackPriceCacheAsync();
                    }
                }
                else
                {
                    logger.Info("[QuickPrice] 模组已禁用，跳过预加载和自动刷新", null);
                }
            }
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private static void LoadConfig(bool force = false)
        {
            try
            {
                lock (_configLock)
                {
                    _configPath ??= ResolveConfigPath();

                    if (string.IsNullOrWhiteSpace(_configPath))
                    {
                        _config = new QuickPriceConfig();
                        return;
                    }

                    if (!File.Exists(_configPath))
                    {
                        _loggerStatic?.Warning($"[QuickPrice] 配置文件不存在: {_configPath}, 将使用默认配置", null);
                        _config = new QuickPriceConfig();
                        _configLastWriteUtc = null;
                        return;
                    }

                    var lastWriteUtc = File.GetLastWriteTimeUtc(_configPath);
                    if (!force && _config != null && _configLastWriteUtc.HasValue && lastWriteUtc == _configLastWriteUtc.Value)
                        return;

                    var jsonContent = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<QuickPriceConfig>(jsonContent) ?? new QuickPriceConfig();
                    _configLastWriteUtc = lastWriteUtc;

                    _loggerStatic?.Info($"[QuickPrice] 配置文件已加载 (路径: {_configPath}, 自动刷新间隔: {_config.AutoRefreshIntervalMinutes} 分钟)", null);
                }
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice] 加载配置文件失败: {ex.Message}, 将使用默认配置", ex);
                _config = new QuickPriceConfig();
            }
        }

        private static void EnsureConfigLoaded()
        {
            var previousInterval = _config?.AutoRefreshIntervalMinutes;
            var previousEnabled = _config?.Enabled;

            LoadConfig();

            if (_autoRefreshTimer != null)
            {
                if (_config?.Enabled == false)
                {
                    _autoRefreshTimer.Dispose();
                    _autoRefreshTimer = null;
                    _loggerStatic?.Info("[QuickPrice] 模组已禁用，已停止自动刷新定时器", null);
                    return;
                }

                if (_config?.AutoRefreshIntervalMinutes != previousInterval || _config?.Enabled != previousEnabled)
                    StartAutoRefreshTimer();
            }
        }

        /// <summary>
        /// 在初始化完成后尝试读取 ragfair.json 的 dynamic.blacklist 并打印
        /// </summary>
        private static void TryLogRagfairDynamicBlacklistAfterInit()
        {
            lock (_ragfairBlacklistLogLock)
            {
                if (_ragfairBlacklistLogged)
                    return;
                _ragfairBlacklistLogged = true;
            }

            try
            {
                LoadRagfairDynamicBlacklistSettings();
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice-RagfairBlacklist] 读取动态黑名单失败: {ex.Message}", ex);
            }
        }

        private static void LoadRagfairDynamicBlacklistSettings()
        {
            lock (_ragfairDynamicSettingsLock)
            {
                if (_ragfairDynamicSettingsLoaded)
                    return;
                _ragfairDynamicSettingsLoaded = true;

                var ragfairConfigPath = IoPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "SPT_Data", "configs", "ragfair.json");
                if (!File.Exists(ragfairConfigPath))
                {
                    return;
                }

                var json = File.ReadAllText(ragfairConfigPath);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("dynamic", out var dynamicElement))
                {
                    return;
                }

                if (!dynamicElement.TryGetProperty("blacklist", out var blacklistElement))
                {
                    return;
                }

                _ragfairCustomBlacklist = new HashSet<string>(ReadStringArray(blacklistElement, "custom"), StringComparer.OrdinalIgnoreCase);
                _ragfairCustomCategoryBlacklist = new HashSet<string>(ReadStringArray(blacklistElement, "customItemCategoryList"), StringComparer.OrdinalIgnoreCase);
                _ragfairEnableBsgList = ReadBool(blacklistElement, "enableBsgList");
                _ragfairEnableQuestList = ReadBool(blacklistElement, "enableQuestList");
                _ragfairEnableCustomItemCategoryList = ReadBool(blacklistElement, "enableCustomItemCategoryList");
                _ragfairTraderItems = ReadBool(blacklistElement, "traderItems");
                _ragfairDamagedAmmoPacks = ReadBool(blacklistElement, "damagedAmmoPacks");
            }
        }

        private static void LoadItemConfigBlacklist()
        {
            lock (_itemConfigLock)
            {
                if (_itemConfigBlacklistLoaded)
                    return;
                _itemConfigBlacklistLoaded = true;

                var itemConfigPath = IoPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "SPT_Data", "configs", "item.json");
                if (!File.Exists(itemConfigPath))
                {
                    return;
                }

                using var doc = JsonDocument.Parse(File.ReadAllText(itemConfigPath));
                _itemConfigBlacklist = new HashSet<string>(
                    ReadStringArray(doc.RootElement, "blacklist"),
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        private static List<string> ReadStringArray(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array)
            {
                var result = new List<string>();
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        result.Add(item.GetString() ?? string.Empty);
                }
                return result;
            }

            return new List<string>();
        }

        private static bool ReadBool(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value)
                && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False))
            {
                return value.GetBoolean();
            }

            return false;
        }

        private static void TryLogFullRagfairBlacklistAfterDatabaseReady()
        {
            lock (_ragfairFullBlacklistLogLock)
            {
                if (_ragfairFullBlacklistLogged)
                    return;
                _ragfairFullBlacklistLogged = true;
            }

            try
            {
                var bannedItems = GetOrBuildRagfairBannedItemInfoCache();
                _loggerStatic?.Info($"[QuickPrice-RagfairBan] 完整禁售列表已生成: {bannedItems.Count} 个物品", null);
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice-RagfairBan] 构建完整禁售列表失败: {ex.Message}", ex);
            }
        }

        private static bool IsEnabled()
        {
            return _config?.Enabled ?? true;
        }

        private static string ResolveConfigPath()
        {
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrWhiteSpace(assemblyLocation))
                {
                    var assemblyDir = IoPath.GetDirectoryName(assemblyLocation);
                    if (!string.IsNullOrWhiteSpace(assemblyDir))
                    {
                        var candidate = IoPath.Combine(assemblyDir, "config.json");
                        if (File.Exists(candidate))
                            return candidate;
                    }
                }
            }
            catch
            {
            }

            var fallbackModPath = IoPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "user", "mods", "QuickPrice");
            return IoPath.Combine(fallbackModPath, "config.json");
        }

        /// <summary>
        /// 定义自定义路由
        /// </summary>
        private static List<RouteAction> GetCustomRoutes()
        {
            return
            [
                // 路由1: 获取货币购买价格
                new RouteAction<EmptyRequestData>(
                    "/showMeTheMoney/getCurrencyPurchasePrices",
                    async (url, info, sessionId, output) =>
                        await HandleGetCurrencyPurchasePrices(url, info, sessionId)
                ),

                // 路由2: 获取静态价格表
                new RouteAction<EmptyRequestData>(
                    "/showMeTheMoney/getStaticPriceTable",
                    async (url, info, sessionId, output) =>
                        await HandleGetStaticPriceTable(url, info, sessionId)
                ),

                // 路由3: 获取动态价格表
                new RouteAction<EmptyRequestData>(
                    "/showMeTheMoney/getDynamicPriceTable",
                    async (url, info, sessionId, output) =>
                        await HandleGetDynamicPriceTable(url, info, sessionId)
                ),

                // 路由4: 获取商人回收价格表（最高价）
                new RouteAction<EmptyRequestData>(
                    "/showMeTheMoney/getTraderBuybackPriceTable",
                    async (url, info, sessionId, output) =>
                        await HandleGetTraderBuybackPriceTable(url, info, sessionId)
                ),

                // 路由5: 获取跳蚤市场禁售物品列表
                new RouteAction<EmptyRequestData>(
                    "/showMeTheMoney/getRagfairBannedItems",
                    async (url, info, sessionId, output) =>
                        await HandleGetRagfairBannedItems(url, info, sessionId)
                )
            ];
        }

        #region 路由处理方法

        /// <summary>
        /// 处理获取货币购买价格的请求
        /// </summary>
        private static ValueTask<string> HandleGetCurrencyPurchasePrices(
            string url,
            EmptyRequestData info,
            MongoId sessionId)
        {
            try
            {
                EnsureConfigLoaded();
                if (!IsEnabled())
                    return new ValueTask<string>(JsonSerializer.Serialize(new CurrencyPurchasePrices { Eur = 153, Usd = 139 }));

                double eurPrice = 153;  // 默认值
                double usdPrice = 139;  // 默认值

                // 尝试从数据库获取实际价格
                if (_databaseServiceStatic != null)
                {
                    try
                    {
                        var tables = _databaseServiceStatic.GetTables();

                        // Skier (Скупщик) - 出售欧元
                        // Peacekeeper (Миротворец) - 出售美元
                        // 这些是默认的货币交易商ID
                        const string SkierId = "58330581ace78e27b8b10cee";
                        const string PeacekeeperId = "5935c25fb3acc3127c3d8cd9";

                        // 尝试从交易商数据获取货币价格
                        // 注：实际实现取决于 DatabaseService 的 API
                        // 如果无法获取，使用默认值

                        _loggerStatic?.Info($"[QuickPrice] Currency prices queried - EUR: {eurPrice}, USD: {usdPrice}", null);
                    }
                    catch (Exception dbEx)
                    {
                        _loggerStatic?.Warning($"[QuickPrice] Could not get currency prices from database: {dbEx.Message}", null);
                    }
                }

                var prices = new CurrencyPurchasePrices
                {
                    Eur = eurPrice,
                    Usd = usdPrice
                };

                var json = JsonSerializer.Serialize(prices);
                return new ValueTask<string>(json);
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice] Error in GetCurrencyPurchasePrices: {ex.Message}", ex);
                // Console.WriteLine($"[QuickPrice] Error in GetCurrencyPurchasePrices: {ex.Message}");
                // 返回默认值
                var fallback = JsonSerializer.Serialize(new CurrencyPurchasePrices { Eur = 153, Usd = 139 });
                return new ValueTask<string>(fallback);
            }
        }

        /// <summary>
        /// 处理获取静态价格表的请求
        /// </summary>
        private static ValueTask<string> HandleGetStaticPriceTable(
            string url,
            EmptyRequestData info,
            MongoId sessionId)
        {
            try
            {
                EnsureConfigLoaded();
                if (!IsEnabled())
                    return new ValueTask<string>(JsonSerializer.Serialize(new Dictionary<string, double>()));

                var priceTable = new Dictionary<string, double>();

                if (_databaseServiceStatic != null)
                {
                    try
                    {
                        var tables = _databaseServiceStatic.GetTables();

                        // 获取价格表 - 这是主要的价格来源
                        if (tables?.Templates?.Prices != null)
                        {
                            foreach (var priceEntry in tables.Templates.Prices)
                            {
                                string itemId = priceEntry.Key;
                                double price = priceEntry.Value;

                                if (price > 0)
                                {
                                    priceTable[itemId] = price;
                                }
                            }

                            _loggerStatic?.Info($"[QuickPrice] Static price table generated with {priceTable.Count} items", null);
                        }
                        else
                        {
                            _loggerStatic?.Warning("[QuickPrice] Templates.Prices is null", null);
                        }
                    }
                    catch (Exception dbEx)
                    {
                        _loggerStatic?.Error($"[QuickPrice] Error accessing database: {dbEx.Message}", dbEx);
                    }
                }
                else
                {
                    _loggerStatic?.Warning("[QuickPrice] DatabaseService is not available", null);
                }

                var json = JsonSerializer.Serialize(priceTable);
                return new ValueTask<string>(json);
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice] Error in GetStaticPriceTable: {ex.Message}", ex);
                // Console.WriteLine($"[QuickPrice] Error in GetStaticPriceTable: {ex.Message}");
                var fallback = JsonSerializer.Serialize(new Dictionary<string, double>());
                return new ValueTask<string>(fallback);
            }
        }

        /// <summary>
        /// 处理获取动态价格表的请求
        /// 优化版：使用缓存 + 并行查询
        /// </summary>
        private static ValueTask<string> HandleGetDynamicPriceTable(
            string url,
            EmptyRequestData info,
            MongoId sessionId)
        {
            try
            {
                EnsureConfigLoaded();
                if (!IsEnabled())
                    return new ValueTask<string>(JsonSerializer.Serialize(new Dictionary<string, double>()));

                // 检查缓存是否有效（根据配置的过期时间）
                var cacheAgeSeconds = (DateTime.Now - _lastCacheUpdate).TotalSeconds;
                int cacheTimeoutSeconds = _config?.CacheTimeoutSeconds ?? 300;
                bool isCacheValid = _cachedDynamicPrices != null && cacheTimeoutSeconds > 0 && cacheAgeSeconds < cacheTimeoutSeconds;

                if (isCacheValid)
                {
                    var cacheAgeMinutes = cacheAgeSeconds / 60d;
                    _loggerStatic?.Info($"[QuickPrice] 返回缓存的动态价格（{_cachedDynamicPrices!.Count} 个物品，缓存年龄: {cacheAgeMinutes:F1} 分钟）", null);
                    var json = JsonSerializer.Serialize(_cachedDynamicPrices);
                    return new ValueTask<string>(json);
                }

                // 缓存过期或不存在，触发异步更新（不阻塞）
                if (!_isUpdatingCache)
                {
                    _ = UpdateDynamicPriceCacheAsync();
                }

                // 如果有旧缓存，先返回旧数据（不让客户端等待）
                if (_cachedDynamicPrices != null)
                {
                    var cacheAgeMinutes = cacheAgeSeconds / 60d;
                    _loggerStatic?.Info($"[QuickPrice] 返回旧缓存数据，同时后台更新（{_cachedDynamicPrices.Count} 个物品，缓存年龄: {cacheAgeMinutes:F1} 分钟）", null);
                    var json = JsonSerializer.Serialize(_cachedDynamicPrices);
                    return new ValueTask<string>(json);
                }

                // 如果没有缓存，回退到静态价格
                _loggerStatic?.Warning("[QuickPrice] 无可用缓存，回退到静态价格", null);
                return HandleGetStaticPriceTable(url, info, sessionId);
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice] Error in GetDynamicPriceTable: {ex.Message}", ex);
                // 回退到静态价格表
                return HandleGetStaticPriceTable(url, info, sessionId);
            }
        }

        /// <summary>
        /// 处理获取商人回收价格表的请求（每个物品保留最高价商人）
        /// </summary>
        private static ValueTask<string> HandleGetTraderBuybackPriceTable(
            string url,
            EmptyRequestData info,
            MongoId sessionId)
        {
            try
            {
                EnsureConfigLoaded();
                if (!IsEnabled())
                    return new ValueTask<string>(JsonSerializer.Serialize(new Dictionary<string, TraderBuybackPrice>()));

                var cacheAgeSeconds = (DateTime.Now - _traderBuybackCacheTime).TotalSeconds;
                int cacheTimeoutSeconds = _config?.CacheTimeoutSeconds ?? 300;
                bool isCacheValid = _cachedTraderBuybackPrices != null && cacheTimeoutSeconds > 0 && cacheAgeSeconds < cacheTimeoutSeconds;

                if (isCacheValid)
                {
                    var cacheAgeMinutes = cacheAgeSeconds / 60d;
                    _loggerStatic?.Info($"[QuickPrice] 返回缓存的商人回收价格（{_cachedTraderBuybackPrices!.Count} 个物品，缓存年龄: {cacheAgeMinutes:F1} 分钟）", null);
                    var json = JsonSerializer.Serialize(_cachedTraderBuybackPrices);
                    return new ValueTask<string>(json);
                }

                if (!_isUpdatingTraderBuybackCache)
                {
                    _ = UpdateTraderBuybackPriceCacheAsync();
                }

                if (_cachedTraderBuybackPrices != null)
                {
                    var cacheAgeMinutes = cacheAgeSeconds / 60d;
                    _loggerStatic?.Info($"[QuickPrice] 返回旧缓存的商人回收价格，同时后台更新（{_cachedTraderBuybackPrices.Count} 个物品，缓存年龄: {cacheAgeMinutes:F1} 分钟）", null);
                    var json = JsonSerializer.Serialize(_cachedTraderBuybackPrices);
                    return new ValueTask<string>(json);
                }

                _loggerStatic?.Warning("[QuickPrice] 无商人回收价格缓存，返回空表", null);
                var fallback = JsonSerializer.Serialize(new Dictionary<string, TraderBuybackPrice>());
                return new ValueTask<string>(fallback);
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice] Error in GetTraderBuybackPriceTable: {ex.Message}", ex);
                var fallback = JsonSerializer.Serialize(new Dictionary<string, TraderBuybackPrice>());
                return new ValueTask<string>(fallback);
            }
        }

        /// <summary>
        /// 异步更新动态价格缓存（并行优化）
        /// </summary>
        private static async Task UpdateDynamicPriceCacheAsync()
        {
            // 防止重复更新
            lock (_cacheLock)
            {
                if (_isUpdatingCache)
                {
                    _loggerStatic?.Info("[QuickPrice] 缓存更新已在进行中，跳过", null);
                    return;
                }
                _isUpdatingCache = true;
            }

            try
            {
                EnsureConfigLoaded();
                if (!IsEnabled())
                {
                    _loggerStatic?.Info("[QuickPrice] 模组已禁用，跳过缓存更新", null);
                    return;
                }

                _loggerStatic?.Info("[QuickPrice] 开始动态价格缓存更新（聚合模式）...", null);
                var stopwatch = Stopwatch.StartNew();

                // 获取静态价格作为基础
                var priceTable = new Dictionary<string, double>();

                if (_databaseServiceStatic != null)
                {
                    var tables = _databaseServiceStatic.GetTables();

                    // 先加载静态价格
                    if (tables?.Templates?.Prices != null)
                    {
                        foreach (var priceEntry in tables.Templates.Prices)
                        {
                            if (priceEntry.Value > 0)
                            {
                                priceTable[priceEntry.Key] = priceEntry.Value;
                            }
                        }
                    }

                    // 聚合跳蚤市场报价（一次性遍历全部报价）
                    if (_ragfairOfferServiceStatic != null && priceTable.Count > 0)
                    {
                        int updatedCount = 0;
                        var validTemplates = new HashSet<string>(priceTable.Keys);
                        var aggregated = new Dictionary<string, (double Sum, int Count)>(priceTable.Count);

                        await Task.Run(() =>
                        {
                            var offers = _ragfairOfferServiceStatic.GetOffers();
                            if (offers == null)
                            {
                                return;
                            }

                            foreach (var offer in offers)
                            {
                                try
                                {
                                    if (offer?.User?.Id == null)
                                    {
                                        continue;
                                    }

                                    if (!offer.RequirementsCost.HasValue || offer.RequirementsCost.Value <= 0)
                                    {
                                        continue;
                                    }

                                    var rootItem = offer.Items?.FirstOrDefault(i => i.Id == offer.Root);
                                    if (rootItem == null)
                                    {
                                        continue;
                                    }

                                    var templateId = rootItem.Template.ToString();
                                    if (string.IsNullOrEmpty(templateId) || !validTemplates.Contains(templateId))
                                    {
                                        continue;
                                    }

                                    var cost = (double)offer.RequirementsCost.Value;
                                    if (aggregated.TryGetValue(templateId, out var acc))
                                    {
                                        aggregated[templateId] = (acc.Sum + cost, acc.Count + 1);
                                    }
                                    else
                                    {
                                        aggregated[templateId] = (cost, 1);
                                    }
                                }
                                catch
                                {
                                    // 某个报价解析失败，继续处理其他报价
                                }
                            }
                        });

                        foreach (var kvp in aggregated)
                        {
                            if (kvp.Value.Count > 0)
                            {
                                priceTable[kvp.Key] = kvp.Value.Sum / kvp.Value.Count;
                                updatedCount++;
                            }
                        }

                        stopwatch.Stop();
                        _loggerStatic?.Info($"[QuickPrice] 动态价格缓存已更新: {priceTable.Count} 个物品（{updatedCount} 个来自跳蚤市场），耗时 {stopwatch.ElapsedMilliseconds} ms", null);
                    }
                }

                // 更新缓存
                lock (_cacheLock)
                {
                    _cachedDynamicPrices = priceTable;
                    _lastCacheUpdate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice] 缓存更新失败: {ex.Message}", ex);
            }
            finally
            {
                lock (_cacheLock)
                {
                    _isUpdatingCache = false;
                }
            }
        }

        /// <summary>
        /// 异步更新商人回收价格缓存
        /// </summary>
        private static async Task UpdateTraderBuybackPriceCacheAsync()
        {
            lock (_traderBuybackCacheLock)
            {
                if (_isUpdatingTraderBuybackCache)
                {
                    _loggerStatic?.Info("[QuickPrice] 商人回收价格缓存更新已在进行中，跳过", null);
                    return;
                }
                _isUpdatingTraderBuybackCache = true;
            }

            try
            {
                EnsureConfigLoaded();
                if (!IsEnabled())
                {
                    _loggerStatic?.Info("[QuickPrice] 模组已禁用，跳过商人回收价格缓存更新", null);
                    return;
                }

                if (_databaseServiceStatic == null)
                {
                    _loggerStatic?.Warning("[QuickPrice] DatabaseService 不可用，无法更新商人回收价格缓存", null);
                    return;
                }

                var stopwatch = Stopwatch.StartNew();
                var cache = await Task.Run(() => BuildTraderBuybackPriceCache());
                stopwatch.Stop();

                lock (_traderBuybackCacheLock)
                {
                    _cachedTraderBuybackPrices = cache;
                    _traderBuybackCacheTime = DateTime.Now;
                }

                _loggerStatic?.Info($"[QuickPrice] 商人回收价格缓存已更新: {cache.Count} 个物品，耗时 {stopwatch.ElapsedMilliseconds} ms", null);
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice] 商人回收价格缓存更新失败: {ex.Message}", ex);
            }
            finally
            {
                lock (_traderBuybackCacheLock)
                {
                    _isUpdatingTraderBuybackCache = false;
                }
            }
        }

        /// <summary>
        /// 服务器启动时预加载动态价格缓存
        /// </summary>
        private static async Task PreloadDynamicPriceCacheAsync()
        {
            try
            {
                EnsureConfigLoaded();
                if (!IsEnabled())
                {
                    _loggerStatic?.Info("[QuickPrice] 模组已禁用，跳过预加载", null);
                    return;
                }

                // 等待更长时间，确保数据库和跳蚤市场已完全初始化
                _loggerStatic?.Info("[QuickPrice] 正在等待数据库初始化...", null);

                // 第一阶段：等待数据库就绪（每1秒轮询一次，直到成功）
                int retryDelayMs = 1000;
                int attempts = 0;

                while (true)
                {
                    await Task.Delay(retryDelayMs);
                    attempts++;

                    try
                    {
                        // 尝试访问数据库，检查是否已初始化
                        if (_databaseServiceStatic != null)
                        {
                            var tables = _databaseServiceStatic.GetTables();
                            if (tables?.Templates?.Prices != null && tables.Templates.Prices.Count > 0)
                            {
                                var elapsedSeconds = attempts * retryDelayMs / 1000;
                                _loggerStatic?.Info($"[QuickPrice] 数据库已就绪（耗时 {elapsedSeconds} 秒）", null);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // 数据库还未就绪，继续等待
                    }

                    if (attempts % 10 == 0)
                    {
                        _loggerStatic?.Info($"[QuickPrice] 仍在等待数据库...（已等待 {attempts * retryDelayMs / 1000} 秒）", null);
                    }
                }

                // 第二阶段：额外等待10秒，确保跳蚤市场报价已生成
                _loggerStatic?.Info("[QuickPrice] 再等待10秒以确保跳蚤市场报价生成完成...", null);
                await Task.Delay(10000);

                // 数据库就绪后尝试构建完整禁售列表
                TryLogFullRagfairBlacklistAfterDatabaseReady();

                _loggerStatic?.Info("========================================", null);
                _loggerStatic?.Info("[QuickPrice] 开始预加载动态价格缓存...", null);
                _loggerStatic?.Info("========================================", null);

                // 调用更新缓存方法
                await UpdateDynamicPriceCacheAsync();

                if (_cachedDynamicPrices != null && _cachedDynamicPrices.Count > 0)
                {
                    _loggerStatic?.Success("========================================", null);
                    _loggerStatic?.Success($"[QuickPrice] 缓存预加载完成！{_cachedDynamicPrices.Count} 个物品已就绪", null);
                    _loggerStatic?.Success("[QuickPrice] 客户端现在可以即时获取价格数据！", null);
                    _loggerStatic?.Success("========================================", null);

                    // 启动定时刷新（每30分钟）
                    StartAutoRefreshTimer();
                }
                else
                {
                    _loggerStatic?.Warning("========================================", null);
                    _loggerStatic?.Warning("[QuickPrice] 缓存预加载失败，将使用静态价格作为备用", null);
                    _loggerStatic?.Warning("========================================", null);
                }
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice] 预加载缓存失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 服务器启动时预加载商人回收价格缓存
        /// </summary>
        private static async Task PreloadTraderBuybackPriceCacheAsync()
        {
            try
            {
                EnsureConfigLoaded();
                if (!IsEnabled())
                {
                    _loggerStatic?.Info("[QuickPrice] 模组已禁用，跳过商人回收价格预加载", null);
                    return;
                }

                _loggerStatic?.Info("[QuickPrice] 正在等待数据库初始化（商人回收缓存）...", null);

                int retryDelayMs = 1000;
                int attempts = 0;

                while (true)
                {
                    await Task.Delay(retryDelayMs);
                    attempts++;

                    try
                    {
                        if (_databaseServiceStatic != null)
                        {
                            var tables = _databaseServiceStatic.GetTables();
                            if (tables?.Traders != null
                                && tables.Traders.Count > 0
                                && tables.Templates?.Handbook?.Items != null
                                && tables.Templates.Handbook.Items.Count > 0
                                && tables.Templates.Items != null
                                && tables.Templates.Items.Count > 0)
                            {
                                var elapsedSeconds = attempts * retryDelayMs / 1000;
                                _loggerStatic?.Info($"[QuickPrice] 数据库已就绪（商人回收缓存，耗时 {elapsedSeconds} 秒）", null);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // 数据库还未就绪，继续等待
                    }

                    if (attempts % 10 == 0)
                    {
                        _loggerStatic?.Info($"[QuickPrice] 仍在等待数据库（商人回收缓存）...（已等待 {attempts * retryDelayMs / 1000} 秒）", null);
                    }
                }

                _loggerStatic?.Info("========================================", null);
                _loggerStatic?.Info("[QuickPrice] 开始预加载商人回收价格缓存...", null);
                _loggerStatic?.Info("========================================", null);

                await UpdateTraderBuybackPriceCacheAsync();

                if (_cachedTraderBuybackPrices != null && _cachedTraderBuybackPrices.Count > 0)
                {
                    _loggerStatic?.Success("========================================", null);
                    _loggerStatic?.Success($"[QuickPrice] 商人回收缓存预加载完成！{_cachedTraderBuybackPrices.Count} 个物品已就绪", null);
                    _loggerStatic?.Success("========================================", null);
                }
                else
                {
                    _loggerStatic?.Warning("========================================", null);
                    _loggerStatic?.Warning("[QuickPrice] 商人回收缓存预加载失败，将在请求时再计算", null);
                    _loggerStatic?.Warning("========================================", null);
                }
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice] 商人回收缓存预加载失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 启动自动刷新定时器（根据配置的间隔刷新）
        /// </summary>
        private static void StartAutoRefreshTimer()
        {
            try
            {
                if (!IsEnabled())
                {
                    _autoRefreshTimer?.Dispose();
                    _autoRefreshTimer = null;
                    _loggerStatic?.Info("[QuickPrice] 模组已禁用，自动刷新未启动", null);
                    return;
                }

                // 获取配置的刷新间隔（默认5分钟）
                int intervalMinutes = _config?.AutoRefreshIntervalMinutes ?? 5;

                // 如果间隔为0，则禁用自动刷新
                if (intervalMinutes <= 0)
                {
                    _loggerStatic?.Info("[QuickPrice] 自动刷新已禁用 (配置间隔为0)", null);
                    return;
                }

                // 如果定时器已存在，先停止
                _autoRefreshTimer?.Dispose();

                // 创建定时器：根据配置的间隔刷新
                var refreshInterval = TimeSpan.FromMinutes(intervalMinutes);

                _autoRefreshTimer = new System.Threading.Timer(
                    async (state) =>
                    {
                        _loggerStatic?.Info("========================================", null);
                        _loggerStatic?.Info($"[QuickPrice] 自动刷新定时器触发（每{intervalMinutes}分钟）", null);
                        _loggerStatic?.Info("========================================", null);

                        await UpdateDynamicPriceCacheAsync();
                        await UpdateTraderBuybackPriceCacheAsync();
                    },
                    null,
                    refreshInterval,  // 首次执行延迟
                    refreshInterval   // 后续执行间隔
                );

                _loggerStatic?.Info($"[QuickPrice] 自动刷新定时器已启动（间隔: {intervalMinutes} 分钟）", null);
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice] 启动自动刷新定时器失败: {ex.Message}", ex);
            }
        }

        private sealed class TraderBuybackPrice
        {
            public string TraderId { get; set; } = string.Empty;
            public string TraderName { get; set; } = string.Empty;
            public double PriceRoubles { get; set; }
        }

        private static Dictionary<string, TraderBuybackPrice> BuildTraderBuybackPriceCache()
        {
            var result = new Dictionary<string, TraderBuybackPrice>(StringComparer.OrdinalIgnoreCase);

            if (_databaseServiceStatic == null)
                return result;

            var tables = _databaseServiceStatic.GetTables();
            if (tables?.Traders == null || tables.Traders.Count == 0)
            {
                _loggerStatic?.Warning("[QuickPrice] Traders 表为空，无法构建商人回收缓存", null);
                return result;
            }

            if (tables.Templates?.Handbook?.Items == null || tables.Templates.Handbook.Items.Count == 0)
            {
                _loggerStatic?.Warning("[QuickPrice] Handbook 价格表为空，无法构建商人回收缓存", null);
                return result;
            }

            if (tables.Templates.Items == null || tables.Templates.Items.Count == 0)
            {
                _loggerStatic?.Warning("[QuickPrice] Templates.Items 为空，无法构建商人回收缓存", null);
                return result;
            }

            var handbookPrices = new Dictionary<MongoId, double>();
            foreach (var handbookItem in tables.Templates.Handbook.Items)
            {
                var price = handbookItem.Price.GetValueOrDefault();
                if (price <= 0)
                    continue;

                handbookPrices[handbookItem.Id] = price;
            }

            if (handbookPrices.Count == 0)
            {
                _loggerStatic?.Warning("[QuickPrice] Handbook 价格表为空，无法构建商人回收缓存", null);
                return result;
            }

            var debugCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var debugFirstLines = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool debugLogged = false;

            foreach (var traderEntry in tables.Traders)
            {
                var traderId = traderEntry.Key;
                var trader = traderEntry.Value;
                var traderBase = trader?.Base;
                if (traderBase == null)
                    continue;

                var buyPriceCoef = traderBase.LoyaltyLevels?.FirstOrDefault()?.BuyPriceCoefficient ?? 0d;
                var percent = 100d - buyPriceCoef;
                if (percent <= 0)
                    continue;

                var buyCategories = traderBase.ItemsBuy?.Category;
                var buyIdList = traderBase.ItemsBuy?.IdList;
                if ((buyCategories == null || buyCategories.Count == 0) && (buyIdList == null || buyIdList.Count == 0))
                    continue;

                var traderName = GetTraderDisplayName(traderBase, traderId);
                var baseClassCache = new Dictionary<MongoId, bool>();

                foreach (var priceEntry in handbookPrices)
                {
                    var itemTpl = priceEntry.Key;
                    if (!CanTraderBuyItem(itemTpl, buyCategories, buyIdList, tables.Templates.Items, baseClassCache))
                        continue;

                    var priceRoubles = Math.Round(priceEntry.Value * percent / 100d, 0);
                    if (priceRoubles <= 0)
                        continue;

                    var itemIdStr = itemTpl.ToString();
                    if (!result.TryGetValue(itemIdStr, out var existing) || priceRoubles > existing.PriceRoubles)
                    {
                        result[itemIdStr] = new TraderBuybackPrice
                        {
                            TraderId = traderId.ToString(),
                            TraderName = traderName,
                            PriceRoubles = priceRoubles
                        };
                    }

                    if (!debugLogged)
                    {
                        if (!debugCounts.TryGetValue(itemIdStr, out var count))
                        {
                            count = 0;
                        }

                        count++;
                        debugCounts[itemIdStr] = count;

                        var line = $"{traderName}({traderId}) = {priceRoubles:N0}₽ (buyCoef {buyPriceCoef:0.##})";
                        if (count == 1)
                        {
                            debugFirstLines[itemIdStr] = line;
                        }
                        else if (count == 2)
                        {
                            var lines = new List<string>(2);
                            if (debugFirstLines.TryGetValue(itemIdStr, out var firstLine))
                                lines.Add(firstLine);
                            lines.Add(line);

                            tables.Templates.Items.TryGetValue(itemTpl, out var debugTemplate);
                            LogTraderBuybackDebug(itemIdStr, debugTemplate, lines, hasHandbookPrice: true);

                            debugLogged = true;
                            debugCounts.Clear();
                            debugFirstLines.Clear();
                        }
                    }
                }
            }

            if (!debugLogged)
            {
                _loggerStatic?.Warning("[QuickPrice-TraderBuyback] 未找到具有多个商人回收报价的物品", null);
            }

            return result;
        }

        private static string GetTraderDisplayName(TraderBase traderBase, MongoId traderId)
        {
            if (!string.IsNullOrWhiteSpace(traderBase.Nickname))
                return traderBase.Nickname;
            if (!string.IsNullOrWhiteSpace(traderBase.Name))
                return traderBase.Name;
            return traderId.ToString();
        }

        private static bool CanTraderBuyItem(
            MongoId itemTpl,
            HashSet<MongoId>? buyCategories,
            HashSet<MongoId>? buyIdList,
            Dictionary<MongoId, TemplateItem> templates,
            Dictionary<MongoId, bool> baseClassCache)
        {
            if (buyIdList != null && buyIdList.Contains(itemTpl))
                return true;

            if (buyCategories == null || buyCategories.Count == 0)
                return false;

            return IsOfBaseclasses(itemTpl, buyCategories, templates, baseClassCache);
        }

        private static bool IsOfBaseclasses(
            MongoId itemTpl,
            HashSet<MongoId> baseClasses,
            Dictionary<MongoId, TemplateItem> templates,
            Dictionary<MongoId, bool> cache)
        {
            if (cache.TryGetValue(itemTpl, out var cached))
                return cached;

            if (baseClasses.Contains(itemTpl))
            {
                cache[itemTpl] = true;
                return true;
            }

            var current = itemTpl;
            var visited = new HashSet<MongoId>();
            while (!current.IsEmpty && templates.TryGetValue(current, out var template))
            {
                var parent = template.Parent;
                if (parent.IsEmpty)
                    break;

                if (baseClasses.Contains(parent))
                {
                    cache[itemTpl] = true;
                    return true;
                }

                if (!visited.Add(parent))
                    break;

                current = parent;
            }

            cache[itemTpl] = false;
            return false;
        }

        private static void LogTraderBuybackDebug(
            string itemTpl,
            TemplateItem? template,
            List<string> lines,
            bool hasHandbookPrice)
        {
            var itemName = GetItemDisplayName(itemTpl, template);

            if (!hasHandbookPrice)
            {
                _loggerStatic?.Warning($"[QuickPrice-TraderBuyback] 物品无手册价格: {itemName} ({itemTpl})", null);
                return;
            }

            if (lines.Count == 0)
            {
                _loggerStatic?.Warning($"[QuickPrice-TraderBuyback] 未找到商人回收报价: {itemName} ({itemTpl})", null);
                return;
            }

            _loggerStatic?.Info($"[QuickPrice-TraderBuyback] {itemName} ({itemTpl}) 回收报价 {lines.Count} 条:", null);
            foreach (var line in lines)
            {
                _loggerStatic?.Info($"[QuickPrice-TraderBuyback] {line}", null);
            }
        }

        private sealed class RagfairBannedItemInfo
        {
            public RagfairBannedItemInfo(string itemId, string itemName)
            {
                ItemId = itemId;
                ItemName = itemName;
                Reasons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            public string ItemId { get; }
            public string ItemName { get; set; }
            public HashSet<string> Reasons { get; }
        }

        private static Dictionary<string, RagfairBannedItemInfo> BuildRagfairBannedItemInfoMapUsingAllSources()
        {
            LoadRagfairDynamicBlacklistSettings();
            LoadItemConfigBlacklist();

            var result = new Dictionary<string, RagfairBannedItemInfo>(StringComparer.OrdinalIgnoreCase);

            AddItemsFromItemsJson(result);

            if (_databaseServiceStatic != null)
            {
                try
                {
                    var tables = _databaseServiceStatic.GetTables();
                    if (tables?.Templates?.Items != null && tables.Templates.Items.Count > 0)
                        AddItemsToRagfairBannedInfoMap(tables.Templates.Items, result);
                }
                catch (Exception ex)
                {
                    _loggerStatic?.Error($"[QuickPrice-RagfairBan] 读取数据库物品模板失败: {ex.Message}", ex);
                }
            }

            AddBlacklistEntriesNotInTemplates(result);

            return result;
        }

        private static Dictionary<string, RagfairBannedItemInfo> GetOrBuildRagfairBannedItemInfoCache()
        {
            lock (_ragfairBannedCacheLock)
            {
                if (_ragfairBannedItemInfoCache == null)
                {
                    _ragfairBannedItemInfoCache = BuildRagfairBannedItemInfoMapUsingAllSources();
                    _ragfairBannedItemIdCache = _ragfairBannedItemInfoCache.Keys.ToList();
                    _ragfairBannedCacheTime = DateTime.Now;
                }

                return _ragfairBannedItemInfoCache;
            }
        }

        private static List<string> GetOrBuildRagfairBannedItemIdCache()
        {
            lock (_ragfairBannedCacheLock)
            {
                if (_ragfairBannedItemIdCache == null)
                {
                    var infoCache = GetOrBuildRagfairBannedItemInfoCache();
                    _ragfairBannedItemIdCache = infoCache.Keys.ToList();
                }

                return _ragfairBannedItemIdCache;
            }
        }

        private static void AddItemsFromItemsJson(Dictionary<string, RagfairBannedItemInfo> result)
        {
            var itemsPath = IoPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "SPT_Data", "database", "templates", "items.json");
            if (!File.Exists(itemsPath))
            {
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(itemsPath));
                foreach (var entry in doc.RootElement.EnumerateObject())
                {
                    var itemId = entry.Name;
                    if (string.IsNullOrWhiteSpace(itemId))
                        continue;

                    EvaluateRagfairBlacklistForItem(result, itemId, entry.Value);
                }
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice-RagfairBan] 解析 items.json 失败: {ex.Message}", ex);
            }
        }

        private static void AddItemsToRagfairBannedInfoMap<TKey, TItem>(
            IDictionary<TKey, TItem> items,
            Dictionary<string, RagfairBannedItemInfo> result)
            where TItem : class
        {
            foreach (var itemEntry in items)
            {
                var itemId = itemEntry.Key?.ToString();
                if (string.IsNullOrWhiteSpace(itemId))
                    continue;

                EvaluateRagfairBlacklistForItem(result, itemId, itemEntry.Value as object);
            }
        }

        private static void EvaluateRagfairBlacklistForItem(
            Dictionary<string, RagfairBannedItemInfo> result,
            string itemId,
            object? itemObj)
        {
            var canSell = GetBoolPropertyFromItem(itemObj, "CanSellOnRagfair");
            var isQuestItem = GetBoolPropertyFromItem(itemObj, "QuestItem", "IsQuestItem");
            var parentId = GetStringPropertyFromItem(itemObj, "ParentId", "Parent", "_parent");

            if (_ragfairEnableBsgList)
            {
                if (canSell == false)
                    AddBannedItem(result, itemId, itemObj, "noSell");
            }

            if (_ragfairEnableQuestList && isQuestItem == true)
                AddBannedItem(result, itemId, itemObj, "quest");

            if (_ragfairCustomBlacklist.Contains(itemId))
                AddBannedItem(result, itemId, itemObj, "custom");

            if (_ragfairEnableCustomItemCategoryList
                && !string.IsNullOrWhiteSpace(parentId)
                && _ragfairCustomCategoryBlacklist.Contains(parentId))
            {
                AddBannedItem(result, itemId, itemObj, "category");
            }

            if (_itemConfigBlacklist.Contains(itemId))
                AddBannedItem(result, itemId, itemObj, "itemConfig");
        }

        private static void AddBlacklistEntriesNotInTemplates(Dictionary<string, RagfairBannedItemInfo> result)
        {
            foreach (var itemId in _ragfairCustomBlacklist)
                AddBannedItem(result, itemId, null, "custom");

            foreach (var itemId in _itemConfigBlacklist)
                AddBannedItem(result, itemId, null, "itemConfig");
        }

        private static Dictionary<string, RagfairBannedItemInfo> BuildRagfairBannedItemInfoMap<TKey, TItem>(
            IDictionary<TKey, TItem> items)
            where TItem : class
        {
            var result = new Dictionary<string, RagfairBannedItemInfo>(StringComparer.OrdinalIgnoreCase);
            AddItemsToRagfairBannedInfoMap(items, result);
            AddBlacklistEntriesNotInTemplates(result);
            return result;
        }

        private static void AddBannedItem(
            Dictionary<string, RagfairBannedItemInfo> map,
            string itemId,
            object? itemObj,
            string reason)
        {
            if (!map.TryGetValue(itemId, out var info))
            {
                var name = GetItemDisplayName(itemId, itemObj);
                info = new RagfairBannedItemInfo(itemId, name);
                map[itemId] = info;
            }

            info.Reasons.Add(reason);
        }

        private static string GetItemDisplayName(string itemId, object? itemObj)
        {
            var localized = GetLocalizedItemName(itemId);
            if (!string.IsNullOrWhiteSpace(localized))
                return localized!;

            if (itemObj != null)
            {
                var name = GetStringPropertyFromItem(itemObj, "Name", "_name", "ShortName");
                if (!string.IsNullOrWhiteSpace(name))
                    return name!;
            }

            return itemId;
        }

        private static string? GetLocalizedItemName(string itemId)
        {
            EnsureItemNameCache();
            if (_itemNameCache != null && _itemNameCache.TryGetValue(itemId, out var name))
                return name;
            return null;
        }

        private static void EnsureItemNameCache()
        {
            lock (_itemNameLock)
            {
                if (_itemNameCache != null)
                    return;

                _itemNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var candidates = new[]
                {
                    IoPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "SPT_Data", "database", "locales", "global", "zh-cn.json"),
                    IoPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "SPT_Data", "database", "locales", "global", "en.json")
                };

                foreach (var path in candidates)
                {
                    if (!File.Exists(path))
                        continue;

                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    foreach (var entry in doc.RootElement.EnumerateObject())
                    {
                        if (!entry.Name.EndsWith(" Name", StringComparison.Ordinal))
                            continue;

                        var id = entry.Name.Substring(0, entry.Name.Length - " Name".Length);
                        if (_itemNameCache.ContainsKey(id))
                            continue;

                        if (entry.Value.ValueKind == JsonValueKind.String)
                        {
                            var name = entry.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(name))
                                _itemNameCache[id] = name;
                        }
                    }

                if (_itemNameCache.Count > 0)
                    break;
                }

                if (_itemNameCache.Count == 0)
                    return;
            }
        }

        private static bool? GetBoolPropertyFromItem(object? itemObj, params string[] names)
        {
            if (itemObj == null)
                return null;

            if (TryGetBoolFromObject(itemObj, names, out var value))
                return value;

            var props = GetItemProps(itemObj);
            if (props != null && TryGetBoolFromObject(props, names, out value))
                return value;

            return null;
        }

        private static string? GetStringPropertyFromItem(object? itemObj, params string[] names)
        {
            if (itemObj == null)
                return null;

            if (TryGetStringFromObject(itemObj, names, out var value))
                return value;

            var props = GetItemProps(itemObj);
            if (props != null && TryGetStringFromObject(props, names, out value))
                return value;

            return null;
        }

        private static bool TryGetBoolFromObject(object obj, string[] names, out bool? value)
        {
            if (TryGetValueFromObject(obj, names, out var raw))
            {
                if (TryConvertBool(raw, out value))
                    return true;
            }

            foreach (var name in names)
            {
                var rawMember = GetMemberValue(obj, name);
                if (TryConvertBool(rawMember, out value))
                    return true;
            }

            value = null;
            return false;
        }

        private static bool TryGetStringFromObject(object obj, string[] names, out string? value)
        {
            if (TryGetValueFromObject(obj, names, out var raw))
            {
                if (TryConvertString(raw, out value))
                    return true;
            }

            foreach (var name in names)
            {
                var rawMember = GetMemberValue(obj, name);
                if (TryConvertString(rawMember, out value))
                    return true;
            }

            value = null;
            return false;
        }

        private static object? GetItemProps(object itemObj)
        {
            if (TryGetValueFromDictionary(itemObj, new[] { "Props", "_props" }, out var dictValue))
                return dictValue;

            if (TryGetValueFromJsonElement(itemObj, new[] { "Props", "_props" }, out var jsonValue))
                return jsonValue;

            return GetPropertyValue(itemObj, "Props") ?? GetPropertyValue(itemObj, "_props");
        }

        private static object? GetPropertyValue(object obj, string name)
        {
            return GetMemberValue(obj, name);
        }

        private static PropertyInfo? GetCachedProperty(Type type, string name)
        {
            var key = $"{type.FullName}:{name}";
            if (_itemPropertyCache.TryGetValue(key, out var cached))
                return cached;

            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            _itemPropertyCache[key] = prop;
            return prop;
        }

        private static FieldInfo? GetCachedField(Type type, string name)
        {
            var key = $"{type.FullName}:{name}";
            if (_itemFieldCache.TryGetValue(key, out var cached))
                return cached;

            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            _itemFieldCache[key] = field;
            return field;
        }

        private static object? GetMemberValue(object obj, string name)
        {
            var prop = GetCachedProperty(obj.GetType(), name);
            if (prop != null)
                return prop.GetValue(obj);

            var field = GetCachedField(obj.GetType(), name);
            return field?.GetValue(obj);
        }

        private static bool TryGetValueFromObject(object obj, string[] names, out object? value)
        {
            if (TryGetValueFromDictionary(obj, names, out value))
                return true;

            if (TryGetValueFromJsonElement(obj, names, out value))
                return true;

            value = null;
            return false;
        }

        private static bool TryGetValueFromDictionary(object obj, string[] names, out object? value)
        {
            if (obj is IDictionary dict)
            {
                foreach (var name in names)
                {
                    if (dict.Contains(name))
                    {
                        value = dict[name];
                        return true;
                    }
                }

                foreach (DictionaryEntry entry in dict)
                {
                    if (entry.Key is string key)
                    {
                        foreach (var name in names)
                        {
                            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                            {
                                value = entry.Value;
                                return true;
                            }
                        }
                    }
                }
            }

            value = null;
            return false;
        }

        private static bool TryGetValueFromJsonElement(object obj, string[] names, out object? value)
        {
            if (obj is JsonElement element)
            {
                foreach (var name in names)
                {
                    if (element.TryGetProperty(name, out var prop))
                    {
                        value = prop;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        private static bool TryConvertBool(object? raw, out bool? value)
        {
            if (raw is bool b)
            {
                value = b;
                return true;
            }

            if (raw is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                {
                    value = element.GetBoolean();
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool TryConvertString(object? raw, out string? value)
        {
            if (raw is string s && !string.IsNullOrWhiteSpace(s))
            {
                value = s;
                return true;
            }

            if (raw is JsonElement element && element.ValueKind == JsonValueKind.String)
            {
                var str = element.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    value = str;
                    return true;
                }
            }

            if (raw != null)
            {
                var str = raw.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    value = str;
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// 处理获取跳蚤市场禁售物品列表的请求
        /// </summary>
        private static ValueTask<string> HandleGetRagfairBannedItems(
            string url,
            EmptyRequestData info,
            MongoId sessionId)
        {
            try
            {
                EnsureConfigLoaded();
                if (!IsEnabled())
                    return new ValueTask<string>(JsonSerializer.Serialize(new List<string>()));

                var bannedItems = GetOrBuildRagfairBannedItemIdCache();

                var json = JsonSerializer.Serialize(bannedItems);
                return new ValueTask<string>(json);
            }
            catch (Exception ex)
            {
                _loggerStatic?.Error($"[QuickPrice-RagfairBan] Error in GetRagfairBannedItems: {ex.Message}", ex);
                // Console.WriteLine($"[QuickPrice-RagfairBan] Error in GetRagfairBannedItems: {ex.Message}");
                // 返回空列表
                var fallback = JsonSerializer.Serialize(new List<string>());
                return new ValueTask<string>(fallback);
            }
        }

        #endregion
    }
}
