# Example application (2024): Blazor sample

## Pre-requisites

- Git, for source code (`winget install Git.Git --source winget`)
- .NET 8 SDK, for the server (`winget install Microsoft.DotNet.SDK.8`)
- NVM (`winget install CoreyButler.NVMforWindows`), or another node version manager
- Node.JS, for the front end (`nvm use latest`)
- Podman, Docker, or another container runtime (`winget install Redhat.Podman`)
  - Podman-compose, for local dev dependencies (install Python,
    `winget install -e --id Python.Python.3.11`, then in a new console,
    `pip3 install podman-compose`)
- An editor, e.g. VS Code (`winget install Microsoft.VisualStudioCode`)
  - VS Code plugins: Prettier, CSharpier
- PowerShell 7+, for running scripts (`winget install Microsoft.PowerShell`)
- Azure Data Studio, or similar, for PostgreSQL administration
  (`winget install Microsoft.AzureDataStudio`)

## Run the app

First run the dependencies via a container framework:

```powershell
podman machine init
podman machine start

podman-compose up -d
```

To run the back end:

```powershell
dotnet run --project Demo.WebApi -- --urls "http://*:8005;https://*:44305/"
```

To run the front end:

```powershell
dotnet run --project Demo.BlazorApp -- --urls "http://*:8004;https://*:44304/"
```

Test the back end API at <https://localhost:44305/swagger/index.html>

Access the app at <https://localhost:44304/>

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




