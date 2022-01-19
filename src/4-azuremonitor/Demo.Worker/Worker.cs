using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly Azure.Messaging.ServiceBus.ServiceBusClient _serviceBusClient;

        public Worker(ILogger<Worker> logger, TelemetryClient telemetryClient, Azure.Messaging.ServiceBus.ServiceBusClient serviceBusClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
            _serviceBusClient = serviceBusClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var serviceBusProcessor = _serviceBusClient.CreateProcessor("sbq-demo");
            serviceBusProcessor.ProcessMessageAsync += args =>
            {
                using var activity = new System.Diagnostics.Activity("ServiceBusProcessor.ProcessMessage");
                if (args.Message.ApplicationProperties.TryGetValue("Diagnostic-Id", out var objectId) &&
                    objectId is string traceparent)
                {
                    activity.SetParentId(traceparent);
                }
                using var operation = _telemetryClient.StartOperation<Microsoft.ApplicationInsights.DataContracts.RequestTelemetry>(activity);

                _logger.LogInformation(2003, "TRACING DEMO: Message received: {MessageBody}", args.Message.Body);
                return Task.CompletedTask;
            };
            serviceBusProcessor.ProcessErrorAsync += args =>
            {
                _logger.LogError(5000, args.Exception, "TRACING DEMO: Service bus error");
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
