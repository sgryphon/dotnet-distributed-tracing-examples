using MassTransit;
using System.Diagnostics;

namespace Demo.Worker;

public class WeatherMessageConsumer : IConsumer<WeatherMessage>
{
    private readonly ILogger _logger;

    public WeatherMessageConsumer(ILogger<WeatherMessageConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WeatherMessage> context)
    {
        _logger.LogWarning(4002, "TRACING DEMO: Worker message received: {Note}", context.Message.Note);

        // var workerActivity = default(Activity);
        // try
        // {
        //     workerActivity = DemoActivitySource.Instance.StartActivity("worker_activity", ActivityKind.Internal,
        //         Activity.Current?.Context ?? new ActivityContext());
        //     workerActivity?.AddTag("code.function", "Consume");

            await Task.Delay(TimeSpan.FromMilliseconds(200), context.CancellationToken);

        // }
        // finally
        // {
        //     workerActivity?.Stop();
        // }
    }
}
