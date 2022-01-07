# Dotnet Distributed Tracing Examples
Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.

## 3) Azure message bus

Example manually configuring Azure service bus message handler to read the incoming correlation identifier (which is automatically sent) and start a local child.

### Requirements

* Dotnet 5.0
* Docker (with docker-compose), for local services
* Azure subscription, for cloud services
* Azure CLI, to create cloud resources
* Powershell, for running scripts

### Set up a message bus

First of all, you need to log in to your Azure resources:

```powershell
az login
```

There is a PowerShell script that will create the required resources, and output the required connection string.

```powershell
./deploy-infrastructure.ps1
```

You can log in to the Azure portal to check your queue was created at `https://portal.azure.com`

#### Azure command details

You can also run the individual Azure commands directly to create a resource group, then a service bus namespace.

You need to use a unique name for the namespace, e.g. the script uses first four characters of your subscription ID, then create queue within that namespace.

```powershell
$OrgId = "0x$($(az account show --query id --output tsv).Substring(0,4))"
az group create -n rg-tracedemo-dev-001 -l australiaeast
az servicebus namespace create -n sb-tracedemo-$OrgId-dev -g rg-tracedemo-dev-001 --sku Standard
az servicebus queue create -n sbq-demo --namespace-name sb-tracedemo-$OrgId-dev -g rg-tracedemo-dev-001
```

You will need the primary connection string key to configure in the application:

```powershell
$connectionString = (az servicebus namespace authorization-rule keys list -g rg-tracedemo-dev-001 --namespace-name sb-tracedemo-$OrgId-dev --name RootManageSharedAccessKey --query primaryConnectionString -o tsv)
$connectionString
```

### Send a message to the queue

Add the Azure Messaging nuget package to the Web App project:

```sh
dotnet add Demo.WebApp package Azure.Messaging.ServiceBus
dotnet add Demo.WebApp package Microsoft.Extensions.Azure
```

Register the message bus client in `Startup.cs`, first with the namespace:

```csharp
using Microsoft.Extensions.Azure;
```

Then register the service:

```csharp
  public void ConfigureServices(IServiceCollection services)
  {
    ...
    services.AddAzureClients(builder =>
    {
        builder.AddServiceBusClient(Configuration.GetConnectionString("ServiceBus"));
    });
  }
```

In `WeatherForecastController.cs` inject the client into the constructor:

```csharp
  private readonly Azure.Messaging.ServiceBus.ServiceBusClient _serviceBusClient;
  
  public WeatherForecastController(..., 
      Azure.Messaging.ServiceBus.ServiceBusClient serviceBusClient)
  {
    ...
    _serviceBusClient = serviceBusClient;
  }
```

Then change the request handler to async and send a simple message:

```csharp
  [HttpGet]
  public async Task<string> Get(System.Threading.CancellationToken cancellationToken)
  {
    _logger.LogInformation(2001, "TRACING DEMO: WebApp API weather forecast request forwarded");
    await using var sender = _serviceBusClient.CreateSender("demo-queue");
    await sender.SendMessageAsync(new Azure.Messaging.ServiceBus.ServiceBusMessage("Demo Message"), cancellationToken);
    return await _httpClient.GetStringAsync("https://localhost:44301/WeatherForecast", cancellationToken);
  }
```

Add the primary connection string (taken from Azure, as above) to `appsettings.Development.json`, or pass in via the command line as below:

```json
  "ConnectionStrings": {
    "ServiceBus": "Endpoint=sb://sb-tracedemo-0xacc5-dev.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=5X3...ug="
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

Also configure the service bus in `Program.cs`:

```csharp
using Microsoft.Extensions.Azure;
...

  .ConfigureServices((hostContext, services) =>
  {
    ...
    services.AddAzureClients(builder =>
    {
      builder.AddServiceBusClient(hostContext.Configuration.GetSection("ConnectionStrings:ServiceBus")
          .Value);
    });
  });
```

Inject the service bus client into `Worker.cs`:

```csharp
  private readonly Azure.Messaging.ServiceBus.ServiceBusClient _serviceBusClient;

  public Worker(ILogger<Worker> logger, Azure.Messaging.ServiceBus.ServiceBusClient serviceBusClient)
  {
    ...
    _serviceBusClient = serviceBusClient;
  }
