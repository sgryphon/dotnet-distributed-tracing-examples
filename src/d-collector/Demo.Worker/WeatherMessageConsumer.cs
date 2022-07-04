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
        Log.Warning.WorkerMessageReceived(_logger, context.Message.Note, null);

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
