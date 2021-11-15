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
        private readonly Azure.Messaging.ServiceBus.ServiceBusClient _serviceBusClient;
        
        public WeatherForecastController(ILogger<WeatherForecastController> logger, 
            System.Net.Http.HttpClient httpClient,
            Azure.Messaging.ServiceBus.ServiceBusClient serviceBusClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _serviceBusClient = serviceBusClient;
        }

        [HttpGet]
        public async Task<string> Get(System.Threading.CancellationToken cancellationToken)
        {
            _logger.LogInformation(2001, "TRACING DEMO: WebApp API weather forecast request forwarded");
            await using var sender = _serviceBusClient.CreateSender("demo-queue");
            await sender.SendMessageAsync(new Azure.Messaging.ServiceBus.ServiceBusMessage("Demo Message"), cancellationToken);
            return await _httpClient.GetStringAsync("https://localhost:44301/WeatherForecast", cancellationToken);
        }
    }
}
