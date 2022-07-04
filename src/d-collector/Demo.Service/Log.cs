namespace Demo.Service;

internal static class Log
{
    // Using high performance logging pattern, see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage
    internal static class Warning
    {
        public static readonly Action<ILogger, Exception?> ServiceForecastRequest =
            LoggerMessage.Define(LogLevel.Warning,
                new EventId(4002, nameof(ServiceForecastRequest)),
                "TRACING DEMO: Back end service weather forecast requested");
    }
}
