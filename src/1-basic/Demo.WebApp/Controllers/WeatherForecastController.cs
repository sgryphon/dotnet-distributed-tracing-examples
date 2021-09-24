using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Demo.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> GetAsync(string value, int ts,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(2001, "Weather forecast requested with value {Value} at timestamp {TimeStamp}",
                value, ts);
            var response = await _httpClient.GetAsync("https://localhost:44301/WeatherForecast", cancellationToken)
                .ConfigureAwait(false);
            var data = await response.Content
                .ReadFromJsonAsync<List<WeatherForecast>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken).ConfigureAwait(false);
            _logger.LogDebug(6001, "Weather forecast returning {Count} items", data?.Count);
            return data;
        }
    }
}
