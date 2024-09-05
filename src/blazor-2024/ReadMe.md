# Example application (2024): Blazor sample

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

First run the dependencies via a container framework. Check the Aspire logs to find the security key to access

```powershell
podman machine init
podman machine start

podman-compose up -d
podman logs blazor-2024_aspire-dashboard_1
```

**NOTE:** I had some trouble with podman-compose, but `podman pod ps` showed the pod had been created, and `podman pod start pod_blazor-2024` started it running.

Access Aspire app using the token output in the logs, i.e. <http://localhost:18888/login?t=__token_from_logs__>

To run the back end:

```powershell
dotnet run --project Demo.WebApi -- --urls "http://*:8005;https://*:44305/"
```

To run the front end:

```powershell
dotnet run --project Demo.BlazorApp -- --urls "http://*:8004;https://*:44304/"
```

Test the back end API at <https://localhost:44305/swagger/index.html>

Access the app at <https://localhost:44304/> and view the Weather page a few times.

## Azure Monitor demonstration

Need to configure a resource in your Azure subscription and get the connection string:

```powershell
az login
az account set --subscription <subscription id>
$VerbosePreference = 'Continue'
./deploy-shared.ps1
```

Configure OpenTelemetry with Azure Monitor (both the BlazorApp and Web API), instead of OTLP:

```csharp

```

Pass in the connection string in the application settings:

```powershell
```

Cleanup:

```powershell
./remove-shared.ps1
```

## App creation

Blazor from <https://dotnet.microsoft.com/en-us/learn/aspnet/blazor-cli-tutorial/intro>

Blazor logging from <https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/logging>

Web service from <https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api>

OpenTelemetry & Aspire from: <https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-otlp-example>

```powershell
mkdir blazor-2024
cd blazor-2024
dotnet new sln 
dotnet new blazor -o Demo.BlazorApp
dotnet sln add Demo.BlazorApp
dotnet new webapi --use-controllers -o Demo.WebApi
dotnet sln add Demo.WebApi
```

### Wire up Blazor to Web API

Add `HttpClient` to Blazor `Program.cs`

```csharp
builder.Services.AddHttpClient();
```

Inject to `Weather.razor`

```razor
@inject ILogger<Weather> logger
@inject System.Net.Http.HttpClient httpClient
```

Replace weather generation in `Weather.razor` with call to the Web API, with some logging:

```csharp
protected override async Task OnInitializedAsync()
{
  logger.LogWarning(4001, "TRACING DEMO: BlazorApp weather forecast request forwarded");
  forecasts = await httpClient.GetFromJsonAsync<WeatherForecast[]>("https://localhost:44305/WeatherForecast");
  logger.LogWarning(4003, "TRACING DEMO: Weather done");
}
```
Add some logging in Web API `WeatherForecastController.cs`:

```csharp
[HttpGet(Name = "GetWeatherForecast")]
public IEnumerable<WeatherForecast> Get()
{
  _logger.LogWarning(4102, "TRACING DEMO: Back end Web API weather forecast requested");
  ...
```

### Add Activity and Metrics sample code

To demonstrate traces and metrics, as well as logs, add some of the observability sample code, adapted for our scenario.

In `WeatherForceastController.cs`, reference the needed namespaces, add some static fields, and then use them when the request is received:

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;

...

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly Meter weatherMeter = new Meter("OTel.Example", "1.0.0");
    private static readonly Counter<int> countWeatherCalls = weatherMeter.CreateCounter<int>("weather.count", description: "Counts the number of times weather was called");
    private static readonly ActivitySource weatherActivitySource = new ActivitySource("OTel.Example");

    ...

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        using var activity = weatherActivitySource.StartActivity("WeatherActivity");
        countWeatherCalls.Add(1);
        activity?.SetTag("weather", "Hello World!");

        ...
```

Note that these are all standard .NET diagnostics, with no references to OpenTelemetry yet.

### Configure for OpenTelemetry

To configure OpenTelemetry, all you need to do is add the libraries on start up and configure the automatic instrumentation for the components you are using.

A lot of the information (traces, metrics, etc) are automatically built in to .NET. Once configured, OpenTelemetry will also pick up any custom diagnostics you have in your own code, for example and logging using `ILogger<T>` (or `LoggerMessage`).

The above example also has some custom trace spans and custom metrics, but they are necessary -- if you don't have them, you will still get all the automatic .NET instrumentation.

Add the libraries we will be using. You need to add them to both projects, i.e. both `Demo.BlazorApp.csproj` and `Demo.WebApi.csproj`.

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.9.0" />
</ItemGroup>
```

Add OpenTelemetry to .NET logging, then add the OpenTelemetry service and configure metrics, tracing, and the default OTLP exporter.

Note that the BlazorApp doesn't use the custom meter & trace source (so they aren't needed in the configuration). You will still see all the built in .NET traces and metrics for the BlazorApp.

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

...

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

var otel = builder.Services.AddOpenTelemetry();
otel.WithMetrics(metrics =>
{
    metrics.AddAspNetCoreInstrumentation();
    metrics.AddMeter("OTel.Example");
    metrics.AddMeter("Microsoft.AspNetCore.Hosting");
    metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
});
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
    tracing.AddSource("OTel.Example");
});
otel.UseOtlpExporter();
```

Nothing more needs to be configured for the basic demo because the default OTLP exporter uses the OpenTelemetry default destination of `http://localhost:4317`, where Aspire is configured to listen.
