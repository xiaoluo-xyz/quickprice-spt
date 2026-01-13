namespace QuickPrice.Logging
{
    /// <summary>
    /// Central switch for client debug logs.
    /// </summary>
    public static class ClientLog
    {
        private static bool _debugEnabled;

        public static bool DebugEnabled => _debugEnabled;

        public static void EnableDebug() => SetDebugEnabled(true);

        public static void DisableDebug() => SetDebugEnabled(false);

        public static void SetDebugEnabled(bool enabled)
        {
            _debugEnabled = enabled;
        }

        public static void Debug(string message)
        {
            if (!_debugEnabled)
            {
                return;
            }

            Plugin.Log.LogDebug(message);
        }

        public static void Warning(string message)
        {
            if (!_debugEnabled)
            {
                return;
            }

            Plugin.Log.LogWarning(message);
        }
    }
}
