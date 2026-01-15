namespace QuickPrice.Models
{
    public class ServerClientConfig
    {
        public bool OverrideClientConfig { get; set; }
        public int PriceThreshold1 { get; set; }
        public int PriceThreshold2 { get; set; }
        public int PriceThreshold3 { get; set; }
        public int PriceThreshold4 { get; set; }
        public int PriceThreshold5 { get; set; }
        public bool EnableSearchTimeAdjustment { get; set; }
        public float SearchTimeRandomMin { get; set; }
        public float SearchTimeRandomMax { get; set; }
        public float SearchTimeLevel1 { get; set; }
        public float SearchTimeLevel2 { get; set; }
        public float SearchTimeLevel3 { get; set; }
        public float SearchTimeLevel4 { get; set; }
        public float SearchTimeLevel5 { get; set; }
        public float SearchTimeLevel6 { get; set; }
        public int PenetrationThreshold1 { get; set; }
        public int PenetrationThreshold2 { get; set; }
        public int PenetrationThreshold3 { get; set; }
        public int PenetrationThreshold4 { get; set; }
        public int PenetrationThreshold5 { get; set; }
    }
}
