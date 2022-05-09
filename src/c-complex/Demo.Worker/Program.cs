using Demo.Worker;
using Elasticsearch.Extensions.Logging;
using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((hostBuilderContext, loggingBuilder) =>
    {
        loggingBuilder.AddElasticsearch();
        //var resourceBuilder = ResourceBuilder.CreateDefault();
        //loggingBuilder.AddOpenTelemetry(configure =>
        //{
        //        configure.SetResourceBuilder(resourceBuilder);
        //});
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {
        // Configure OpenTelemetry service resource details
        // See https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
        var entryAssemblyName = entryAssembly?.GetName();
        var versionAttribute = entryAssembly?.GetCustomAttributes(false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault();
        var serviceName = entryAssemblyName?.Name;
        var serviceVersion = versionAttribute?.InformationalVersion ?? entryAssemblyName?.Version?.ToString();
        var attributes = new Dictionary<string, object>
        {
            ["host.name"] = Environment.MachineName,
            ["os.description"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ["deployment.environment"] = hostBuilderContext.HostingEnvironment.EnvironmentName.ToLowerInvariant()
        };
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion)
            .AddTelemetrySdk()
            .AddAttributes(attributes);

        // Configure tracing
        services.AddOpenTelemetryTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .SetResourceBuilder(resourceBuilder)
                .AddSource("MassTransit")
                //.AddMassTransitInstrumentation()
                .AddJaegerExporter();
        });

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

        services.AddHostedService<DebugService>();
    })
    .Build();

await host.RunAsync();
