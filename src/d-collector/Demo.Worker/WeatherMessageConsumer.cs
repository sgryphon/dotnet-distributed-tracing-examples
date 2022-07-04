using MassTransit;

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
        await Task.Delay(TimeSpan.FromMilliseconds(200), context.CancellationToken);
    }
}
