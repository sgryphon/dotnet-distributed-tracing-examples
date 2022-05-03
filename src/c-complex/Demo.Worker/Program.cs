using MassTransit;

using Demo.Worker;

IHost host = Host.CreateDefaultBuilder(args)
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
