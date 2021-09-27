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
            var allListeners = new List<IDisposable>();
            allListeners.Add(System.Diagnostics.DiagnosticListener.AllListeners.Subscribe(listener =>
            {
                if (listener.Name == "Azure.Messaging.ServiceBus")
                {
                    allListeners.Add(listener.Subscribe(kvp =>
                    {
                    }));
                }
            }));

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddElasticsearch();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddAzureClients(builder =>
                    {
                        builder.AddServiceBusClient(hostContext.Configuration.GetSection("ConnectionStrings:ServiceBus")
                            .Value);
                    });
                });
    }
}