```

And add the following code to the start of the `ExecuteAsync` method, to log received messages:

```csharp
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await using var serviceBusProcessor = _serviceBusClient.CreateProcessor("demo-queue");
    serviceBusProcessor.ProcessMessageAsync += args =>
    {
        _logger.LogInformation(2003, "TRACING DEMO: Message received: {MessageBody}", args.Message.Body);
        return Task.CompletedTask;
    };
    serviceBusProcessor.ProcessErrorAsync += args =>
    {
        _logger.LogError(5000, args.Exception, "TRACING DEMO: Service bus error");
        return Task.CompletedTask;
    }; 
    await serviceBusProcessor.StartProcessingAsync(stoppingToken);
    ...
```

Also comment out the logging that happens every second of the loop, to avoid cluttering up the output:

```csharp
  while (!stoppingToken.IsCancellationRequested)
  {
      //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
      await Task.Delay(1000, stoppingToken);
  }
```

Add the Azure message bus primary connection string to `appsettings.Development.json`  (or pass in via the command line as in the **Run all three applications** section):

```json
  "ConnectionStrings": {
    "ServiceBus": "Endpoint=sb://sb-tracedemo-0xacc5-dev.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=5X3...ug="
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

### No automatic tracing with base Azure message bus

Although the Azure message bus documentation talks about "Service Bus calls done by your service are automatically tracked and correlated", and does provide tracing instrumentation points, the tracing is only automatic if you are using a tracing provider, such as Application Insights or OpenTelemetry (see below). See https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-end-to-end-tracing?tabs=net-standard-sdk-2

If you do not have a tracing provider, then traces are not directly correlated (and activities aren't even used if there is no `DiagnosticsListener` attached). Internally `Azure.Messaging.ServiceBus` uses a subclass of `Activity` that records linked activities from the incoming messages, rather than directly setting the parent (and only if there is an active listener).

For manual correlation, the `Diagnostic-Id` application property is being used, originally from the HTTP Correlation protocol, but with a note that it is being replace by W3C Trace Correlation (i.e. it now contains `traceparent`), https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.DiagnosticSource/src/HttpCorrelationProtocol.md

The `Diagnostic-Id` is automatically set when sending messages with the `traceparent` details of the source activity, so it is relatively easy to set manually. Add the following to the beginning of the message processing code to start an `Activity` set with the provided parent.

```csharp
  serviceBusProcessor.ProcessMessageAsync += args =>
  {
    using var activity = new System.Diagnostics.Activity("ServiceBusProcessor.ProcessMessage");
    if (args.Message.ApplicationProperties.TryGetValue("Diagnostic-Id", out var objectId) &&
      objectId is string traceparent)
    {
      activity.SetParentId(traceparent);
    }
    activity.Start();

    _logger.LogInformation(2003, "TRACING DEMO: Message received: {MessageBody}", args.Message.Body);
    return Task.CompletedTask;
  };
```

### Run all three applications

Instead of updating the `appsettings.json` file, you can also put the connection string into a PowerShell variable, and then pass it to the projects from the command line.

```powershell
$OrgId = "0x$($(az account show --query id --output tsv).Substring(0,4))"
$connectionString = (az servicebus namespace authorization-rule keys list -g rg-tracedemo-dev-001 --namespace-name sb-tracedemo-$OrgId-dev --name RootManageSharedAccessKey --query primaryConnectionString -o tsv)
$connectionString
```

Console worker:

```powershell
dotnet run --project Demo.Worker --environment Development --ConnectionStrings:ServiceBus $connectionString
```

Back end service:

```powershell
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development
```

And web app + api:

```powershell
dotnet run --project Demo.WebApp --urls "https://*:44302" --environment Development --ConnectionStrings:ServiceBus $connectionString
```

Generate some activity from the front end at `https://localhost:44302/fetch-data`, and then
check the results in Kibana `http://localhost:5601/`

The application log messages from `Demo.WebApp`, `Demo.Service`, and `Demo.Worker` will all have
the same `trace.id` distributed trace context correlation identifier.

![Elasticsearch and Kibana showing correlated messages from web API, back end, and message bus](images/elasticsearch-kibana-with-message-bus.png)

### Aside: Other notes on correlation

Other examples, for the older `WindowsAzure.ServiceBus` show separate `ParentId` and `RootId` properties, as this older library is not automatically instrumented, https://docs.microsoft.com/en-us/azure/azure-monitor/app/custom-operations-tracking#service-bus-queue

There is also a draft standard for binding W3C Trace Context to AMQP, which uses a binary format and includes an initial setting as application properties, but allows overriding by brokers as message annotations, https://w3c.github.io/trace-context-amqp/

Azure message bus supports AMQP as an underlying transport, as well as other formats, and while it does have application properties they are text only. There may still be some work to do for interoperable standardisation.

