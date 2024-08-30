using System.Diagnostics.Tracing;

namespace Demo.WebApi.Extensions;

public class OpenTelemetryDebugService(IConfiguration configuration) : BackgroundService
{
    private DebugEventListener? listener;

    public override void Dispose()
    {
        listener?.Dispose();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        listener = new DebugEventListener(configuration);
        return Task.CompletedTask;
    }

    private class DebugEventListener(IConfiguration configuration) : EventListener
    {
        private readonly bool debugAll = configuration.GetValue<bool>("OpenTelemetry:Debug");
        private readonly bool debugOtlp = configuration.GetValue<bool>("OpenTelemetry:OtlpDebug");

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // This is called during base constructor (before any derived constructor), with existing sources
            if (
                debugOtlp
                && eventSource.Name.StartsWith("OpenTelemetry-Exporter-OpenTelemetryProtocol")
            )
            {
                Console.WriteLine("OTEL_DEBUG: Enabling OTLP source {0}", eventSource.Name);
                EnableEvents(eventSource, EventLevel.Verbose);
            }
            else if (debugAll && eventSource.Name.StartsWith("OpenTelemetry-"))
            {
                Console.WriteLine("OTEL_DEBUG: Enabling source {0}", eventSource.Name);
                EnableEvents(eventSource, EventLevel.Verbose);
            }
            else
            {
                Console.WriteLine("OTEL_DEBUG: Ignoring source {0}", eventSource.Name);
            }

            base.OnEventSourceCreated(eventSource);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // This is debugging OTLP, including logging sent via OTLP, so we can't write to logging
            // because it could cause an infinite loop, so Just output messages to console.
            // (This will be turned off once OTLP is working)
            try
            {
                // OTLP event source sends event ID, level, with message as a format string, and payload the args
                // See: https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/Implementation/OpenTelemetryProtocolExporterEventSource.cs
                var formattedMessage = string.Format(
                        eventData.Message!,
                        eventData.Payload!.ToArray()
                    )
                    .ReplaceLineEndings("|");
                Console.WriteLine(
                    "OTEL_DEBUG: {0} [{1}] {2}",
                    eventData.Level,
                    eventData.EventId,
                    formattedMessage
                );
            }
            catch (Exception ex)
            {
                // Should never happen, but just in case :-)
                Console.WriteLine(
                    "OTEL_DEBUG: EXCEPTION {0} [{1}] {2}",
                    eventData.Level,
                    eventData.EventId,
                    ex.Message
                );
            }
        }
    }
}
