using Demo.Worker;
using Elasticsearch.Extensions.Logging;
using MassTransit;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((hostBuilderContext, loggingBuilder) =>
    {
        loggingBuilder.AddElasticsearch();
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {
        services.AddMassTransit(mtConfig => {
            mtConfig.AddConsumer<WeatherMessageConsumer>();
            mtConfig.UsingRabbitMq((context, rabbitConfig) => {
                rabbitConfig.Host(hostBuilderContext.Configuration.GetValue<string>("MassTransit:RabbitMq:Host"),
                    hostBuilderContext.Configuration.GetValue<ushort>("MassTransit:RabbitMq:Port"),
                    hostBuilderContext.Configuration.GetValue<string>("MassTransit:RabbitMq:VirtualHost"),
                    hostConfig => {
                        hostConfig.Username(hostBuilderContext.Configuration.GetValue<string>("MassTransit:RabbitMq:Username"));
                        hostConfig.Password(hostBuilderContext.Configuration.GetValue<string>("MassTransit:RabbitMq:Password"));
                    }
                );
                rabbitConfig.ConfigureEndpoints(context);
            });
        });
    })
    .Build();

await host.RunAsync();
