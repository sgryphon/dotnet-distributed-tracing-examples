using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Demo.WebApp.Controllers
{
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
            _logger.LogInformation(2001, "WebApp API weather forecast request forwarded");
            return _httpClient.GetStringAsync("https://localhost:44301/WeatherForecast", cancellationToken);
        }
    }
}
