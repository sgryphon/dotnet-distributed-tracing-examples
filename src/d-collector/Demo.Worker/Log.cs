namespace Demo.Worker;

internal static class Log
{
    // Using high performance logging pattern, see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage
    internal static class Warning
    {
        public static readonly Action<ILogger, string, Exception?> WorkerMessageReceived =
            LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(4003, nameof(WorkerMessageReceived)),
                "TRACING DEMO: Worker message received: {Note}");
    }
}
