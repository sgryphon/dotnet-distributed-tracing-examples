using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo.WebApp
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
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
