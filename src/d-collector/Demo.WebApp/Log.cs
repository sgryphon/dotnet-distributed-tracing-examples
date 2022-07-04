namespace Demo.WebApp;

internal static class Log
{
    // Using high performance logging pattern, see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage
    internal static class Warning
    {
        public static readonly Action<ILogger, Exception?> WebAppForecastRequestForwarded =
            LoggerMessage.Define(LogLevel.Warning,
                new EventId(4001, nameof(WebAppForecastRequestForwarded)),
                "TRACING DEMO: WebApp API weather forecast request forwarded");
    }
}
