using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.ConfigurationManager;
using UnityEngine;
using QuickPrice.Logging;
using QuickPrice.Models;

namespace QuickPrice.Config
{
    public static class Settings
    {
        private const string SectionServer = "0. 服务端同步";
        private const string SectionGeneral = "1. 基础开关";
        private const string SectionPriceDisplay = "2. 价格显示";
        private const string SectionInteraction = "3. 提示与交互";
        private const string SectionColorDisplay = "4. 颜色与显示";
        private const string SectionPriceThresholds = "4.1 价格颜色阈值";
        private const string SectionPenetrationThresholds = "4.2 穿甲颜色阈值";
        private const string SectionArmor = "4.3 护甲等级显示";
        private const string SectionThresholdReset = "4.4 阈值重置";
        private const string SectionCache = "5. 价格缓存与刷新";
        private const string SectionContainer = "5.1 容器性能";
        private const string SectionRaidSummary = "5.2 战局结算";
        private const string SectionSearch = "6. 搜索设置";
        private const string SectionDebug = "7. 调试设置";

        private const string LegacySectionServer = "0. 服务端覆盖";
        private const string LegacySectionMain = "1. 主要设置";
        private const string LegacySectionDisplay = "2. 显示设置";
        private const string LegacySectionPriceThresholds = "2.1 价格颜色阈值";
        private const string LegacySectionPenetrationThresholds = "2.2 穿甲颜色阈值";
        private const string LegacySectionArmor = "2.3 护甲等级设置";
        private const string LegacySectionReset = "2.4 重置功能";
        private const string LegacySectionPerformance = "3. 性能设置";
        private const string LegacySectionContainer = "3.1 容器性能优化";
        private const string LegacySectionV2Features = "4. v2.0 新增功能";
        private const string LegacySectionSearch = "4.1 搜索设置";
        private const string LegacySectionDebug = "5. 调试设置";

        private static readonly ConfigurationManagerAttributes ServerOverrideAttributes = new ConfigurationManagerAttributes { ReadOnly = false };
        private static readonly ConfigurationManagerAttributes StatusReadOnlyAttributes = new ConfigurationManagerAttributes { ReadOnly = true };
        private static readonly List<ConfigEntryBase> OverrideEntries = new List<ConfigEntryBase>();
        private static readonly Dictionary<ConfigEntryBase, object> OverrideLocalValues = new Dictionary<ConfigEntryBase, object>();

        // ===== 0. 服务端同步 =====
        public static ConfigEntry<string> ServerConfigStatus;

        // ===== 1. 基础开关 =====
        public static ConfigEntry<bool> PluginEnabled;

        // ===== 2. 价格显示 =====
        public static ConfigEntry<bool> ShowFleaPrices;
        public static ConfigEntry<bool> HideRagfairPriceForNonFIRItems;
        public static ConfigEntry<bool> ShowTraderPrices;      // 显示商人价格
        public static ConfigEntry<bool> ShowFleaTax;            // 显示跳蚤税费
        public static ConfigEntry<bool> ShowPricePerSlot;
        public static ConfigEntry<bool> ShowWeaponModsPrice;
        public static ConfigEntry<bool> ShowDetailedWeaponMods;
        public static ConfigEntry<bool> ShowBestPriceInBold;
        public static ConfigEntry<bool> UseKUnit;
        public static ConfigEntry<int> StackCountUnitPriceThreshold;

        // ===== 3. 提示与交互 =====
        public static ConfigEntry<bool> RequireCtrlKey;
        public static ConfigEntry<float> TooltipDelay;
        public static ConfigEntry<bool> DisableTooltipWidthLimit;
        public static ConfigEntry<bool> ShowTooltipSeparator;

        // ===== 4. 颜色与显示 =====
        public static ConfigEntry<bool> EnableColorCoding;
        public static ConfigEntry<bool> UseCaliberPenetrationPower;
        public static ConfigEntry<bool> ColorItemName;
        public static ConfigEntry<bool> EnablePriceBasedBackgroundColor;
        public static ConfigEntry<bool> ShowGroundItemPrice;  // 显示地面物品价格（跟随物品名称）

        // ===== 4.1 价格颜色阈值 =====
        public static ConfigEntry<int> PriceThreshold1; // 白色→绿色
        public static ConfigEntry<int> PriceThreshold2; // 绿色→蓝色
        public static ConfigEntry<int> PriceThreshold3; // 蓝色→紫色
        public static ConfigEntry<int> PriceThreshold4; // 紫色→橙色
        public static ConfigEntry<int> PriceThreshold5; // 橙色→红色

        // ===== 4.2 穿甲颜色阈值 =====
        public static ConfigEntry<int> PenetrationThreshold1; // 白色→绿色
        public static ConfigEntry<int> PenetrationThreshold2; // 绿色→蓝色
        public static ConfigEntry<int> PenetrationThreshold3; // 蓝色→紫色
        public static ConfigEntry<int> PenetrationThreshold4; // 紫色→橙色
        public static ConfigEntry<int> PenetrationThreshold5; // 橙色→红色

        // ===== 4.3 护甲等级显示 =====
        public static ConfigEntry<bool> EnableArmorClassColoring; // 启用护甲等级着色
        public static ConfigEntry<bool> ShowArmorClass; // 显示护甲等级文字

        // ===== 4.4 阈值重置 =====
        public static ConfigEntry<string> ResetThresholdsButton; // 重置阈值按钮

        // ===== 5. 价格缓存与刷新 =====
        public static ConfigEntry<bool> UseDynamicPrices;
        public static ConfigEntry<CacheMode> PriceCacheMode;    // v2.0: 缓存模式
        public static ConfigEntry<bool> AutoRefreshOnOpenInventory; // 打开物品栏自动刷新
        public static ConfigEntry<KeyCode> RefreshPricesKey;   // 刷新价格快捷键

        // ===== 5.1 容器性能 =====
        public static ConfigEntry<bool> EnableContainerPriceCalculation; // 启用容器内物品价格计算
        public static ConfigEntry<int> MaxContainerDepth;        // 最大递归深度
        public static ConfigEntry<int> MaxContainerItems;        // 最大计算物品数
        public static ConfigEntry<bool> SkipLargeContainers;     // 跳过大容器
        public static ConfigEntry<int> LargeContainerThreshold;  // 大容器阈值

