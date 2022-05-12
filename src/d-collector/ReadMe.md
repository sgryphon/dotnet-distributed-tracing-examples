
**Dotnet Distributed Tracing Examples**

Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.

(d) Complex OpenTelemetry example
=================================

TODO: Intro


Requirements
------------

* Dotnet 6.0
* Docker (with docker compose), for local services
* Azure subscription, for cloud services
* Azure CLI, to create cloud resources
* Powershell, for running scripts


Run local services (Elasticsearch, Jaeger, RabbitMQ, and PostgreSQL)
--------------------------------------------------------------------

This example has the same architecture and set of docker dependencies as the complex OpenTelemetry example, with local Rabbit MQ, for a service bus, and PostgreSQL (and Adminer), for a database, along with Jaeger for traces and Elasticsearch (and Kibana) for logs.

You can start the dependencies via docker compose:

```bash
docker compose -p demo up -d
```

The OpenTelemetry Collector service will also be run in docker, but it will be run separately, after the application is configured.


Set up Azure Monitor workbench and Application Insights
-------------------------------------------------------

Log in to your Azure resources if necessary

```powershell
az login
```

Then use the script to create the required resources, which will also output the required connection string.

```powershell
$VerbosePreference = 'Continue'
./deploy-infrastructure.ps1
```

This will create an Azure Monitor Log Analytics Workspace, and then an Application Insights instance connected to it.

You can log in to the Azure portal to check the logging components were created at `https://portal.azure.com`


Configure OTLP exporters 
------------------------

Building on the complex example where OpenTelemetry instrumentation is already configured, we configure exporters, via the OpenTelemetry Protocol (OTLP) to a local collector.

### Add packages

First of all each project needs to add the OpenTelemetry Protocol exporters:

```bash
dotnet add Demo.WebApp package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.3.0-beta.1
dotnet add Demo.WebApp package OpenTelemetry.Exporter.OpenTelemetryProtocol.Logs --version 1.0.0-rc9.3

dotnet add Demo.Service package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.3.0-beta.1
dotnet add Demo.Service package OpenTelemetry.Exporter.OpenTelemetryProtocol.Logs --version 1.0.0-rc9.3

dotnet add Demo.Worker package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.3.0-beta.1
dotnet add Demo.Worker package OpenTelemetry.Exporter.OpenTelemetryProtocol.Logs --version 1.0.0-rc9.3
```

### Add configuration

Add a configuration section to `appsettings.Development.json`, with the default OTLP port:

```json
  "OpenTelemetry": {
    "OtlpExporter": {
      "Endpoint": "http://localhost:4317/",
      "ExportProcessorType": "Batch",
      "Protocol": "grpc"
    }
  }
```

### OpenTelemetry collector tracing

In `Program.cs`, in the OpenTelemetry configuration, include `AddOtlpExporter()`, binding the options to the configuration section:

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
...

builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
        ...
        .AddOtlpExporter(otlpExporterOptions =>
        {
            builder.Configuration.GetSection("OpenTelemetry:OtlpExporter").Bind(otlpExporterOptions);
        });
});
```

### OpenTelemetry collector logging

Instead of writing direct to Elasticsearch, configure logging with `AddOpenTelemetry()`, and then configure `AddOtlpExporter()`, with the same options section. Also turn on settings to include formatted messages, scopes, and state values.

```csharp
using OpenTelemetry.Logs;
...

builder.Logging
    .AddOpenTelemetry(configure =>
    {
        configure
            .SetResourceBuilder(resourceBuilder)
            .AddOtlpExporter(otlpExporterOptions =>
            {
                builder.Configuration.GetSection("OpenTelemetry:OtlpExporter").Bind(otlpExporterOptions);
            });
        configure.IncludeFormattedMessage = true;
        configure.IncludeScopes = true;
        configure.ParseStateValues = true;
    });
```

Configure the collector service
-------------------------------

Before running the collector, you need to create a configuration file, `otel-collector-config.yaml`, with an OTLP receiver and the exporters you need, e.g. Jaeger and Azure Monitor.

```yaml
receivers:
  otlp:
    protocols:
      grpc:
      http:

processors:
  batch:

exporters:
  azuremonitor:
    instrumentation_key:
    endpoint: 
  jaeger:
    endpoint: jaeger:14250
    tls:
      insecure: true
  logging:
    logLevel: info

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [logging, jaeger, azuremonitor]
    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [logging, azuremonitor]
```



Run the example
---------------

### Run the OpenTelemetry Collector

```bash
docker run -it --rm \
  --network demo_default \
  -p 4317:4317 \
  -v $PWD/otel-collector-config.yaml:/etc/otel/config.yaml \
  otel/opentelemetry-collector-contrib
```

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

And run the worker app in a fourth terminal

```powershell
dotnet run --project Demo.Worker --environment Development
```

Generate some activity via the front end at `https://localhost:44303/fetch-data`.

#### Using tmux

There is also a combined script that will use **tmux** to open a split window with both projects running:

```bash
./start-collector-demo.sh
```




https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/elasticsearchexporter
https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/azuremonitorexporter

https://jessitron.com/2021/08/11/run-an-opentelemetry-collector-locally-in-docker/



docker exec -it demo-opentelemetry-collector-1 cat /etc/otel/config.yaml


https://github.com/open-telemetry/opentelemetry-collector-releases/blob/main/distributions/otelcol-contrib/manifest.yaml

