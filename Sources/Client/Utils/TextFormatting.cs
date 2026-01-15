using System;
using System.Globalization;
using QuickPrice.Config;

namespace QuickPrice.Utils
{
    /// <summary>
    /// 文本格式化工具
    /// </summary>
    public static class TextFormatting
    {
        /// <summary>
        /// 格式化价格为卢布格式: ₽45,000
        /// </summary>
        public static string FormatPrice(double price)
        {
            if (Settings.UseKUnit != null && Settings.UseKUnit.Value)
            {
                return FormatPriceKUnit(price);
            }

            return FormatPriceDefault(price);
        }

        /// <summary>
        /// 格式化价格为紧凑格式: ₽45k, ₽1.5M
        /// </summary>
        public static string FormatPriceCompact(double price)
        {
            if (price >= 1000000)
                return $"₽{price / 1000000:0.#}M";
            if (price >= 1000)
                return $"₽{price / 1000:0.#}k";
            return $"₽{price:0}";
        }

        private static string FormatPriceDefault(double price)
        {
            return $"₽{price:#,0}";
        }

        private static string FormatPriceKUnit(double price)
        {
            if (price < 10000)
            {
                return FormatPriceDefault(price);
            }

            double value = price / 1000d;
            string formatted = value.ToString("0.#", CultureInfo.InvariantCulture);
            return $"₽{formatted}k";
        }

        /// <summary>
        /// 为文本添加粗体标签
        /// </summary>
        public static string Bold(string text)
        {
            return $"<b>{text}</b>";
        }

        /// <summary>
        /// 为文本添加斜体标签
        /// </summary>
        public static string Italic(string text)
        {
            return $"<i>{text}</i>";
        }
    }
}
