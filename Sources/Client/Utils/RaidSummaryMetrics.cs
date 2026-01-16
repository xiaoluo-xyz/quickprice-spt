using System;
using System.Globalization;

namespace QuickPrice.Utils
{
    /// <summary>
    /// 战局结算数值缓存与格式化工具
    /// </summary>
    public static class RaidSummaryMetrics
    {
        public static event Action ValuesChanged;

        private static long _broughtValue;
        private static long _lossValue;
        private static long _lootValue;
        private static long _settlementValue;
        private static bool _isInRaid;

        public static long BroughtValue
        {
            get => _broughtValue;
            set
            {
                if (_broughtValue == value)
                    return;
                _broughtValue = value;
                ValuesChanged?.Invoke();
            }
        }

        public static long LossValue
        {
            get => _lossValue;
            set
            {
                if (_lossValue == value)
                    return;
                _lossValue = value;
                ValuesChanged?.Invoke();
            }
        }

        public static long LootValue
        {
            get => _lootValue;
            set
            {
                if (_lootValue == value)
                    return;
                _lootValue = value;
                ValuesChanged?.Invoke();
            }
        }

        public static long SettlementValue
        {
            get => _settlementValue;
            set
            {
                if (_settlementValue == value)
                    return;
                _settlementValue = value;
                ValuesChanged?.Invoke();
            }
        }

        public static bool IsInRaid
        {
            get => _isInRaid;
            set
            {
                if (_isInRaid == value)
                    return;
                _isInRaid = value;
                ValuesChanged?.Invoke();
            }
        }

        private const string PositiveColor = "#6FA36A";
        private const string NegativeColor = "#C24A4A";
        private const string NeutralColor = "#FFFFFF";

        public static void Reset()
        {
            bool changed = false;
            changed |= SetValue(ref _broughtValue, 0);
            changed |= SetValue(ref _lossValue, 0);
            changed |= SetValue(ref _lootValue, 0);
            changed |= SetValue(ref _settlementValue, 0);
            changed |= SetFlag(ref _isInRaid, false);
            if (changed)
            {
                ValuesChanged?.Invoke();
            }
        }

        public static string BuildSettlementText()
        {
            string amount = FormatRuble(SettlementValue);
            if (SettlementValue > 0)
            {
                return $"<color={PositiveColor}>本局收获: {amount}</color>";
            }
            if (SettlementValue < 0)
            {
                return $"<color={NegativeColor}>本局收获: {amount}</color>";
            }
            return $"本局收获: {amount}";
        }

        public static string FormatRuble(long value)
        {
            long absValue = Math.Abs(value);
            string digits = absValue.ToString("#,0", CultureInfo.InvariantCulture);
            string prefix = value < 0 ? "-₽" : "₽";
            return prefix + digits;
        }

        public static string FormatLabelValue(string label, long value, string color)
        {
            string text = $"{label}: {FormatRuble(value)}";
            if (string.IsNullOrEmpty(color))
                return text;
            return $"<color={color}>{text}</color>";
        }

        public static string GetNeutralColor()
        {
            return NeutralColor;
        }

        public static string GetNegativeColor()
        {
            return NegativeColor;
        }

        public static string GetPositiveColor()
        {
            return PositiveColor;
        }

        public static string GetSettlementColor(long value)
        {
            if (value > 0)
                return PositiveColor;
            if (value < 0)
                return NegativeColor;
            return NeutralColor;
        }

        private static bool SetValue(ref long target, long value)
        {
            if (target == value)
                return false;
            target = value;
            return true;
        }

        private static bool SetFlag(ref bool target, bool value)
        {
            if (target == value)
                return false;
            target = value;
            return true;
        }
    }
}
