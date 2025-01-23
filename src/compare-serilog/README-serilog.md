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
* Aspire Dashboard, for logging, <http://localhost:18888/>, get the access token via `podman logs compare-serilog_aspire-dashboard_1`

```powershell
podman machine init
podman machine start

podman-compose up -d
```

To run the back end (logging to console only):

```powershell
dotnet tool restore

dotnet run --project Demo.WebApi -- --urls "http://*:8002;https://*:44302" --environment Development
```

Access the app at <http://localhost:44302/weatherforecast> to see the console logs.

To see logging and tracing to the two backends, use the `--log` argument to configure specific loggers:
- `serilog-seq`: Serilog using the Seq exporter (sink)
- `serilog-otlpseq`: Serilog using the OTLP exporter to Seq endpoint
- `serilog-otlp`: Serilog using OTLP to the default (gRPC) endpoint, e.g. Aspire Dashboard
- `otel-otlpseq`: OpenTelemetry using OTLP to the Seq logs endpoint
- `otel-otlp`: OpenTelemetry using default OTLP endpoint, e.g. Aspire Dashboard

And the `--trace` argument to configure the tracing:
- `serilog`: Forward traces via Serilog, which will use whatever logger is configured (above)
- `otel-otlpseq`: OpenTelemetry using OTLP to the Seq traces endpoint
- `otel-otlp`: OpenTelemetry using default OTLP endpoint, e.g. Aspire Dashboard

### Example combinations

Example: Seq exporter via Serilog

```
dotnet run --project Demo.WebApi -- --urls "http://*:8002;https://*:44302" --environment Development --log serilog-seq --trace serilog
```

Example: OTLP to Seq via OpenTelemetry

```
dotnet run --project Demo.WebApi -- --urls "http://*:8002;https://*:44302" --environment Development --log otel-otlpseq --trace otel-otlpseq
```

Example: OTLP to ApireDashboard via Serilog

```
dotnet run --project Demo.WebApi -- --urls "http://*:8002;https://*:44302" --environment Development --log serilog-otlp --trace serilog
```

Example: Logs using OTLP to Seq via Serilog, and Traces using OTLP to Seq via OpenTelemetry

```
dotnet run --project Demo.WebApi -- --urls "http://*:8002;https://*:44302" --environment Development --log serilog-otlpseq --trace otel-otlpseq
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

### Add Serilog logging (Seq connector)

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

var logConfig = (builder.Configuration.GetSection($"Log")?.Value ?? "")
    .ToLowerInvariant().Split(',').ToList();

if (logConfig.Contains("serilog-seq"))
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

### Serilog OTLP to Seq

Serilog also supports an OTLP sink, and Seq supports OTLP, so that can be used as an alternative.

```powershell
dotnet add Demo.WebApi package Serilog.Sinks.OpenTelemetry
```

Use a differenter parameter value to configure this logger:

```csharp
using Serilog.Sinks.OpenTelemetry;

// ...

if (logConfig.Contains("serilog-otlpseq"))
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

### Serilog OTLP to Aspire Dashboard

If we don't specify an endpoint, then the default is gRPC to port 4317, which is where
Aspire Dashboard is configured.

```csharp
if (logConfig.Contains("serilog-otlp"))
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(options => {
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "weather-demo-serilog-otlp"
            };
        })
        .CreateLogger();
    Log.Information("Serilog OTLP (default) configured");
    builder.Services.AddSerilog();
}
```

### OpenTelemetry logging to Seq

Add the OpenTelemetry packages:

```powershell
dotnet add Demo.WebApi package OpenTelemetry
dotnet add Demo.WebApi package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add Demo.WebApi package OpenTelemetry.Extensions.Hosting
```

And configure OpenTelemetry to export via OTLP to the Seq endpoint. Note that
OpenTelemetry doesn't replace the default loggers, so console logging remains
at the default.

```csharp
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

// ...

var otel = builder.Services.AddOpenTelemetry();
otel.ConfigureResource(resource => resource.AddService(serviceName: "weather-demo-otel"));

if (logConfig.Contains("otel-otlpseq"))
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.AddOtlpExporter(opt => {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri("http://localhost:5341/ingest/otlp/v1/logs");
        });
    });
}
```