        // ===== 5.2 战局结算 =====
        public static ConfigEntry<bool> ExcludeSecuredContainerFromBroughtValue; // 带入价值排除保险箱
        public static ConfigEntry<bool> ExcludeKnifeFromBroughtValue; // 带入价值排除刀具
        public static ConfigEntry<bool> ExcludeArmBandFromBroughtValue; // 带入价值排除臂带
        public static ConfigEntry<bool> ExcludeDogtagFromBroughtValue; // 带入价值排除狗牌
        public static ConfigEntry<bool> ExcludeSpecialSlotsFromBroughtValue; // 带入价值排除特殊装备栏
        public static ConfigEntry<bool> ShowBroughtValueInRaid; // 战局内显示带入价值
        public static ConfigEntry<bool> ShowLossValueInRaid; // 战局内显示损耗价值

        // ===== 6. 搜索设置 =====
        public static ConfigEntry<bool> EnableSearchSound;    // 搜索音效开关
        public static ConfigEntry<bool> EnableSearchTimeAdjustment; // 搜索时间调整开关
        public static ConfigEntry<float> SearchTimeRandomMin; // 搜索随机延迟最小值
        public static ConfigEntry<float> SearchTimeRandomMax; // 搜索随机延迟最大值
        public static ConfigEntry<float> SearchTimeLevel1;    // 搜索时间：品质等级1
        public static ConfigEntry<float> SearchTimeLevel2;    // 搜索时间：品质等级2
        public static ConfigEntry<float> SearchTimeLevel3;    // 搜索时间：品质等级3
        public static ConfigEntry<float> SearchTimeLevel4;    // 搜索时间：品质等级4
        public static ConfigEntry<float> SearchTimeLevel5;    // 搜索时间：品质等级5
        public static ConfigEntry<float> SearchTimeLevel6;    // 搜索时间：品质等级6

        // ===== 7. 调试设置 =====
        public static ConfigEntry<bool> EnableDebugLogs;      // 调试日志开关

        // 默认阈值常量
        private const int DEFAULT_PRICE_THRESHOLD_1 = 25000;
        private const int DEFAULT_PRICE_THRESHOLD_2 = 45000;
        private const int DEFAULT_PRICE_THRESHOLD_3 = 70000;
        private const int DEFAULT_PRICE_THRESHOLD_4 = 100000;
        private const int DEFAULT_PRICE_THRESHOLD_5 = 250000;

        private const int DEFAULT_PENETRATION_THRESHOLD_1 = 20;
        private const int DEFAULT_PENETRATION_THRESHOLD_2 = 30;
        private const int DEFAULT_PENETRATION_THRESHOLD_3 = 40;
        private const int DEFAULT_PENETRATION_THRESHOLD_4 = 50;
        private const int DEFAULT_PENETRATION_THRESHOLD_5 = 60;

        private static ConfigFile _configFile; // 保存 ConfigFile 引用用于重置
        private static bool _serverOverrideEnabled;
        private static ServerClientConfig _serverConfig;
        private static bool _isApplyingOverrideValues;
        private static Type _configManagerType;

        // 缓存模式枚举
        public enum CacheMode
        {
            Permanent,      // 永久缓存（启动时加载，不再刷新）
            FiveMinutes,    // 5分钟自动过期
            TenMinutes,     // 10分钟自动过期
            Manual          // 仅手动刷新
        }

