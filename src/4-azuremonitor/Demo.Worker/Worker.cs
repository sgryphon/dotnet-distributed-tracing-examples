using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Azure.Messaging.ServiceBus.ServiceBusClient _serviceBusClient;

        public Worker(ILogger<Worker> logger, Azure.Messaging.ServiceBus.ServiceBusClient serviceBusClient)
        {
            _logger = logger;
            _serviceBusClient = serviceBusClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var serviceBusProcessor = _serviceBusClient.CreateProcessor("demo-queue");
            serviceBusProcessor.ProcessMessageAsync += args =>
            {
                _logger.LogInformation(2003, "Message received: {MessageBody}", args.Message.Body);
                return Task.CompletedTask;
            };
            serviceBusProcessor.ProcessErrorAsync += args =>
            {
                _logger.LogError(5000, args.Exception, "Service bus error");
                return Task.CompletedTask;
            };
            await serviceBusProcessor.StartProcessingAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
