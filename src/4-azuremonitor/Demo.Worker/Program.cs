using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Elasticsearch.Extensions.Logging;
using Microsoft.Extensions.Azure;

namespace Demo.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostContext, loggingBuilder) =>
                {
//                    loggingBuilder.AddElasticsearch();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.AddSingleton<Microsoft.ApplicationInsights.Extensibility.ITelemetryInitializer, ApplicationInsights.DemoTelemetryInitializer>();
                    services.AddHostedService<Worker>();
                    services.AddAzureClients(builder =>
                    {
                        builder.AddServiceBusClient(hostContext.Configuration.GetSection("ConnectionStrings:ServiceBus")
                            .Value);
                    });
                });
    }
}
