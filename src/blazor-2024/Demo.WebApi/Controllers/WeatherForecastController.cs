using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Demo.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    // Custom metrics for the application
    private static readonly Meter weatherMeter = new Meter("OTel.Example", "1.0.0");
    private static readonly Counter<int> countWeatherCalls = weatherMeter.CreateCounter<int>("weather.count", description: "Counts the number of times weather was called");

    // Custom ActivitySource for the application
    private static readonly ActivitySource weatherActivitySource = new ActivitySource("OTel.Example");
    
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        // Create a new Activity scoped to the method
        using var activity = weatherActivitySource.StartActivity("WeatherActivity");

        // Increment the custom counter
        countWeatherCalls.Add(1);

        // Add a tag to the Activity
        activity?.SetTag("weather", "Hello World!");
    
        _logger.LogWarning(4102, "TRACING DEMO: Back end Web API weather forecast requested");

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}
