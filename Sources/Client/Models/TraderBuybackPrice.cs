namespace QuickPrice.Models
{
    /// <summary>
    /// 服务端返回的商人回收价格条目
    /// </summary>
    public class TraderBuybackPrice
    {
        public string TraderId { get; set; } = string.Empty;
        public string TraderName { get; set; } = string.Empty;
        public double PriceRoubles { get; set; }
    }
}
