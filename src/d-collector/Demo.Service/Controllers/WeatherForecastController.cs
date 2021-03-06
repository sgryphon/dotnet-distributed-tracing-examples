using Microsoft.AspNetCore.Mvc;

namespace Demo.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly WeatherContext _weatherContext;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, WeatherContext weatherContext)
    {
        _logger = logger;
        _weatherContext = weatherContext;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        _weatherContext.WeatherServiceRequests.Add(new WeatherServiceRequest() {Note = "Demo Note"});
        _weatherContext.SaveChanges();

        Log.Warning.ServiceForecastRequest(_logger, null);

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}
