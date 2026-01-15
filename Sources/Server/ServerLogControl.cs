namespace QuickPrice.Server
{
    internal enum ServerLogLevel
    {
        Off = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4
    }

    internal static class ServerLogControl
    {
        private static volatile ServerLogLevel _currentLevel = ServerLogLevel.Info;

        internal static ServerLogLevel CurrentLevel => _currentLevel;

        internal static void UpdateLevel(string? logLevel)
        {
            _currentLevel = ParseLogLevel(logLevel, ServerLogLevel.Info);
        }

        internal static bool Allows(ServerLogLevel messageLevel)
        {
            if (messageLevel == ServerLogLevel.Error || messageLevel == ServerLogLevel.Warning)
                return true;

            var configuredLevel = _currentLevel;
            return configuredLevel != ServerLogLevel.Off && messageLevel <= configuredLevel;
        }

        internal static ServerLogLevel ParseLogLevel(string? raw, ServerLogLevel fallback)
        {
            if (raw == null)
                return fallback;

            if (string.IsNullOrWhiteSpace(raw))
                return ServerLogLevel.Off;

            switch (raw.Trim().ToLowerInvariant())
            {
                case "off":
                case "none":
                case "disable":
                case "disabled":
                    return ServerLogLevel.Off;
                case "error":
                    return ServerLogLevel.Error;
                case "warn":
                case "warning":
                    return ServerLogLevel.Warning;
                case "info":
                case "information":
                case "success":
                    return ServerLogLevel.Info;
                case "debug":
                case "trace":
                case "verbose":
                    return ServerLogLevel.Debug;
                default:
                    return fallback;
            }
        }
    }
}
