# Dotnet Distributed Tracing Examples
Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.


## Requirements

* Dotnet 5.0
* Docker (with docker-compose), for local services
* Azure subscription, for cloud services
* Azure CLI, to create cloud resources
* Powershell, for running scripts

## 1) Basic example

Front end is a little special, so lets just start with server to server calls. Distributed trace correlation is already built into the recent versions of dotnet.

**NOTE:** If you have trouble with HTTPS, or do not have certificats set up, then see the section at
the end of this file for HTTPS Developer Certificates.

### New back end service

Create a development certificate (if you are using a different shell, replace the PowerShell variable):

Create a directory for the project and a solution file

```sh
mkdir 1-basic
cd 1-basic
dotnet new sln
dotnet new webapi --output Demo.Service
dotnet sln add Demo.Service
```

Check it works:

```sh
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development
```

Test it in a browser at `https://localhost:44301/WeatherForecast`

### New web + api server

In another terminal:

```sh
cd 1-basic
dotnet new react --output Demo.WebApp
dotnet sln add Demo.WebApp
```

Check it works:

```sh
dotnet run --project Demo.WebApp --urls "https://*:44302" --environment Development
```

Test it in a browser at `https://localhost:44302` 

### Changes - web app front end

In the Demo.WebApp project, at the end of the main return in `FetchData.js`, add a button to make it easy to call the server.

```javascript
  return (
    <div>
      ...
      <p><button className="btn btn-primary" onClick={() => this.populateWeatherData()}>Refresh</button></p>
    </div>
  );
```

### Changes - web app api

Rather than return the data directly, have the web app API log a message and forward the call to the service.

Note that `HttpClient` should never be used directly, but via the built in factory to ensure the correct lifecycle is applied. Register the system factory in `Startup.cs`:

```csharp
  public void ConfigureServices(IServiceCollection services)
  {
      ...
      services.AddHttpClient();
  }
```

Modify `WeatherForecastController.cs` in the web app to inject `HttpClient`:


```csharp
  private readonly System.Net.Http.HttpClient _httpClient;
  private readonly ILogger<WeatherForecastController> _logger;

  public WeatherForecastController(ILogger<WeatherForecastController> logger, 
      System.Net.Http.HttpClient httpClient)
  {
      _logger = logger;
      _httpClient = httpClient;
  }
```

Then replace the `Get()` method with the following:

```csharp
  [HttpGet]
  public Task<string> Get(System.Threading.CancellationToken cancellationToken)
  {
      _logger.LogInformation(2001, "TRACING DEMO: WebApp API weather forecast request forwarded");
      return _httpClient.GetStringAsync("https://localhost:44301/WeatherForecast", cancellationToken);
  }
```

Finally, in `appSettings.Development.json`, configure logging to include scopes:

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

### Changes - service

Add log statements in the service `WeatherForecastController.cs`:

```csharp
  public IEnumerable<WeatherForecast> Get()
  {
    _logger.LogInformation(2002, "TRACING DEMO: Back end service weather forecast requested");
    ...
  }
```

And include scope logging in `appSettings.Development.json`:

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

### Run the two services

In separate terminals run the service:

```sh
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development
```

And web app + api:

```sh
dotnet run --project Demo.WebApp --urls "https://*:44302" --environment Development
```

And check the front end at `https://localhost:44302/fetch-data`

### Distributed tracing is built in

Without any additional configuration, trace correlation is automatically passed between the services. In the logging output of the back end service you can see the same TraceId as the web app.

```
info: Demo.Service.Controllers.WeatherForecastController[2002]
      => SpanId:79f874d8bb5c7745, TraceId:4cc0769223865d41924eb5337778be25, ParentId:cf6a9d1f30334642 => ConnectionId:0HMC18204SUS0 => RequestPath:/WeatherForecast RequestId:0HMC18204SUS0:00000002 => Demo.Service.Controllers.WeatherForecastController.Get (Demo.Service)
      Back end service weather forecast requested
```


