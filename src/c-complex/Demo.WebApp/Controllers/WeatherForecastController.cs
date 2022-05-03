using Microsoft.AspNetCore.Mvc;

namespace Demo.WebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly MassTransit.IPublishEndpoint _publishEndpoint;

    public WeatherForecastController(ILogger<WeatherForecastController> logger,
        System.Net.Http.HttpClient httpClient,
        MassTransit.IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _httpClient = httpClient;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<string> Get(System.Threading.CancellationToken cancellationToken)
    {
        _logger.LogWarning(4001, "TRACING DEMO: WebApp API weather forecast request forwarded");
        await _publishEndpoint.Publish<Demo.WeatherMessage>(new { Note = "Demo Message" }, cancellationToken);
        return await _httpClient.GetStringAsync("https://localhost:44301/WeatherForecast", cancellationToken);
    }
}