using System.Diagnostics.Tracing;

namespace Demo.Worker;

public class DebugService : BackgroundService
{
    private DebugEventListener? _listener;

    public override void Dispose()
    {
        _listener?.Dispose();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new DebugEventListener();
        return Task.CompletedTask;
    }

    private class DebugEventListener : EventListener
    {
        private static string[] _sources = new string[]{ "OpenTelemetry-Instrumentation-MassTransit" };

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // This is called during base constructor (before any derive constructor), with existing sources
            if (_sources.Contains(eventSource.Name))
            {
                Console.WriteLine("DEBUG: Enabling source {0}", eventSource.Name);
                EnableEvents(eventSource, EventLevel.Verbose);
            }
            else
            {
                Console.WriteLine("DEBUG: Ignoring source {0}", eventSource.Name);
            }

            base.OnEventSourceCreated(eventSource);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            try
            {
                var formattedMessage = string.Format(eventData.Message!, eventData.Payload!.ToArray()).ReplaceLineEndings("|");
                Console.WriteLine("DEBUG: {0} [{1}] {2}", eventData.Level, eventData.EventId, formattedMessage);
            }
            catch (Exception ex)
            {
                // Should never happen, but just in case :-)
                Console.WriteLine("DEBUG: EXCEPTION {0} [{1}] {2}", eventData.Level, eventData.EventId, ex.Message);
            }
        }
    }
}
