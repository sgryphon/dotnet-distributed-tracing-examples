
**Dotnet Distributed Tracing Examples**

Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.

(d) Complex OpenTelemetry example
=================================


Requirements
------------

* Dotnet 6.0
* Docker (with docker compose), for local services


Configure a basic collector service
-----------------------------------

### OpenTelemetry Collector configuration


### Docker compose configuration


Configure tracing
-----------------

OpenTelemetry can be used to automatically instrument the application, and provide full instrumentation.

### Add packages

First of all each project needs the basic OpenTelemetry libraries, relevant instrumentation packages, and exporters:

```bash
dotnet add Demo.WebApp package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.3.0-beta.1
dotnet add Demo.WebApp package OpenTelemetry.Exporter.OpenTelemetryProtocol.Logs --version 1.0.0-rc9.3

dotnet add Demo.Service package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.3.0-beta.1
dotnet add Demo.Service package OpenTelemetry.Exporter.OpenTelemetryProtocol.Logs --version 1.0.0-rc9.3

dotnet add Demo.Worker package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.3.0-beta.1
dotnet add Demo.Worker package OpenTelemetry.Exporter.OpenTelemetryProtocol.Logs --version 1.0.0-rc9.3
```

### Add configuration

Add a configuration section to `appsettings.Development.json`:

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

Run the example
---------------

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