        public static void Init(ConfigFile config)
        {
            // 保存 ConfigFile 引用用于重置功能
            _configFile = config;

            // ===== 0. 服务端同步 =====
            ServerConfigStatus = BindWithLegacy(
                config,
                SectionServer,
                "服务器配置状态（只读）",
                "使用本地配置",
                CreateStatusDescription("显示当前是否使用服务器配置覆盖本地参数"),
                LegacySectionServer
            );

            // ===== 1. 基础开关 =====
            PluginEnabled = BindWithLegacy(
                config,
                SectionGeneral,
                "启用插件",
                true,
                "是否启用 QuickPrice 插件",
                LegacySectionMain
            );

            // ===== 2. 价格显示 =====
            ShowFleaPrices = BindWithLegacy(
                config,
                SectionPriceDisplay,
                "显示跳蚤市场价格",
                true,
                "在物品提示框中显示跳蚤市场价格",
                LegacySectionMain
            );

            HideRagfairPriceForNonFIRItems = BindWithLegacy(
                config,
                SectionPriceDisplay,
                "非发现物隐藏跳蚤价格",
                false,
                "物品未标记为战局发现时隐藏跳蚤价格\n" +
                "单格价值改用商人价格计算",
                LegacySectionMain
            );

            ShowPricePerSlot = BindWithLegacy(
                config,
                SectionPriceDisplay,
                "显示每格价格",
                true,
                "显示物品的单格位价格（价格/格数）",
                LegacySectionMain
            );

            ShowWeaponModsPrice = BindWithLegacy(
                config,
                SectionPriceDisplay,
                "显示武器配件价格",
                true,
                "显示武器所有配件的总价值（递归计算所有层级配件）",
                LegacySectionMain
            );

            ShowDetailedWeaponMods = BindWithLegacy(
                config,
                SectionPriceDisplay,
                "显示配件详细列表",
                false,
                "显示所有配件的层级结构和价格\n" +
                "以缩进树状结构显示配件及其子配件",
                LegacySectionMain
            );

            ShowBestPriceInBold = BindWithLegacy(
                config,
                SectionPriceDisplay,
                "最佳价格加粗",
                true,
                "用粗体突出显示最高价格",
                LegacySectionDisplay
            );

            UseKUnit = BindWithLegacy(
                config,
                SectionPriceDisplay,
                "启用K单位显示",
                false,
                "价格 ≥ 10,000 时显示为 ₽10k / ₽10.5k\n" +
                "价格 < 10,000 保持默认格式\n" +
                "自动四舍五入到 1 位小数，去掉多余的 .0",
                LegacySectionDisplay
            );

            StackCountUnitPriceThreshold = BindWithLegacy(
                config,
                SectionPriceDisplay,
                "堆叠数量按单价显示阈值",
                1000,
                new ConfigDescription(
                    "堆叠数量超过该阈值时仅显示单价，避免商店库存导致总价异常",
                    new AcceptableValueRange<int>(1, 1000000)
                ),
                LegacySectionDisplay
            );

            // ===== 3. 提示与交互 =====
            RequireCtrlKey = BindWithLegacy(
                config,
                SectionInteraction,
                "按住Ctrl键才显示",
                false,
                "需要按住Ctrl键（左Ctrl或右Ctrl）才显示价格\n" +
                "关闭此选项则鼠标悬停即显示价格",
                LegacySectionMain
            );

            TooltipDelay = BindWithLegacy(
                config,
                SectionInteraction,
                "提示框延迟（秒）",
                0.0f,
                new ConfigDescription(
                    "鼠标悬停后多久显示价格提示框（0 = 立即显示）",
                    new AcceptableValueRange<float>(0f, 2f)
                ),
                LegacySectionMain
            );

            DisableTooltipWidthLimit = BindWithLegacy(
                config,
                SectionInteraction,
                "取消提示框宽度限制",
                true,
                "取消物品提示框的固定宽度限制，避免长文本自动换行\n" +
                "⚠️ 可能导致提示框超出屏幕边界",
                LegacySectionMain
            );

            ShowTooltipSeparator = BindWithLegacy(
                config,
                SectionInteraction,
                "显示提示框下划线",
                true,
                "在物品名称下方显示提示框分隔线（下划线）",
                LegacySectionMain
            );

            // ===== 4. 颜色与显示 =====
            EnableColorCoding = BindWithLegacy(
                config,
                SectionColorDisplay,
                "启用颜色编码",
                true,
                "根据价格自动着色物品名称\n" +
                "白色≤3千 | 绿色≤1万 | 蓝色≤2万 | 紫色≤5万 | 橙色≤10万 | 红色>10万",
                LegacySectionDisplay
            );

            UseCaliberPenetrationPower = BindWithLegacy(
                config,
                SectionColorDisplay,
                "子弹按穿甲等级着色",
                true,
                "子弹和弹药盒使用穿甲等级着色，而不是价格着色\n" +
                "颜色等级：白色<15 | 绿色<25 | 蓝色<35 | 紫色<45 | 橙色<55 | 红色≥55",
                LegacySectionDisplay
            );

            ColorItemName = BindWithLegacy(
                config,
                SectionColorDisplay,
                "物品名称着色",
                true,
                "根据物品价值或穿甲等级给物品名称着色\n" +
                "✅ 适用范围：物品栏提示框 + 战局内地面散落物品\n" +
                "普通物品/武器按价格着色 | 子弹/弹匣按穿甲等级着色 | 护甲按防弹等级着色",
                LegacySectionDisplay
            );

            EnablePriceBasedBackgroundColor = BindWithLegacy(
                config,
                SectionColorDisplay,
                "自动着色物品背景",
                true,
                "根据物品价格自动修改物品单元格背景颜色\n" +
                "无需鼠标悬停，打开物品栏即可看到所有物品已着色\n" +
                "使用与物品名称相同的价格阈值配置\n" +
                "注意：可能与 ColorConverterAPI 等其他颜色插件冲突",
                LegacySectionDisplay
            );

            ShowGroundItemPrice = BindWithLegacy(
                config,
                SectionColorDisplay,
                "地面物品显示价格",
                true,
                "在战局内地面散落物品名称后显示价格\n" +
                "格式：物品名称 (单格价值₽) 或 物品名称 (总价₽)\n" +
                "颜色会根据单格价值自动调整\n" +
                "注意：需要启用\"物品名称着色\"选项才会生效",
                LegacySectionDisplay
            );

            // ===== 4.1 价格颜色阈值 =====
            PriceThreshold1 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionPriceThresholds,
                "阈值1 - 白色→绿色",
                DEFAULT_PRICE_THRESHOLD_1,
                CreateOverrideDescription(
                    "价格 ≤ 此值显示白色，> 此值显示绿色或更高等级",
                    new AcceptableValueRange<int>(0, 1000000)
                ),
                LegacySectionPriceThresholds
            ));

