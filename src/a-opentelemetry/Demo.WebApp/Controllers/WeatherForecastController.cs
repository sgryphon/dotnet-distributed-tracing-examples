using Microsoft.AspNetCore.Mvc;

namespace Demo.WebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger,
        System.Net.Http.HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    [HttpGet]
    public Task<string> Get(System.Threading.CancellationToken cancellationToken)
    {
        _logger.LogWarning(4001, "TRACING DEMO: WebApp API weather forecast request forwarded");
        return _httpClient.GetStringAsync("https://localhost:44301/WeatherForecast", cancellationToken);
    }
}
