using MassTransit;

namespace Demo.Worker;

public class WeatherMessageConsumer : IConsumer<WeatherMessage>
{
    private readonly ILogger _logger;

    public WeatherMessageConsumer(ILogger<WeatherMessageConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WeatherMessage> context)
    {
        _logger.LogWarning(4002, "TRACING DEMO: Worker message received: {Note}", context.Message.Note);
        return Task.CompletedTask;
    }
}
