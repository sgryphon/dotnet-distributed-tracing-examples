# Comparison of OpenTelemetry vs Serilog

## Pre-requisites

- Git, for source code (`winget install Git.Git --source winget`)
- .NET 8 SDK, for the server (`winget install Microsoft.DotNet.SDK.8`)
- Podman, Docker, or another container runtime (`winget install Redhat.Podman`)
  - Podman-compose, for local dev dependencies (install Python,
    `winget install -e --id Python.Python.3.11`, then in a new console,
    `pip3 install podman-compose`)
- An editor, e.g. VS Code (`winget install Microsoft.VisualStudioCode`)
- PowerShell 7+, for running scripts (`winget install Microsoft.PowerShell`)

## Run the app

First run the dependencies via a container framework:

* Seq, for logging <http://localhost:8341/>, admin / seqdev123
* TODO: Aspire

```powershell
podman machine init
podman machine start

podman-compose up -d
```

To run the back end:

```powershell
dotnet tool restore

dotnet run --project Demo.WebApi -- --urls "http://*:8002;https://*:44302" --environment Development
```

Access the app at <http://localhost:44302/weatherforecast>, then view the results in Seq.

To run a specific log configuration (`serilog-seq`, `serilog-otlp`, `otel-otlp`):

```
dotnet run --project Demo.WebApi -- --urls "http://*:8002;https://*:44302" --environment Development --log serilog-otlp
```

## App creation

```powershell
mkdir compare-serilog
cd compare-serilog
dotnet new sln 
dotnet new webapi -o Demo.WebApi
dotnet sln add Demo.WebApi
```

### Add custom logging

Inside the request handler get a logger and write to it, with some sample semantic value:

```csharp
app.MapGet("/weatherforecast", () =>
{
    var logger = app.Services.GetService<ILogger<Program>>();
    logger.LogInformation(1001, "Weather Requested {WeatherGuid}", Guid.NewGuid());

    var forecast =  Enumerable.Range(1, 5).Select(index =>
```

### Add Serilog

Add the Serilog library with the native Seq connector:

```powershell
dotnet add Demo.WebApi package Serilog
dotnet add Demo.WebApi package Serilog.AspNetCore
dotnet add Demo.WebApi package Serilog.Sinks.Console
dotnet add Demo.WebApi package Serilog.Sinks.Seq
```

Based on a passed in configuration parameter, configure Serilog to write to Seq, 
using the native Sink. Also write to the console, so we can see the output, as
Serilog removes the default loggers.

```csharp
using Serilog;

// ...

var logConfig = builder.Configuration.GetSection($"Log")?.Value;

if (string.Equals(logConfig, "serilog-seq", StringComparison.OrdinalIgnoreCase))
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.Seq("http://localhost:5341")
        .CreateLogger();
    Log.Information("Serilog Seq configured");
    builder.Services.AddSerilog();
}

var app = builder.Build();
```

### Serilog OTLP

Serilog also supports an OTLP sink, and Seq supports OTLP, so that can be used as an alternative.

```powershell
dotnet add Demo.WebApi package Serilog.Sinks.OpenTelemetry
```

Use a differenter parameter value to configure this logger:

```csharp
using Serilog.Sinks.OpenTelemetry;

// ...

if (string.Equals(logConfig, "serilog-otlp", StringComparison.OrdinalIgnoreCase))
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(options => {
            options.Endpoint = "http://localhost:5341/ingest/otlp/v1/logs";
            options.Protocol = OtlpProtocol.HttpProtobuf;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "weather-demo"
            };
        })
        .CreateLogger();
    Log.Information("Serilog OTLP configured");
    builder.Services.AddSerilog();
}
```
