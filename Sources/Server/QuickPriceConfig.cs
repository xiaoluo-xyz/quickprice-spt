// ----------------------------------------------------------------------------
// QuickPrice - Server Configuration Model
// 服务端配置模型
// ----------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace QuickPrice.Server
{
    /// <summary>
    /// QuickPrice 服务端配置模型
    /// 对应 config.json 文件结构
    /// </summary>
    public class QuickPriceConfig
    {
        /// <summary>
        /// 是否启用模组
        /// </summary>
        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 日志级别
        /// </summary>
        [JsonPropertyName("LogLevel")]
        public string LogLevel { get; set; } = "Info";

        /// <summary>
        /// 缓存超时时间（秒）
        /// </summary>
        [JsonPropertyName("CacheTimeoutSeconds")]
        public int CacheTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// 自动刷新间隔（秒）
        /// 默认300秒，设置为0则禁用自动刷新
        /// </summary>
        [JsonPropertyName("AutoRefreshIntervalSeconds")]
        public int AutoRefreshIntervalSeconds { get; set; } = 300;

        /// <summary>
        /// 是否启用服务端配置覆盖客户端配置
        /// </summary>
        [JsonPropertyName("OverrideClientConfig")]
        public bool OverrideClientConfig { get; set; } = false;

        /// <summary>
        /// 价格分级阈值1（白色→绿色）
        /// </summary>
        [JsonPropertyName("PriceThreshold1")]
        public int PriceThreshold1 { get; set; } = 25000;

        /// <summary>
        /// 价格分级阈值2（绿色→蓝色）
        /// </summary>
        [JsonPropertyName("PriceThreshold2")]
        public int PriceThreshold2 { get; set; } = 45000;

        /// <summary>
        /// 价格分级阈值3（蓝色→紫色）
        /// </summary>
        [JsonPropertyName("PriceThreshold3")]
        public int PriceThreshold3 { get; set; } = 70000;

        /// <summary>
        /// 价格分级阈值4（紫色→橙色）
        /// </summary>
        [JsonPropertyName("PriceThreshold4")]
        public int PriceThreshold4 { get; set; } = 100000;

        /// <summary>
        /// 价格分级阈值5（橙色→红色）
        /// </summary>
        [JsonPropertyName("PriceThreshold5")]
        public int PriceThreshold5 { get; set; } = 250000;

        /// <summary>
        /// 是否启用搜索品质延迟
        /// </summary>
        [JsonPropertyName("EnableSearchTimeAdjustment")]
        public bool EnableSearchTimeAdjustment { get; set; } = false;

        /// <summary>
        /// 搜索随机延迟最小值（秒）
        /// </summary>
        [JsonPropertyName("SearchTimeRandomMin")]
        public float SearchTimeRandomMin { get; set; } = 0f;

        /// <summary>
        /// 搜索随机延迟最大值（秒）
        /// </summary>
        [JsonPropertyName("SearchTimeRandomMax")]
        public float SearchTimeRandomMax { get; set; } = 1f;

        /// <summary>
        /// 品质等级1搜索时间（秒）
        /// </summary>
        [JsonPropertyName("SearchTimeLevel1")]
        public float SearchTimeLevel1 { get; set; } = 1f;

        /// <summary>
        /// 品质等级2搜索时间（秒）
        /// </summary>
        [JsonPropertyName("SearchTimeLevel2")]
        public float SearchTimeLevel2 { get; set; } = 2f;

        /// <summary>
        /// 品质等级3搜索时间（秒）
        /// </summary>
        [JsonPropertyName("SearchTimeLevel3")]
        public float SearchTimeLevel3 { get; set; } = 3f;

        /// <summary>
        /// 品质等级4搜索时间（秒）
        /// </summary>
        [JsonPropertyName("SearchTimeLevel4")]
        public float SearchTimeLevel4 { get; set; } = 4f;

        /// <summary>
        /// 品质等级5搜索时间（秒）
        /// </summary>
        [JsonPropertyName("SearchTimeLevel5")]
        public float SearchTimeLevel5 { get; set; } = 5f;

        /// <summary>
        /// 品质等级6搜索时间（秒）
        /// </summary>
        [JsonPropertyName("SearchTimeLevel6")]
        public float SearchTimeLevel6 { get; set; } = 6f;

        /// <summary>
        /// 穿甲等级阈值1（白色→绿色）
        /// </summary>
        [JsonPropertyName("PenetrationThreshold1")]
        public int PenetrationThreshold1 { get; set; } = 20;

        /// <summary>
        /// 穿甲等级阈值2（绿色→蓝色）
        /// </summary>
        [JsonPropertyName("PenetrationThreshold2")]
        public int PenetrationThreshold2 { get; set; } = 30;

        /// <summary>
        /// 穿甲等级阈值3（蓝色→紫色）
        /// </summary>
        [JsonPropertyName("PenetrationThreshold3")]
        public int PenetrationThreshold3 { get; set; } = 40;

        /// <summary>
        /// 穿甲等级阈值4（紫色→橙色）
        /// </summary>
        [JsonPropertyName("PenetrationThreshold4")]
        public int PenetrationThreshold4 { get; set; } = 50;

        /// <summary>
        /// 穿甲等级阈值5（橙色→红色）
        /// </summary>
        [JsonPropertyName("PenetrationThreshold5")]
        public int PenetrationThreshold5 { get; set; } = 60;

        /// <summary>
        /// 配置说明
        /// </summary>
        [JsonPropertyName("Notes")]
        public string Notes { get; set; } = "QuickPrice 服务端配置文件";

    }
}