## 2) Local logger - Elasticsearch

Distributed trace correlation is also supported out of the box by many logging providers.

For example, you can run a local Elasticsearch service to send logs to from multiple services, so they can be viewed together.

### Run local Elasticsearch and Kibana

You need to be running Elasticsearch and Kibana, for example on Linux a docker compose 
configuration is provided. There are a number of prerequesites that you will need to meet, 
such as enough file handles; the elk-docker project provides a good list, including 
some troubleshooting (see https://elk-docker.readthedocs.io/).

For example, the most common issue is mmap count limit, which can be changed via: 

```sh
echo vm.max_map_count=262144 | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

Once the prerequisites are satisfied, you can bring up the docker-compose file, which will create two nodes, one for Elasticsearch and one for Kibana:

```sh
docker-compose -p demo up -d
```

To check the Kibana console, browse to `http://localhost:5601`

### Add Elasticsearch logger to app

A logger provider is available that can write directly to Elasticsearch. It can be installed via nuget.

```sh
dotnet add Demo.WebApp package Elasticsearch.Extensions.Logging --version 1.6.0-alpha1
```

To use the logger provider you need add a using statement at the top of `Program.cs`:

```csharp
using Elasticsearch.Extensions.Logging;
```

Then add a `ConfigureLogging` section to the host builder:

```csharp
  Host.CreateDefaultBuilder(args)
    .ConfigureLogging((hostContext, loggingBuilder) =>
    {
        loggingBuilder.AddElasticsearch();
    })
    ...
```

Repeat this for the back end service, adding the package, but the configuration as above:

```sh
dotnet add Demo.Service package Elasticsearch.Extensions.Logging --version 1.6.0-alpha1
```

### Run the two services with Elasticsearch

In separate terminals run the service:

```sh
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development
```

And web app + api:

```sh
dotnet run --project Demo.WebApp --urls "https://*:44302" --environment Development
```

And check the front end at `https://localhost:44302/fetch-data`


### Initialise Kibana and view logs

Once you have run the application and sent some log event, you need to open Kibana (http://localhost:5601/) and initialise the database.

Navigate to **Management** > **Index Patterns** and click **Create index pattern**. Enter "dotnet-*" as the pattern (it should match the entries just created) and click **Next step**. Select the time filter "@timestamp", and click **Create index pattern**.

Once the index is created you can use **Explore** to view the log entries.

If you add the columns `service.type`, `trace.id`, and `message`, then you can see the messages from the web app and back end service are correlated by the trace ID.


## 3) Azure message bus

### Set up a message bus

First of all, you need to log in to your Azure resources:

```powershell
az login
```

There is a PowerShell script that will create the required resources, and output the required connection string.

```powershell
./deploy-azure-servicebus.ps1
```

You can log in to the Azure portal to check your queue was created at `https://portal.azure.com`

#### Azure command details

You can also run the individual Azure commands directly to create a resource group, then a service bus namespace.

You need to use a unique name for the namespace, e.g. the script uses first four characters of your subscription ID, then create queue within that namespace.

```powershell
$suffix = (ConvertFrom-Json "$(az account show)").id.Substring(0,4)
az group create --name demo-tracing-rg --location australiaeast
az servicebus namespace create --resource-group demo-tracing-rg --name demo-trace-$suffix --sku Standard
az servicebus queue create --resource-group demo-tracing-rg --namespace-name demo-trace-$suffix --name demo-queue
```

You will need the primary connection string key to configure in the application:

```powershell
$connectionString = (az servicebus namespace authorization-rule keys list --resource-group demo-tracing-rg --namespace-name demo-trace-$suffix --name RootManageSharedAccessKey --query primaryConnectionString -o tsv)
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
    "ServiceBus": "Endpoint=sb://demo-sg-2243.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=4Xuz5VWZio3pnTtaiA4ngQF/87BdEGwGtK4qE/JUCL0="
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
    "ServiceBus": "Endpoint=sb://demo-sg-2243.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=4Xuz5VWZio3pnTtaiA4ngQF/87BdEGwGtK4qE/JUCL0="
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

Instead of updating the `appsettigns.json` file, you can also put the connection string into a PowerShell variable, and then pass it to the projects from the command line.

```powershell
$connectionString = (az servicebus namespace authorization-rule keys list --resource-group demo-tracing-rg --namespace-name demo-trace-$suffix --name RootManageSharedAccessKey --query primaryConnectionString -o tsv)
$connectionString
```

Console worker:

```powershell
dotnet run --project Demo.Worker --environment Development --ConnectionStrings:ServiceBus $connectionString
```

Back end service:

```powershell
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development --ConnectionStrings:ServiceBus $connectionString
```

And web app + api:

```powershell
dotnet run --project Demo.WebApp --urls "https://*:44302" --environment Development
```

Generate some activity from the front end at `https://localhost:44302/fetch-data`, and then
check the results in Kibana `http://localhost:5601/`

The application log messages from `Demo.WebApp`, `Demo.Service`, and `Demo.Worker` will all have
the same `trace.id` distributed trace context correlation identifier.

### Aside: Other notes on correlation

Other examples, for the older `WindowsAzure.ServiceBus` show separate `ParentId` and `RootId` properties, as this older library is not automatically instrumented, https://docs.microsoft.com/en-us/azure/azure-monitor/app/custom-operations-tracking#service-bus-queue

There is also a draft standard for binding W3C Trace Context to AMQP, which uses a binary format and includes an initial setting as application properties, but allows overriding by brokers as message annotations, https://w3c.github.io/trace-context-amqp/

Azure message bus supports AMQP as an underlying transport, as well as other formats, and while it does have application properties they are text only. There may still be some work to do for interoperable standardisation.

## 4) Using Azure Monitor / Application Insights

### Set up Azure Monitor workbench and Application Insights

Log in to your Azure resources if necessary

```powershell
az login
```

Then use the script to create the required resources, which will also output the required connection string.

```powershell
./deploy-azure.ps1
```

You can log in to the Azure portal to check your queue was created at `https://portal.azure.com`

### Add libraries

Add packages for ApplicationInsights. There are packages for AspNetCore and for WorkerService.

```sh
dotnet add Demo.WebApp package Microsoft.ApplicationInsights.AspNetCore
dotnet add Demo.Service package Microsoft.ApplicationInsights.AspNetCore
dotnet add Demo.Worker package Microsoft.ApplicationInsights.WorkerService
```

### Enable application insights

In `Startup.cs` of the WebApp project:

```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();
            services.AddControllersWithViews();
```

In `Startup.cs` of the Service project:

```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();
            services.AddControllers();
```

In `Program.cs` of the Worker project:

```csharp
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.AddHostedService<Worker>();
```

### Start App Insights operation

Instead of a generic activity, use the App Insights TelemetryClient to start the message received operation in `Worker.cs`. Because we are using an extension method (StartOperation), we need to reference the ApplicationInsights. We then need to inject the TelemetryClient, and using StartOperation (instead of manually starting the Activity).

```csharp
using Microsoft.ApplicationInsights;
...
        private readonly TelemetryClient _telemetryClient;

        public Worker(ILogger<Worker> logger, TelemetryClient telemetryClient, Azure.Messaging.ServiceBus.ServiceBusClient serviceBusClient)
        {
          ...
          _telemetryClient = telemetryClient;
        }
...
            serviceBusProcessor.ProcessMessageAsync += args =>
            {
                using var activity = new System.Diagnostics.Activity("ServiceBusProcessor.ProcessMessage");
                if (args.Message.ApplicationProperties.TryGetValue("Diagnostic-Id", out var objectId) &&
                    objectId is string traceparent)
                {
                    activity.SetParentId(traceparent);
                }
                using var operation = _telemetryClient.StartOperation<RequestTelemetry>(activity);

                _logger.LogInformation(2003, "TRACING DEMO: Message received: {MessageBody}", args.Message.Body);
```

### Add configuration

By default only Warning logs and above are collected. Change the configuration to include Information level.

```json
  "Logging": {
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    },
```

### View Results

Log in to Azure Portal, https://portal.azure.com/

#### Logs

Open the Log Analytics workspace that was created. The default will be under
Home > Resource groups > demo-tracing-rg > trace-demo-logs

Select General > Logs from the left. Dismiss the Queries popup to get to an empty editor.

Note that you may have to wait a bit for logs to be injested and appear in the workspace.

To see the events corresponding to the buttons in the sample app, you can use the following query:

```
union AppDependencies, AppExceptions, AppRequests, AppTraces
| where TimeGenerated  > ago(1h)
| where Properties.CategoryName startswith "Demo." 
| sort by TimeGenerated desc
| project TimeGenerated, OperationId, SeverityLevel, Message, Name, Type, DependencyType, Properties.CategoryName, OperationName, ParentId, SessionId, AppRoleInstance, AppVersion, UserId, ClientType, Id, Properties
```

#### Performance

Open the Application Insights that was created. The default will be under
Home > Resource groups > demo-tracing-rg > trace-demo-app-insights

Select Performance on the left hand menu, then Operation Name "GET WeatherForecast/Get" (the top level operation requesting the page). The right hand side will show the instances.

Click on "Drill into... N Samples" in the bottom right, then select the recent operation.

The page will show the End-to-end transaction with the correlation Operation ID (the same as the console), along with a hierarchical timeline of events.

There will be a top level event for **localhost:44302** with two children for the **Message** and **localhost:44301** (the back end service).

The "View all telemetry" button will show all the messages, including traces.


## HTTPS Developer Certificates

### Windows and macOS

See: https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio#trust-the-aspnet-core-https-development-certificate-on-windows-and-macos

The certificate is automatically installed. To trust the certificate:

```
dotnet dev-certs https --trust
```

### Ubuntu

See: https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio#ubuntu-trust-the-certificate-for-service-to-service-communication

Create the HTTPS developer certificate for the current user personal certificate store (if not already initialised). 

```
dotnet dev-certs https
```

You can check the certificate exists for the current user; the file name is the SHA1 thumbprint. (If you want to clear out previous certificates use `dotnet dev-certs https --clean`, which will delete the file.)

```
ls ~/.dotnet/corefx/cryptography/x509stores/my
```

#### Trust the certificate for server communication

You need to have OpenSSL installed (check with `openssl version`).

Install the certificate. You need to use the `-E` flag with `sudo` when exporting the file, so that it exports the file for the current user (otherwise it will export the file for root, which will be different).

```
sudo -E dotnet dev-certs https -ep /usr/local/share/ca-certificates/aspnet/https.crt --format PEM
sudo update-ca-certificates
```

You can check the file exists, and then use open SSL to verify it has the same SHA1 thumbprint.

```
ls /usr/local/share/ca-certificates/aspnet
openssl x509 -noout -fingerprint -sha1 -inform pem -in /usr/local/share/ca-certificates/aspnet/https.crt
```

If the thumbprints do not match, you may have install the root (sudo user) certificate. You can check it at `sudo ls -la /root/.dotnet/corefx/cryptography/x509stores/my`.

#### Trust in Chrome

```
sudo apt-get install -y libnss3-tools
certutil -d sql:$HOME/.pki/nssdb -A -t "P,," -n localhost -i /usr/local/share/ca-certificates/aspnet/https.crt
certutil -d sql:$HOME/.pki/nssdb -A -t "C,," -n localhost -i /usr/local/share/ca-certificates/aspnet/https.crt
```

#### Trust in Firefox:

```
cat <<EOF | sudo tee /usr/lib/firefox/distribution/policies.json
{
    "policies": {
        "Certificates": {
            "Install": [
                "/usr/local/share/ca-certificates/aspnet/https.crt"
            ]
        }
    }
}
EOF
```