            PriceThreshold2 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionPriceThresholds,
                "阈值2 - 绿色→蓝色",
                DEFAULT_PRICE_THRESHOLD_2,
                CreateOverrideDescription(
                    "价格 ≤ 此值显示绿色，> 此值显示蓝色或更高等级",
                    new AcceptableValueRange<int>(0, 1000000)
                ),
                LegacySectionPriceThresholds
            ));

            PriceThreshold3 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionPriceThresholds,
                "阈值3 - 蓝色→紫色",
                DEFAULT_PRICE_THRESHOLD_3,
                CreateOverrideDescription(
                    "价格 ≤ 此值显示蓝色，> 此值显示紫色或更高等级",
                    new AcceptableValueRange<int>(0, 1000000)
                ),
                LegacySectionPriceThresholds
            ));

            PriceThreshold4 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionPriceThresholds,
                "阈值4 - 紫色→橙色",
                DEFAULT_PRICE_THRESHOLD_4,
                CreateOverrideDescription(
                    "价格 ≤ 此值显示紫色，> 此值显示橙色或更高等级",
                    new AcceptableValueRange<int>(0, 1000000)
                ),
                LegacySectionPriceThresholds
            ));

            PriceThreshold5 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionPriceThresholds,
                "阈值5 - 橙色→红色",
                DEFAULT_PRICE_THRESHOLD_5,
                CreateOverrideDescription(
                    "价格 ≤ 此值显示橙色，> 此值显示红色",
                    new AcceptableValueRange<int>(0, 1000000)
                ),
                LegacySectionPriceThresholds
            ));

            // ===== 4.2 穿甲颜色阈值 =====
            PenetrationThreshold1 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionPenetrationThresholds,
                "阈值1 - 白色→绿色",
                DEFAULT_PENETRATION_THRESHOLD_1,
                CreateOverrideDescription(
                    "穿甲值 < 此值显示白色，≥ 此值显示绿色或更高等级",
                    new AcceptableValueRange<int>(0, 100)
                ),
                LegacySectionPenetrationThresholds
            ));

            PenetrationThreshold2 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionPenetrationThresholds,
                "阈值2 - 绿色→蓝色",
                DEFAULT_PENETRATION_THRESHOLD_2,
                CreateOverrideDescription(
                    "穿甲值 < 此值显示绿色，≥ 此值显示蓝色或更高等级",
                    new AcceptableValueRange<int>(0, 100)
                ),
                LegacySectionPenetrationThresholds
            ));

            PenetrationThreshold3 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionPenetrationThresholds,
                "阈值3 - 蓝色→紫色",
                DEFAULT_PENETRATION_THRESHOLD_3,
                CreateOverrideDescription(
                    "穿甲值 < 此值显示蓝色，≥ 此值显示紫色或更高等级",
                    new AcceptableValueRange<int>(0, 100)
                ),
                LegacySectionPenetrationThresholds
            ));

            PenetrationThreshold4 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionPenetrationThresholds,
                "阈值4 - 紫色→橙色",
                DEFAULT_PENETRATION_THRESHOLD_4,
                CreateOverrideDescription(
                    "穿甲值 < 此值显示紫色，≥ 此值显示橙色或更高等级",
                    new AcceptableValueRange<int>(0, 100)
                ),
                LegacySectionPenetrationThresholds
            ));

            PenetrationThreshold5 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionPenetrationThresholds,
                "阈值5 - 橙色→红色",
                DEFAULT_PENETRATION_THRESHOLD_5,
                CreateOverrideDescription(
                    "穿甲值 < 此值显示橙色，≥ 此值显示红色",
                    new AcceptableValueRange<int>(0, 100)
                ),
                LegacySectionPenetrationThresholds
            ));

            // ===== 4.3 护甲等级显示 =====
            EnableArmorClassColoring = BindWithLegacy(
                config,
                SectionArmor,
                "启用护甲等级着色",
                true,
                "根据护甲防弹等级（1-6级）自动着色护甲背景和名称\n" +
                "1级=灰色 | 2级=绿色 | 3级=蓝色 | 4级=紫色 | 5级=橙色 | 6级=红色\n" +
                "自动检测护甲本体和内部防弹插板，取最高等级",
                LegacySectionArmor
            );

            ShowArmorClass = BindWithLegacy(
                config,
                SectionArmor,
                "显示护甲等级文字",
                true,
                "在护甲提示框中显示防弹等级（例如：防弹等级: 4级）\n" +
                "自动检测可拆卸防弹插板和内置防弹内衬",
                LegacySectionArmor
            );

            // ===== 4.4 阈值重置 =====
            ResetThresholdsButton = BindWithLegacy(
                config,
                SectionThresholdReset,
                "点击重置所有阈值",
                "点击按钮重置",
                "点击下方按钮将所有价格和穿甲阈值重置为默认值\n" +
                "⚠️ 重置后立即生效，会覆盖您的自定义配置\n" +
                "💡 提示：在配置管理器(F12)中，修改此项的值即可触发重置",
                LegacySectionReset
            );

            // 监听重置按钮的值变化
            ResetThresholdsButton.SettingChanged += (sender, args) =>
            {
                // 当配置值改变时，触发重置
                ResetPriceThresholds();
                // Plugin.Log.LogInfo("===========================================");
                // Plugin.Log.LogInfo("  ✅ 阈值已重置为默认值！");
                // Plugin.Log.LogInfo($"  📊 价格阈值: {DEFAULT_PRICE_THRESHOLD_1 / 1000}K / {DEFAULT_PRICE_THRESHOLD_2 / 1000}K / {DEFAULT_PRICE_THRESHOLD_3 / 1000}K / {DEFAULT_PRICE_THRESHOLD_4 / 1000}K / {DEFAULT_PRICE_THRESHOLD_5 / 1000}K");
                // Plugin.Log.LogInfo($"  🎯 穿甲阈值: {DEFAULT_PENETRATION_THRESHOLD_1} / {DEFAULT_PENETRATION_THRESHOLD_2} / {DEFAULT_PENETRATION_THRESHOLD_3} / {DEFAULT_PENETRATION_THRESHOLD_4} / {DEFAULT_PENETRATION_THRESHOLD_5}");
                // Plugin.Log.LogInfo("  💾 配置已保存，立即生效");
                // Plugin.Log.LogInfo("===========================================");
            };

            // ===== 5. 价格缓存与刷新 =====
            UseDynamicPrices = BindWithLegacy(
                config,
                SectionCache,
                "使用动态价格",
                true,  // 默认开启动态价格
                "✅ 推荐：开启（使用跳蚤市场实时价格，更准确）\n" +
                "动态价格：从跳蚤市场实时获取（更准确但加载慢）\n" +
                "静态价格：使用游戏基础价格（快速但可能略有偏差）\n" +
                "⚠️ 动态价格需要查询数千个物品，首次加载可能需要30-60秒",
                LegacySectionPerformance
            );

            PriceCacheMode = BindWithLegacy(
                config,
                SectionCache,
                "价格缓存模式",
                CacheMode.Permanent,
                "永久缓存：启动时加载一次，不再刷新（推荐）\n" +
                "5分钟刷新：缓存5分钟后自动过期\n" +
                "10分钟刷新：缓存10分钟后自动过期\n" +
                "仅手动刷新：只在手动触发时刷新\n" +
                "⚠️ 自动刷新可能导致短暂卡顿",
                LegacySectionPerformance
            );

            // ===== 5.1 容器性能 =====
            EnableContainerPriceCalculation = BindWithLegacy(
                config,
                SectionContainer,
                "启用容器内物品价格计算",
                true,
                "是否计算容器（背包、箱子等）内部物品的价格\n" +
                "✅ 启用：显示「容器价值 + 内部物品价值」的总价\n" +
                "❌ 禁用：仅显示容器本身的价格，完全跳过内部物品计算\n" +
                "⚠️ 如果您的容器物品很多导致卡顿，建议禁用此选项\n" +
                "推荐：如果经常卡顿则禁用，否则启用",
                LegacySectionContainer
            );

            MaxContainerDepth = BindWithLegacy(
                config,
                SectionContainer,
                "最大递归深度",
                10,
                new ConfigDescription(
                    "容器嵌套计算的最大深度（背包套娃层数限制）\n" +
                    "默认值 10 层，原版为 50 层\n" +
                    "降低此值可显著提升性能，但可能影响深层嵌套容器的准确性\n" +
                    "推荐值：10-20\n" +
                    "⚠️ 仅在「启用容器内物品价格计算」为 true 时有效",
                    new AcceptableValueRange<int>(1, 50)
                ),
                LegacySectionContainer
            );

            MaxContainerItems = BindWithLegacy(
                config,
                SectionContainer,
                "最大计算物品数",
                100,
                new ConfigDescription(
                    "单个容器最多计算多少个物品的价格\n" +
                    "超过此数量将停止计算并显示警告\n" +
                    "默认值 100，设置为 0 表示无限制\n" +
                    "推荐值：50-100\n" +
                    "⚠️ 仅在「启用容器内物品价格计算」为 true 时有效",
                    new AcceptableValueRange<int>(0, 500)
                ),
                LegacySectionContainer
            );

            SkipLargeContainers = BindWithLegacy(
                config,
                SectionContainer,
                "跳过大容器计算",
                true,
                "当容器内物品数量超过阈值时，跳过详细价格计算\n" +
                "仅显示容器本身价格，避免卡顿\n" +
                "推荐：启用（可避免大型物品箱卡顿）\n" +
                "⚠️ 仅在「启用容器内物品价格计算」为 true 时有效",
                LegacySectionContainer
            );

            LargeContainerThreshold = BindWithLegacy(
                config,
                SectionContainer,
                "大容器物品数阈值",
                50,
                new ConfigDescription(
                    "当容器内物品数量超过此值时，视为「大容器」\n" +
                    "如果启用了「跳过大容器计算」，将跳过详细计算\n" +
                    "⚠️ 修复：原默认值 150 太高，改为 50 更合理\n" +
                    "推荐值：30-100\n" +
                    "⚠️ 仅在「启用容器内物品价格计算」为 true 时有效",
                    new AcceptableValueRange<int>(10, 500)
                ),
                LegacySectionContainer
            );

            // ===== 5.2 战局结算 =====
            ExcludeSecuredContainerFromBroughtValue = BindWithLegacy(
                config,
                SectionRaidSummary,
                "带入价值排除保险箱",
                true,
                "计算带入价值时不包含保险箱本体，但仍计入内部物品\n" +
                "⚠️ 仅影响带入/损耗计算，不影响战局收获统计",
                LegacySectionV2Features
            );

            ExcludeKnifeFromBroughtValue = BindWithLegacy(
                config,
                SectionRaidSummary,
                "带入价值排除刀具",
                true,
                "计算带入价值时不包含刀具（Scabbard 槽位）\n" +
                "⚠️ 仅影响带入/损耗计算，不影响战局收获统计",
                LegacySectionV2Features
            );

            ExcludeArmBandFromBroughtValue = BindWithLegacy(
                config,
                SectionRaidSummary,
                "带入价值排除臂带",
                true,
                "计算带入价值时不包含臂带（ArmBand 槽位）\n" +
                "⚠️ 仅影响带入/损耗计算，不影响战局收获统计",
                LegacySectionV2Features
            );

            ExcludeDogtagFromBroughtValue = BindWithLegacy(
                config,
                SectionRaidSummary,
                "带入价值排除狗牌",
                true,
                "计算带入价值时不包含狗牌（Dogtag 槽位）\n" +
                "⚠️ 仅影响带入/损耗计算，不影响战局收获统计",
                LegacySectionV2Features
            );

            ExcludeSpecialSlotsFromBroughtValue = BindWithLegacy(
                config,
                SectionRaidSummary,
                "带入价值排除特殊装备栏",
                true,
                "计算带入价值时不包含特殊装备栏（例如指南针等）\n" +
                "⚠️ 仅影响带入/损耗计算，不影响战局收获统计",
                LegacySectionV2Features
            );

            ShowBroughtValueInRaid = BindWithLegacy(
                config,
                SectionRaidSummary,
                "战局内显示带入价值",
                false,
                "战局内结算面板是否显示带入价值",
                LegacySectionV2Features
            );

            ShowLossValueInRaid = BindWithLegacy(
                config,
                SectionRaidSummary,
                "战局内显示损耗价值",
                false,
                "战局内结算面板是否显示损耗价值",
                LegacySectionV2Features
            );

            // ===== 2. 价格显示 =====
            ShowTraderPrices = BindWithLegacy(
                config,
                SectionPriceDisplay,
                "显示商人价格",
                true,
                "显示所有商人的收购价格，并与跳蚤价格对比\n" +
                "自动显示最佳价格（商人 vs 跳蚤市场）\n" +
                "⚠️ 注意：首次启动游戏后，需要先打开一次商人界面（如 Prapor）来初始化商人数据",
                LegacySectionV2Features
            );

            ShowFleaTax = BindWithLegacy(
                config,
                SectionPriceDisplay,
                "显示跳蚤税费",
                true,
                "显示在跳蚤市场出售物品需要支付的税费\n" +
                "包含税后净利润计算",
                LegacySectionV2Features
            );

            // ===== 5. 价格缓存与刷新 =====
            AutoRefreshOnOpenInventory = BindWithLegacy(
                config,
                SectionCache,
                "打开物品栏自动刷新",
                true,
                "打开物品栏时如果缓存过期则自动异步刷新价格数据\n" +
                "不阻塞界面，后台更新\n" +
                "⚠️ 仅在缓存模式为「5分钟刷新」或「10分钟刷新」时有效\n" +
                "永久缓存模式下此选项无效",
                LegacySectionV2Features
            );

            RefreshPricesKey = BindWithLegacy(
                config,
                SectionCache,
                "刷新价格快捷键",
                KeyCode.F10,
                "按此键立即强制刷新跳蚤市场价格缓存\n" +
                "默认快捷键: F10\n" +
                "刷新过程异步进行，不会阻塞游戏\n" +
                "适用于动态价格模式，可随时获取最新跳蚤市场价格\n" +
                "💡 配合永久缓存模式使用，需要更新价格时手动刷新",
                LegacySectionV2Features
            );

            // ===== 6. 搜索设置 =====
            EnableSearchSound = BindWithLegacy(
                config,
                SectionSearch,
                "启用搜索音效",
                false,
                "根据物品价值等级播放不同的搜索音效\n" +
                "关闭后使用游戏原始搜索音效",
                LegacySectionSearch
            );

            EnableSearchTimeAdjustment = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionSearch,
                "启用搜索时间调整",
                false,
                CreateOverrideDescription(
                    "根据物品价值等级调整搜索时间（高价值耗时更长）\n" +
                    "关闭后使用游戏原始搜索时间"
                ),
                LegacySectionSearch
            ));

            SearchTimeRandomMin = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionSearch,
                "搜索随机延迟最小值（秒）",
                0f,
                CreateOverrideDescription(
                    "在基础搜索时间上增加随机延迟的最小值\n" +
                    "仅在启用搜索时间调整时生效",
                    new AcceptableValueRange<float>(0f, 10f)
                ),
                LegacySectionSearch
            ));

            SearchTimeRandomMax = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionSearch,
                "搜索随机延迟最大值（秒）",
                1f,
                CreateOverrideDescription(
                    "在基础搜索时间上增加随机延迟的最大值\n" +
                    "仅在启用搜索时间调整时生效\n" +
                    "如果最大值小于最小值，将自动交换",
                    new AcceptableValueRange<float>(0f, 10f)
                ),
                LegacySectionSearch
            ));

            SearchTimeLevel1 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionSearch,
                "品质等级1搜索时间（秒）",
                1f,
                CreateOverrideDescription(
                    "最低价值等级的搜索时间\n" +
                    "仅在启用搜索时间调整时生效",
                    new AcceptableValueRange<float>(0.1f, 30f)
                ),
                LegacySectionSearch
            ));

            SearchTimeLevel2 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionSearch,
                "品质等级2搜索时间（秒）",
                2f,
                CreateOverrideDescription(
                    "较低价值等级的搜索时间\n" +
                    "仅在启用搜索时间调整时生效",
                    new AcceptableValueRange<float>(0.1f, 30f)
                ),
                LegacySectionSearch
            ));

            SearchTimeLevel3 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionSearch,
                "品质等级3搜索时间（秒）",
                3f,
                CreateOverrideDescription(
                    "中等价值等级的搜索时间\n" +
                    "仅在启用搜索时间调整时生效",
                    new AcceptableValueRange<float>(0.1f, 30f)
                ),
                LegacySectionSearch
            ));

            SearchTimeLevel4 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionSearch,
                "品质等级4搜索时间（秒）",
                4f,
                CreateOverrideDescription(
                    "较高价值等级的搜索时间\n" +
                    "仅在启用搜索时间调整时生效",
                    new AcceptableValueRange<float>(0.1f, 30f)
                ),
                LegacySectionSearch
            ));

            SearchTimeLevel5 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionSearch,
                "品质等级5搜索时间（秒）",
                5f,
                CreateOverrideDescription(
                    "高价值等级的搜索时间\n" +
                    "仅在启用搜索时间调整时生效",
                    new AcceptableValueRange<float>(0.1f, 30f)
                ),
                LegacySectionSearch
            ));

            SearchTimeLevel6 = RegisterOverrideEntry(BindWithLegacy(
                config,
                SectionSearch,
                "品质等级6搜索时间（秒）",
                6f,
                CreateOverrideDescription(
                    "最高价值等级的搜索时间\n" +
                    "仅在启用搜索时间调整时生效",
                    new AcceptableValueRange<float>(0.1f, 30f)
                ),
                LegacySectionSearch
            ));

            // ===== 7. 调试设置 =====
            EnableDebugLogs = BindWithLegacy(
                config,
                SectionDebug,
                "启用Debug日志",
                false,
                "开启后输出Debug/Warning级别日志（用于排查问题）\n" +
                "默认关闭以减少客户端日志量",
                LegacySectionDebug
            );

            ClientLog.SetDebugEnabled(EnableDebugLogs.Value);
            EnableDebugLogs.SettingChanged += (sender, args) =>
            {
                ClientLog.SetDebugEnabled(EnableDebugLogs.Value);
            };
        }

        /// <summary>
        /// 重置所有价格和穿甲阈值为默认值
        /// </summary>
        public static void ResetPriceThresholds()
        {
            // 重置价格阈值
            PriceThreshold1.Value = DEFAULT_PRICE_THRESHOLD_1;
            PriceThreshold2.Value = DEFAULT_PRICE_THRESHOLD_2;
            PriceThreshold3.Value = DEFAULT_PRICE_THRESHOLD_3;
            PriceThreshold4.Value = DEFAULT_PRICE_THRESHOLD_4;
            PriceThreshold5.Value = DEFAULT_PRICE_THRESHOLD_5;

            // 重置穿甲阈值
            PenetrationThreshold1.Value = DEFAULT_PENETRATION_THRESHOLD_1;
            PenetrationThreshold2.Value = DEFAULT_PENETRATION_THRESHOLD_2;
            PenetrationThreshold3.Value = DEFAULT_PENETRATION_THRESHOLD_3;
            PenetrationThreshold4.Value = DEFAULT_PENETRATION_THRESHOLD_4;
            PenetrationThreshold5.Value = DEFAULT_PENETRATION_THRESHOLD_5;

            // 保存配置文件
            _configFile?.Save();
        }

        public static bool ApplyServerConfigOverrides(ServerClientConfig serverConfig)
        {
            if (serverConfig == null)
                return false;

            _serverConfig = serverConfig;
            bool overrideEnabled = serverConfig.OverrideClientConfig;

            if (overrideEnabled)
            {
                if (!_serverOverrideEnabled)
                {
                    CaptureLocalOverrideValues();
                }

                ApplyOverrideValues(serverConfig);
            }
            else if (_serverOverrideEnabled)
            {
                RestoreLocalOverrideValues();
            }

            _serverOverrideEnabled = overrideEnabled;
            SetOverrideReadOnly(_serverOverrideEnabled);
            UpdateServerConfigStatus();
            return _serverOverrideEnabled;
        }

        public static bool IsServerOverrideEnabled() => _serverOverrideEnabled;

        public static int GetPriceThreshold1() => GetOverrideValue(config => config.PriceThreshold1, PriceThreshold1.Value);
        public static int GetPriceThreshold2() => GetOverrideValue(config => config.PriceThreshold2, PriceThreshold2.Value);
        public static int GetPriceThreshold3() => GetOverrideValue(config => config.PriceThreshold3, PriceThreshold3.Value);
        public static int GetPriceThreshold4() => GetOverrideValue(config => config.PriceThreshold4, PriceThreshold4.Value);
        public static int GetPriceThreshold5() => GetOverrideValue(config => config.PriceThreshold5, PriceThreshold5.Value);

        public static int GetPenetrationThreshold1() => GetOverrideValue(config => config.PenetrationThreshold1, PenetrationThreshold1.Value);
        public static int GetPenetrationThreshold2() => GetOverrideValue(config => config.PenetrationThreshold2, PenetrationThreshold2.Value);
        public static int GetPenetrationThreshold3() => GetOverrideValue(config => config.PenetrationThreshold3, PenetrationThreshold3.Value);
        public static int GetPenetrationThreshold4() => GetOverrideValue(config => config.PenetrationThreshold4, PenetrationThreshold4.Value);
        public static int GetPenetrationThreshold5() => GetOverrideValue(config => config.PenetrationThreshold5, PenetrationThreshold5.Value);

        public static bool GetEnableSearchTimeAdjustment() => GetOverrideValue(config => config.EnableSearchTimeAdjustment, EnableSearchTimeAdjustment.Value);
        public static float GetSearchTimeRandomMin() => GetOverrideValue(config => config.SearchTimeRandomMin, SearchTimeRandomMin.Value);
        public static float GetSearchTimeRandomMax() => GetOverrideValue(config => config.SearchTimeRandomMax, SearchTimeRandomMax.Value);
        public static float GetSearchTimeLevel1() => GetOverrideValue(config => config.SearchTimeLevel1, SearchTimeLevel1.Value);
        public static float GetSearchTimeLevel2() => GetOverrideValue(config => config.SearchTimeLevel2, SearchTimeLevel2.Value);
        public static float GetSearchTimeLevel3() => GetOverrideValue(config => config.SearchTimeLevel3, SearchTimeLevel3.Value);
        public static float GetSearchTimeLevel4() => GetOverrideValue(config => config.SearchTimeLevel4, SearchTimeLevel4.Value);
        public static float GetSearchTimeLevel5() => GetOverrideValue(config => config.SearchTimeLevel5, SearchTimeLevel5.Value);
        public static float GetSearchTimeLevel6() => GetOverrideValue(config => config.SearchTimeLevel6, SearchTimeLevel6.Value);

        private static ConfigEntry<T> RegisterOverrideEntry<T>(ConfigEntry<T> entry)
        {
            if (entry != null && !OverrideEntries.Contains(entry))
            {
                OverrideEntries.Add(entry);
            }

            return entry;
        }

        private static void CaptureLocalOverrideValues()
        {
            if (OverrideLocalValues.Count > 0)
                return;

            foreach (var entry in OverrideEntries)
            {
                OverrideLocalValues[entry] = entry.BoxedValue;
            }
        }

        private static void ApplyOverrideValues(ServerClientConfig serverConfig)
        {
            if (serverConfig == null)
                return;

            if (_isApplyingOverrideValues)
                return;

            _isApplyingOverrideValues = true;
            try
            {
                RunWithoutSaving(() =>
                {
                    PriceThreshold1.Value = serverConfig.PriceThreshold1;
                    PriceThreshold2.Value = serverConfig.PriceThreshold2;
                    PriceThreshold3.Value = serverConfig.PriceThreshold3;
                    PriceThreshold4.Value = serverConfig.PriceThreshold4;
                    PriceThreshold5.Value = serverConfig.PriceThreshold5;

                    PenetrationThreshold1.Value = serverConfig.PenetrationThreshold1;
                    PenetrationThreshold2.Value = serverConfig.PenetrationThreshold2;
                    PenetrationThreshold3.Value = serverConfig.PenetrationThreshold3;
                    PenetrationThreshold4.Value = serverConfig.PenetrationThreshold4;
                    PenetrationThreshold5.Value = serverConfig.PenetrationThreshold5;

                    EnableSearchTimeAdjustment.Value = serverConfig.EnableSearchTimeAdjustment;
                    SearchTimeRandomMin.Value = serverConfig.SearchTimeRandomMin;
                    SearchTimeRandomMax.Value = serverConfig.SearchTimeRandomMax;
                    SearchTimeLevel1.Value = serverConfig.SearchTimeLevel1;
                    SearchTimeLevel2.Value = serverConfig.SearchTimeLevel2;
                    SearchTimeLevel3.Value = serverConfig.SearchTimeLevel3;
                    SearchTimeLevel4.Value = serverConfig.SearchTimeLevel4;
                    SearchTimeLevel5.Value = serverConfig.SearchTimeLevel5;
                    SearchTimeLevel6.Value = serverConfig.SearchTimeLevel6;
                });
            }
            finally
            {
                _isApplyingOverrideValues = false;
            }
        }

        private static void RestoreLocalOverrideValues()
        {
            if (OverrideLocalValues.Count == 0)
                return;

            if (_isApplyingOverrideValues)
                return;

            _isApplyingOverrideValues = true;
            try
            {
                RunWithoutSaving(() =>
                {
                    foreach (var entry in OverrideLocalValues)
                    {
                        entry.Key.BoxedValue = entry.Value;
                    }
                });
            }
            finally
            {
                _isApplyingOverrideValues = false;
            }

            OverrideLocalValues.Clear();
        }

        private static void SetOverrideReadOnly(bool readOnly)
        {
            ServerOverrideAttributes.ReadOnly = readOnly;
            UpdateOverrideEntryHandlers(readOnly);
            RefreshConfigManagerUi();
        }

        private static void UpdateOverrideEntryHandlers(bool readOnly)
        {
            if (_configFile == null)
                return;

            _configFile.SettingChanged -= OnConfigSettingChanged;
            if (readOnly)
            {
                _configFile.SettingChanged += OnConfigSettingChanged;
            }
        }

        private static void OnConfigSettingChanged(object sender, SettingChangedEventArgs args)
        {
            if (args?.ChangedSetting == null)
                return;

            if (!OverrideEntries.Contains(args.ChangedSetting))
                return;

            OnOverrideEntrySettingChanged(sender, EventArgs.Empty);
        }

        private static void OnOverrideEntrySettingChanged(object sender, EventArgs args)
        {
            if (_isApplyingOverrideValues || !_serverOverrideEnabled || _serverConfig == null)
                return;

            ApplyOverrideValues(_serverConfig);
        }

        private static void RefreshConfigManagerUi()
        {
            try
            {
                if (Chainloader.ManagerObject == null)
                    return;

                var configManagerType = ResolveConfigManagerType();
                if (configManagerType == null)
                    return;

                var instance = Chainloader.ManagerObject.GetComponent(configManagerType);
                if (instance == null)
                    return;

                // ConfigManager caches read-only state; rebuild the list when override toggles.
                var method = GetRefreshMethod(configManagerType);
                method?.Invoke(instance, null);
            }
            catch (Exception ex)
            {
                ClientLog.Debug($"ConfigManager UI refresh failed: {ex.Message}");
            }
        }

        private static MethodInfo GetRefreshMethod(Type configManagerType)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var method = configManagerType.GetMethod("BuildSettingList", flags);
            if (method != null && method.GetParameters().Length == 0)
                return method;

            method = configManagerType.GetMethod("RebuildSettingList", flags);
            if (method != null && method.GetParameters().Length == 0)
                return method;

            method = configManagerType.GetMethod("ReloadSettings", flags);
            if (method != null && method.GetParameters().Length == 0)
                return method;

            method = configManagerType.GetMethod("UpdateSettingList", flags);
            if (method != null && method.GetParameters().Length == 0)
                return method;

            return null;
        }

        private static Type ResolveConfigManagerType()
        {
            if (_configManagerType != null)
                return _configManagerType;

            _configManagerType = Type.GetType("BepInEx.ConfigurationManager.ConfigurationManager, ConfigurationManager");
            if (_configManagerType != null)
                return _configManagerType;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("BepInEx.ConfigurationManager.ConfigurationManager", false);
                if (type != null)
                {
                    _configManagerType = type;
                    break;
                }
            }

            return _configManagerType;
        }

        private static ConfigEntry<T> BindWithLegacy<T>(
            ConfigFile config,
            string section,
            string key,
            T defaultValue,
            string description,
            params string[] legacySections)
        {
            return BindWithLegacy(config, section, key, defaultValue, new ConfigDescription(description), legacySections);
        }

        private static ConfigEntry<T> BindWithLegacy<T>(
            ConfigFile config,
            string section,
            string key,
            T defaultValue,
            ConfigDescription description,
            params string[] legacySections)
        {
            var entry = config.Bind(section, key, defaultValue, description);

            if (legacySections == null || legacySections.Length == 0)
                return entry;

            foreach (var legacySection in legacySections)
            {
                if (string.IsNullOrWhiteSpace(legacySection))
                    continue;

                if (TryGetLegacyValue(config, legacySection, key, out T legacyValue) &&
                    EqualityComparer<T>.Default.Equals(entry.Value, defaultValue))
                {
                    SetEntryValueWithoutSaving(entry, legacyValue);
                    break;
                }
            }

            return entry;
        }

        private static ConfigDescription CreateStatusDescription(string description)
        {
            return new ConfigDescription(description, null, new object[] { StatusReadOnlyAttributes });
        }

        private static ConfigDescription CreateOverrideDescription(string description, AcceptableValueBase acceptableValues = null)
        {
            return new ConfigDescription(description, acceptableValues, new object[] { ServerOverrideAttributes });
        }

        private static void RunWithoutSaving(Action action)
        {
            if (_configFile == null)
            {
                action();
                return;
            }

            bool previous = _configFile.SaveOnConfigSet;
            _configFile.SaveOnConfigSet = false;
            try
            {
                action();
            }
            finally
            {
                _configFile.SaveOnConfigSet = previous;
            }
        }

        private static void SetEntryValueWithoutSaving<T>(ConfigEntry<T> entry, T value)
        {
            if (entry == null)
                return;

            RunWithoutSaving(() => entry.Value = value);
        }

        private static bool TryGetLegacyValue<T>(ConfigFile config, string section, string key, out T value)
        {
            value = default;
            if (config == null)
                return false;

            try
            {
                var method = typeof(ConfigFile).GetMethod(
                    "TryGetEntry",
                    new[] { typeof(ConfigDefinition), typeof(ConfigEntryBase).MakeByRefType() });

                if (method == null)
                    return false;

                object[] args = { new ConfigDefinition(section, key), null };
                bool success = (bool)method.Invoke(config, args);
                if (!success || args[1] == null)
                    return false;

                if (args[1] is ConfigEntry<T> typedEntry)
                {
                    value = typedEntry.Value;
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static T GetOverrideValue<T>(Func<ServerClientConfig, T> serverAccessor, T localValue)
        {
            if (_serverOverrideEnabled && _serverConfig != null)
                return serverAccessor(_serverConfig);

            return localValue;
        }

        private static void UpdateServerConfigStatus()
        {
            if (ServerConfigStatus == null)
                return;

            SetEntryValueWithoutSaving(
                ServerConfigStatus,
                _serverOverrideEnabled
                    ? "正在使用服务器参数（本地配置已保留）"
                    : "使用本地配置"
            );
        }
    }
}
