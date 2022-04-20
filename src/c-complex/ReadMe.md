# Dotnet Distributed Tracing Examples

Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.

## c) Complex Open Telemetry example

An OpenTelemetry example, with multiple components include adding message bus and SQL server, writing to both Jaeger, for tracing, and Elasticsearch, for logs.


### Requirements

* Dotnet 6.0
* Docker (with docker compose), for local services


### Run local services (Elasticsearch, Jaeger, RabbitMQ, and PostgreSQL)

For this complex example, you need to be running local Elasticsearch to send 
log messages to, and a local Jaeger service to send distributed tracing 
information to. 

You also need to run Rabbit MQ, for a service bus, and PostgreSQL, for a database.

For example on Linux a docker compose configuration is provided that runs all
components. To run the compose file:

```sh
docker compose -p demo up
```

#### Jaeger

For more details see https://www.jaegertracing.io/

To check the Jaeger UI, browse to `http://localhost:16686/`

#### Elasticsearch and Kibana

There are a number of prerequesites that you will need to meet, such as enough 
file handles; the elk-docker project provides a good list, including 
some troubleshooting (see https://elk-docker.readthedocs.io/).

For example, the most common issue is mmap count limit, which can be changed via: 

```sh
echo vm.max_map_count=262144 | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

To check the Kibana console, browse to `http://localhost:5601`

#### RabbitMQ

For more details see https://www.rabbitmq.com/

To check the RabbitMQ console, browse to `http://localhost:15672`

#### PostgreSQL and Adminer

For more details see https://www.postgresql.org/

To check the Adminer console, browse to `http://localhost:8080`

### Configure logging

A logger provider is available that can write directly to Elasticsearch. It can be installed via nuget.

```sh
dotnet add Demo.WebApp package Elasticsearch.Extensions.Logging --version 1.6.0-alpha1
```

To use the logger provider you need add a using statement at the top of `Program.cs`:

```csharp
using Elasticsearch.Extensions.Logging;
```

Change the logging configuration to keep the default console instead of OpenTelemetry, i.e. remove ClearLoggers(),
and add Elasticsearch. 

```csharp
// Configure logging
builder.Logging.ClearProviders()
    .AddOpenTelemetry(configure =>
    {
        configure
            .AddConsoleExporter();
    });
    .AddElasticsearch();
```

Repeat this for the back end service, adding the package, and the configuration as above:

```sh
dotnet add Demo.Service package Elasticsearch.Extensions.Logging --version 1.6.0-alpha1
```

### Configure tracing

A nuget package is available with the exporter.

```
dotnet add Demo.Service package OpenTelemetry.Exporter.Jaeger
dotnet add Demo.WebApp package OpenTelemetry.Exporter.Jaeger
```

Note that Jaeger only supports traces, not logging.

Change both Demo.Service and Demo.WebApp in `Program.cs`

Replace `AddConsoleExporter()` with `AddJaegerExporter()` in the tracing configuration.

```
// Add services to the container.
builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddJaegerExporter();
});
```

You can also remove the `OpenTelemetry.Exporter.Console` exporter package.

### Adding a message bus

#### Send a message

This example uses the MassTransit library wth RabbitMQ as the transport, based on https://masstransit-project.com/quick-starts/rabbitmq.html.

There is also an OpenTelemetry instrumentation library for MassTransit.

First add the required packages to the Web App project, which will send the message:

```sh
dotnet add Demo.WebApp package MassTransit.RabbitMQ
dotnet add Demo.WebApp package OpenTelemetry.Contrib.Instrumentation.MassTransit --version 1.0.0-beta2
```


Then add configuration settings to `appsettings.Development.json`, based on the set up of docker:

```json
  "MassTransit": {
    "RabbitMq": {
      "Host": "localhost",
      "Port": 5672,
      "VirtualHost": "/",
      "Username": "user",
      "Password": "password"
    }
  }
```

Configure the service in `Program.cs`, with the namespace:

```csharp
using MassTransit;
```

And add the instrumentation to OpenTelemetry tracing:

```
builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
        ...
        .AddMassTransitInstrumentation()
        ...
});
```

Then register the MassTransit service configured with RabbitMQ:

```csharp
builder.Services.AddMassTransit(mtConfig => {
    mtConfig.UsingRabbitMq((context, rabbitConfig) => {
        rabbitConfig.Host(configuration.GetValue<string>("MassTransit:RabbitMq:Host"),
            configuration.GetValue<ushort>("MassTransit:RabbitMq:Port"),
            configuration.GetValue<string>("MassTransit:RabbitMq:VirtualHost"),
            hostConfig => {
                hostConfig.Username(configuration.GetValue<string>("MassTransit:RabbitMq:Username"));
                hostConfig.Password(configuration.GetValue<string>("MassTransit:RabbitMq:Password"));
            }
        );
    });
});
```

Add a new class `WeatherMessage.cs` with an interface for the message we are sending. The full name,
including the namespace, is used for configuring messaging and needs to be the same on both the sender
and receiver:

```csharp
namespace Demo;

public interface WeatherMessage
{
    string Note { get; }
}
```

In `WeatherForecastController.cs` inject the MassTransit publisher into the constructor:

```csharp
  private readonly MassTransit.IPublishEndpoint _publishEndpoint;
  
  public WeatherForecastController(..., 
      MassTransit.IPublishEndpoint publishEndpoint)
  {
    ...
    _publishEndpoint = publishEndpoint;
  }
```

Then change the request handler to async and publish a message with the interface type:

```csharp
  [HttpGet]
  public async Task<string> Get(System.Threading.CancellationToken cancellationToken)
  {
    _logger.LogInformation(2001, "TRACING DEMO: WebApp API weather forecast request forwarded");
    await _publishEndpoint.Publish<Demo.WeatherMessage>(new { Note = "Demo Message" }, cancellationToken);
    return await _httpClient.GetStringAsync("https://localhost:44301/WeatherForecast", cancellationToken);
  }
```


### Add a console worker app to receive the message

Create a console app and add the logging and Azure message bus packages

```sh
dotnet new worker --output Demo.Worker
dotnet sln add Demo.Worker
dotnet add Demo.Worker package Elasticsearch.Extensions.Logging --version 1.6.0-alpha1
dotnet add Demo.Worker package Azure.Messaging.ServiceBus
dotnet add Demo.Worker package Microsoft.Extensions.Azure
```

Configure logging in `Program.cs`:

```csharp
using Elasticsearch.Extensions.Logging;
...

  Host.CreateDefaultBuilder(args)
    .ConfigureLogging((hostContext, loggingBuilder) =>
    {
        loggingBuilder.AddElasticsearch();
    })
  ...
```

Also configure MassTransit in `Program.cs`:



Also comment out the logging that happens every second of the loop, to avoid cluttering up the output:

```csharp
  while (!stoppingToken.IsCancellationRequested)
  {
      //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
      await Task.Delay(1000, stoppingToken);
  }
```


Also update logging in the new worker service `appSettings.Development.json` to be consistent with the other applications and include scopes:

```json
{
  "Logging": {
    "Console": {
      "FormatterName": "simple",
      "FormatterOptions": {
        "IncludeScopes": true
      }
    },
    ...
  }
}
```


### Adding a database


### Run the services

In separate terminals run the service:

```powershell
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development
```

To run the web app front end you need to configure the web API address it will use via an environment variable:

```powershell
$ENV:ASPNETCORE_URLS = "http://localhost:8002"
npm run start --prefix Demo.WebApp/ClientApp
```

Then run the web api in a third terminal:

```powershell
dotnet run --project Demo.WebApp --urls "http://*:8002" --environment Development
```

Generate some activity via the front end at `https://localhost:44303/fetch-data`.




