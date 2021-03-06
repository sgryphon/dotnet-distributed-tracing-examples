# Dotnet Distributed Tracing Examples

Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.

## b) Jaeger

An OpenTelemetry example, exporting trace information to Jaeger for graphical display of timelines and application architecture.

Note that Jaeger only supports activity traces, not log records, so you need to combine it with a logging solution such as Elasticsearch.


### Requirements

* Dotnet 6.0
* Docker (with docker compose), for local services

### Run local Jaeger service

You need to run the Jaeger service to send distributed tracing information to. For example on Linux a docker compose configuration is provided. For more details see https://www.jaegertracing.io/

To run the Jaeger service:

```sh
docker compose -p demo up
```

To check the Jaeger UI, browse to `http://localhost:16686/`


### Configure Jaeger exporter

A nuget package is available with the exporter.

```
dotnet add Demo.Service package OpenTelemetry.Exporter.Jaeger
dotnet add Demo.WebApp package OpenTelemetry.Exporter.Jaeger
```

Note that Jaeger only supports traces, not logging. (The OpenTelemetry logging specification, particularly OLTP, is not finalised).

Change both Demo.Service and Demo.WebApp in `Program.cs`

Remove the logging configuration. This will restore the default console logger.

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

### Jaeger trace timelines

![](images/jaeger-traces.png)

Activity traces can be searched and shown in Jaeger. Graphs of recent traces are available, showing outliers (that took excessive time). And individual trace (see the trace ID in the URL) can be drilled into to see the spans it contains and the relationship between the spans.

Jaeger only reports traces, and although it has lots of details about the spans (tags, etc), it needs to be combined with a logging solution (e.g. Elasticsearch). You can correlate the trace ID between the two systems, e.g. when logging indicates an error, you can view all the related spans in Jaeger to diagnose.

### Jaeger service architecture

![](images/jaeger-architecture.png)

Jaeger will also display an architecture, showing the components and relationships between them (where there are parent-child trace span relationships). This can be very useful to understand the actual relationships and calls between your services.