### OpenTelemetry logging to Aspire Dashboard

The default OTLP configuration is to localhost on standard OTLP ports, so we just add the default exporter.

```csharp
if (logConfig.Contains("otel-otlp"))
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.AddOtlpExporter();
    });
}
```

## Tracing

Add custom activity tracing to the application using standard .NET activity source.

Similar to using `ILogger<T>`, your application code should use the standard .NET
`ActivitySource()`, irrespective of how logging/tracing is configured.

This way while your host has a dependency on the logging solution you choose, none
of your application code has an external dependency.

```csharp
app.MapGet("/weatherforecast", () =>
{
    var activitySource = new ActivitySource("Weather.Source");
    using var activity = activitySource.StartActivity("Weather Trace {UnixTimeSeconds}");
    activity?.SetTag("UnixTimeSeconds", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(1001, "Weather Requested {WeatherGuid}", Guid.NewGuid());
```

### Serilog tracing to Seq

Reference the SerilogTracing packages:

```powershell
dotnet add Demo.WebApi package SerilogTracing
dotnet add Demo.WebApi package SerilogTracing.Expressions
dotnet add Demo.WebApi package SerilogTracing.Instrumentation.AspNetCore
```

Serilog tracing is sent via Serilog logger; we are using the shared logger,
which means the same configuration for both. i.e. passing `--trace serilog`
will send to Seq via either the Seq exporter or OTLP, depending on which
was specified.

```csharp
using SerilogTracing;

// ...

var traceConfig = (builder.Configuration.GetSection($"Trace")?.Value ?? "")
    .ToLowerInvariant().Split(',').ToList();

IDisposable? activityListenerHandle = null;
if (traceConfig.Contains("serilog"))
{
    // Destination of the traces uses the corresponding log definition (above)
    activityListenerHandle  = new ActivityListenerConfiguration()
        .Instrument.AspNetCoreRequests()
        .TraceToSharedLogger();
    Log.Information("Serilog tracing configured");
}
```

### Serilog tracing to Aspire Dashboard

To send Serilog to Aspire Dashboard, we use the default OTLP configuration, however
we also need to suppress to gRPC activity source (to avoid a loop of activities).

```csharp
if (logConfig.Contains("serilog-otlp"))
{
    Log.Logger = new LoggerConfiguration()
        // This logger uses the default gRPC sink, so suppress gRPC activity source,
        // so we don't get a loop (if tracing enabled)
        .MinimumLevel.Override("Grpc.Net.Client", LogEventLevel.Warning)

        // ...
```

### OpenTelemetry tracing to Seq

Reference the OpenTelemetry tracing packages:

```powershell
dotnet add Demo.WebApi package OpenTelemetry.Instrumentation.AspNetCore
dotnet add Demo.WebApi package OpenTelemetry.Instrumentation.Http
```

Configure tracing, with our custom source, moving the initial configuration of
OpenTelemetry into a shared section, and then adding components for logging and
tracing as needed

```csharp
using System.Diagnostics;
using OpenTelemetry.Trace;

// ...

if (traceConfig.Contains("otel-otlpseq"))
{
    otel.WithTracing(tracing =>
    {
        tracing.AddSource("Weather.Source");
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddOtlpExporter(opt => {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri("http://localhost:5341/ingest/otlp/v1/traces");
        });
    });
}
```

### OpenTelemetry tracing to Aspire Dashboard

Default configuration, similar to OpenTelemetry logging.

```csharp
if (traceConfig.Contains("otel-otlp"))
{
    otel.WithTracing(tracing =>
    {
        tracing.AddSource("Weather.Source");
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddOtlpExporter();
    });
}
```

## References

https://github.com/serilog/serilog-sinks-opentelemetry/blob/dev/example/Example/Program.cs

https://github.com/datalust/seq-examples/blob/main/client/opentelemetry-csharp-sdk-to-seq/Example.WeatherService/Program.cs

https://docs.datalust.co/docs/opentelemetry-net-sdk-1


