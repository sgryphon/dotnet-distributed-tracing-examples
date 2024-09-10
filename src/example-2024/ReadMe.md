# Example application (2024): dice roller

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

This sample is a basic dice-rolling application.

First run the dependencies via a container framework:

* Jaeger, for tracing: <http://localhost:16686/>
* Seq, for logging <http://localhost:8341/>, admin / seqdev123

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

Front end, in a separate console:

First you need to have Node.js available, e.g. if you are using a manager you may need to initialise it, then ensure all dependencies are installed, then run the front end client:

```powershell
nvm use latest
pushd demo-web-app; npm install; popd

npm run --prefix demo-web-app dev '--' --port 8003
```

Access the app at <http://localhost:8003>

### Demo behaviour

The dice rolling demo application has a simple server API for rolling polyhedrol (not just cubes) dice.

Open the browser developer Console, to see output of what is happening in the web app, and open Seq and Jaeger to see the logging.

* **Roll 3d6**: Has no explicit tracing in the web app but the auto-instrumentation on Fetch will generate a Trace ID that you can see in the request `traceparent` header and in the console telemetry exporter object. In Seq you will see the same Trace ID. Jaeger show a graph of traces over time by duration and number of spans.

Note that if you don't send a Trace ID from the client, then .NET will start a trace on the server, but the Trace ID won't be known to the client (unless it comes back in something like a ProblemDetails error).

**Multiple calls.** Modern single page apps may make multiple calls to the API from one user action.

* **Roll (1d8)d10**: Makes two requests to the API, the first to roll 1d8 that determines how many d10 to roll (e.g. if the first 1d8 is a 5, the second roll is 5d10). Both requests will have an auto-instrumented Trace ID from the client, but they will be different, and not linked in the back end logs.
* **Roll (1d8)d10, with soan**: To handle this we start a trace span when the user clicks the button, and that same Trace ID is then used for both requests. Both requests have the same Trace ID, and they are correlated in the back end logs. Note that the context manager doesn't support TypeScript `async`/`await`, so you have to use `Promise` continuations to preserve the span context.

## Web client React modules

### Client configuration

Client frameworks usually support configuration via environment variables, however that configuration is usually at build time, rather than deploy time (or run time), and so does not easily support configuration per deployment environment.

To enable this a two-step token replacement approach is taken, injecting configuration values into the root document.

#### Configuration during local development

Local development uses single-step token replacement.

With React / Next.js App Router, configuration properties are injected into `layout.tsx` via an inline script component configured to run beforeInteractive.

```tsx
<head>
  <Script strategy="beforeInteractive" id="windowAppConfig">
    {`window.appConfig = {
        "apiUrl": "` + process.env.NEXT_PUBLIC_API_URL + `",
        "environment": "` + process.env.NEXT_PUBLIC_ENVIRONMENT + `",
        "version": "` + process.env.NEXT_PUBLIC_VERSION + `",
    }`}
  </Script>
</head>
```

Values are injected directly during build from `.env.development`:

```ini
NEXT_PUBLIC_API_URL="https://localhost:44302/"
NEXT_PUBLIC_ENVIRONMENT="LocalDev"
NEXT_PUBLIC_VERSION="0.0.1-next.dev"
```

This results in the following configuration in the browser:

```html
<head>
  <script id="windowAppConfig">
    window.appConfig = {
        "apiUrl": "https://localhost:44302/",
        "environment": "LocalDev",
        "version": "0.0.1-next.dev",
    }
  </Script>
</head>
```

#### Configuration for deployment

Deployment uses a two-step token replacement process, where the build step replaces the environment variables with tokens, and then those tokens are replaced during deployment (or at run time).

The first step is during build, which injects some build information (such as the build version), whilst tokens are injected for other values.

```powershell
$ENV:BUILD_VERSION = "0.0.1-next.build"
npm run build --prefix demo-web-app
```

The definition of which values are replaced by tokens is in `.env.production`. Note that despite the name the build can be used in multiple deployed environments (not just production); it is production-candidate build. The token format uses the indicators #{ ... }#, although a different scheme could be used (for some example alternatives, see <https://www.npmjs.com/package/@qetza/replacetokens>) 

```ini
NEXT_PUBLIC_API_URL="#{ClientConfig.ApiUrl}#"
NEXT_PUBLIC_ENVIRONMENT="#{ClientConfig.Environment}#"
NEXT_PUBLIC_VERSION="$BUILD_VERSION"
```

After injecting the tokens, the generated file in `out/index.html` (and other .html files) contain the injected tokens.

```html
<head>
  <script id="windowAppConfig">
    window.appConfig = {
        "apiUrl": "#{ClientConfig.ApiUrl}#",
        "environment": "#{ClientConfig.Environment}#",
        "version": "0.0.1-next.build",
    }
  </Script>
</head>
```

These tokens then need to be replaced at deployment time (or run time), with environment-specific values, e.g.

```powershell
Copy-Item -Recurse 'demo-web-app/out' 'Demo.WebApi/wwwroot'
$replace = @{ "ClientConfig.ApiUrl" = "/"; "ClientConfig.Environment" = "Test"; "ClientConfig.PathBase" = ""; "ClientConfig.TracePropagateCorsUrls" = "" }
Get-ChildItem "demo-web-app/out" -Recurse -Filter "*.js" | ForEach-Object { Get-Content $_.FullName -Raw | ForEach-Object { $line = $_; $replace.Keys | ForEach-Object { $line = $line -replace ("#{" + $_ + "}#"), $replace[$_] }; $line } | Set-Content ($_.FullName -replace 'demo-web-app\\out', 'Demo.WebApi\wwwroot') }
Get-ChildItem "demo-web-app/out" -Recurse -Filter "*.html" | ForEach-Object { Get-Content $_.FullName -Raw | ForEach-Object { $line = $_; $replace.Keys | ForEach-Object { $line = $line -replace ("#{" + $_ + "}#"), $replace[$_] }; $line } | Set-Content ($_.FullName -replace 'demo-web-app\\out', 'Demo.WebApi\wwwroot') }
```

This will generate the output file that contains the environment-specific values.

```html
<head>
  <script id="windowAppConfig">
    window.appConfig = {
        "apiUrl": "/",
        "environment": "Test",
        "version": "0.0.1-next.build",
    }
  </Script>
</head>
```

You can then run just the .NET Web server:

```powershell
dotnet run --project Demo.WebApi -- --urls "http://*:8002;https://*:44302" --environment Development
```

Which will also server the single page app at <https://localhost:44302>

#### Alternative client configuration approaches

**Runtime configuration:** Rather than replace tokens at deployment time, the `index.html` file can have token replacements done at runtime by the server. This allows configuration to be dynamically changed without having to redeploy.

**External script file:** Rather than embedding the configuration as an inline script, it can be contained in a referenced `client_config.js` script file. Referenced scripts are run in the order they are specified, so it still runs before the rest of the page. This can make it easier to do the replacement in a separate file, but it slightly more complex.

**Dynamically loaded configuration:** Rather than an embedded script, a server request can be made to load the configuration, e.g. as JSON. A drawback here is that you need to wait for the async request to complete (rather than having the config values available inline). You also have a reference problem if the server location needs configuration.

### OpenTelemetry web client utility component

React Next.js does include some OpenTelemetry support, but it is for the server component only. There is no configuration of the web client side.

Web client instrumentation libraries are available for OpenTelemetry, but are experimental.

The sample application includes a `tracing.ts` file to configure client side tracing and provide some support functions. By default it doesn't do any exporting (although the console exporter can be turned on). What it does do is generate the initial trace ID on the web client and use it in server requests.

This can be used to correlate across multiple service calls from one user operation, or to log (such as with a third party logging system) or display on the client.

You need to first ensure dependencies are installed:

```powershell
pushd demo-web-app
npm install @opentelemetry/sdk-trace-web @opentelemetry/context-zone @opentelemetry/instrumentation-fetch @opentelemetry/instrumentation-xml-http-request
popd
```

To initially configure the library, e.g. at the top level of `page.tsx` (this also uses the application configuration, above, to set some of the values).

```ts
import { configureOpenTelemetry } from "./tracing";
import { appConfig } from "./appConfig";

configureOpenTelemetry({
  enableConsoleExporter: true,
  enableFetchInstrumentation: true,
  enableXhrInstrumentation: false,
  propagateCorsUrls: appConfig.tracePropagateCorsUrls,
  serviceName: 'DemoApp',
  version: appConfig.version,
})  
```

To use the client side OpenTelemetry, you can use the `traceSpan()` utility function to encapsulate actions in a trace span. The current trace will then be automatically proparated via `fetch` (and XML HTTP Request) operations as configured. Note that the zone manager does not yet support `async`/`await`, so you need to use `Promise` continuation to ensure the current trace context is preserved.

There is also an optional utility method `getActiveSpanContext()` if you want to manually access the current trace ID (such as for the additional console logs below).

```ts
const clickFetchND10 = async () => {
  traceSpan('click_fetch_Nd10', async () => {
    const url = process.env.NEXT_PUBLIC_API_URL + 'api/dice/roll?dice=1D8'
    console.log('clickFetchND10 for N', url, getActiveSpanContext()?.traceId)
    fetch(url)
      .then(response => response.json())
      .then(json => {
        const url2 = process.env.NEXT_PUBLIC_API_URL + `api/dice/roll?dice=${json}D10`
        fetch(url2)
          .then(response => response.json())
          .then(json => {
            setFetchND10Result(json)    
          })
      })
  })
}
```

#### OpenTelemetry web client export via hosted collector

TODO: Container compose solution with app + reverse proxy (e.g. nginx) + OpenTelemetry collector, allowing the client to send OTLP (forwarded to the back end destinations). Going through the reverse proxy, CORS is not used, although it can be used to demonstrate forwarding headers.

## Server modules

### CORS module

Allows a basic CORS configuration to be applied from `appsettings.json`.

To enable in `Program.cs`:

```csharp
builder.ConfigureApplicationDefaultCors();
...
app.UseCors();
```

The configuration supports both arrays of strings/structured objects for JSON based configuration, a simple comma separated list of values for ease of setting from the command line, or a single string value. A value of "*" means to allow any, e.g. `AllowAnyHeader()`.

As environment variables (not the double underscores):

```sh
set Cors__AllowedOrigins="http://localhost:8003,https://localhost:44303"
set Cors__AllowedHeaders="traceparent,tracestate"
```

Example `appsettings.json`:

```json
{
  "Cors": {
    "AllowCredentials": true,
    "AllowedOrigins": [ "http://localhost:8003", "https://localhost:44303" ],
    "AllowedHeaders": "*",
    "ExposedHeaders": "Content-Disposition"
  }
}
```

### Client app runtime configuration module

TODO: Module to inject tokens to static file.

### Forwarded Headers module

Allows the Forwarded Headers configuration to be applied from `appsettings.json`. This is used for setting the real client IP address when behind a reverse proxy or content distribution network.

The immediate remote host (other end of the connection) will be the proxy or CDN, which will inject an appropriate header with the real client IP address.

We should not, however, just blindly accept the injected header, as it is a security risk -- we should use the built in .NET forwarded header handler to check that the actual sender is our known reverse proxy or CDN, and only then trust the corresponding injected header.

This is a built in component of ASP.NET, all we need to do is provide the appropriate configuration.

To enable in `Program.cs`:

```csharp
builder.ConfigureApplicationForwardedHeaders();
...
app.UseForwardedHeaders();
```

The configuration supports both arrays of strings/structured objects for JSON based configuration, and a simple comma separated list of values for ease of setting from the command line.

As environment variables (not the double underscores):

```pwsh
$ENV:ForwardedHeadersOptions__KnownProxies = "2001:DB8::1,203.0.113.1"
$ENV:ForwardedHeadersOptions__KnownNetworks = "fd00::/7,10.0.0.0/8"
```

Example `appsettings.json` as arrays:

```json
{
  "ForwardedHeadersOptions": {
    "ForwardedHeaders": "XForwardedFor,XForwardedProto",
    "KnownProxies": ["2001:DB8::1", "203.0.113.1"],
    "KnownNetworks": [
      {
        "Prefix": "fd00::",
        "PrefixLength": 7
      },
      {
        "Prefix": "10.0.0.0",
        "PrefixLength": 8
      }
    ]
  }
}
```

### OpenTelementy configuration module

Used to configure OpenTelemetry resource settings, options, and exporters from application setttings.

This is an alternative approach to that taken by the auto-instrumentation library, which relies heavily on environment settings only.

This can be enabled in `Program.cs`. The specific instrumentation needs to be configured per-application, e.g. most applications will probably use ASP.NET and HTTP Client, but the usage of database (PostgreSQL, SQL Server, etc), messaging, and other libraries, or custom actitvity sources will vary by application

```csharp
builder.ConfigureApplicationTelemetry(configureTracing: tracing =>
{
  // Add application-specific instrumentation
  tracing.AddAspNetCoreInstrumentation();
  tracing.AddHttpClientInstrumentation();
});
```

The configuation of options, exporters, and linking exports to each area is done in `appsettings.json`.

The default `Otlp` exporter is based on the standard OpenTelemetry environment variable settings. There is also a default `Console` exporter.

Additional exporters can be defined using `Otlp` protocol for alternative dimensions by prefixing the exporter name with `Otlp:`, to different destinations. In the example below one exporter is configured for Seq and another for Jaeger (both support OTLP).

Each OpenTelemetry area (logs, metrics, traces) can then be configured with multiple exporters, e.g. logging can be sent to both the default and to the Seq endpoint.

There are also flags for general OpenTelemetry `Debug` and for protocol level `OtlpDebug`.

```json
{
  "OpenTelemetry": {
    "Debug": false,
    "Exporters": {
      "Otlp:Seq": {
        "Endpoint": "http://127.0.0.1:5341/ingest/otlp/v1/logs",
        "Protocol": "HttpProtobuf"
      },
      "Otlp:Jaeger": {
        "Endpoint": "http://127.0.0.1:4317",
        "Protocol": "Grpc"
      }
    },
    "Logs": {
      "Exporters": [
        "Otlp",
        "Otlp:Seq"
      ]
    },
    "OtlpDebug": false,
    "Traces": {
      "Exporters": [
        "Otlp",
        "Otlp:Jaeger"
      ]
    }
  }
}
```

Configuration of the default `Otlp` exporter can be done via standard OpenTelemetry environment variables, e.g.

```sh
set OTEL_EXPORTER_OTLP_ENDPOINT="localhost:4317"
```

## Server configuration

### Logging options configuration

Allows configuration of several logging options from application settings, e.g. `appsettings.json`, environment variables, or command line.

This helper method will configure logger factory options, enrichment options, and the built in process and service log enrichers. A machine name enricher is also provided.

Note that while the `simple` console logger can be configured to output scopes, it does not output state (where enrichment values are). You need to use the `json` console logger, or a third party, to see the enrichment values, e.g. machine name is output to JSON path `$.State.host.name`. The JSON logger can also be configured to output trace ID as via scopes, at the JSON path `$.Scopes[0].TraceId`.

Enrichment values are included in OpenTelemetry logs, however there is some overlap. e.g. machine name is sent as both `resource.host.name` and log property `host.name`, so it appears twice in tools like Seq. Other values, like process ID and thread ID are not duplicated (they are only sent as state properties, if enrichment is enabled).

```csharp
builder.ConfigureApplicationLoggingOptions();
builder.Services.AddStaticLogEnricher<MachineNameLogEnricher>();
```

An example `appsettings.json` configuration file, that enables all logging types:

```json
{
  "Logging": {
    "Enrichment": {
      "CaptureStackTraces": true,
      "IncludeExceptionMessage": true,
      "UseFileInfoForStackTraces": true
    },
    "LoggerFactory": {
      "ActivityTrackingOptions": "TraceId, SpanId, ParentId, Baggage, Tags"
    },
    "ProcessLogEnricher": {
      "ProcessId": true,
      "ThreadId": true
    },
    "ServiceLogEnricher": {
      "ApplicationName": true,
      "BuildVersion": true,
      "DeploymentRing": false,
      "EnvironmenName": true
    }
  }
}
```

You can configure to use the JSON console formatter either via `appsettings.json` or using standard environment variables, along with Scopes and using the ISO Roundtrip ("o") date format. This is useful if you are running in an environment that will automatically parse the application output, e.g. AWS Elastic Container Service logging to CloudWatch will automatically detect JSON and convert to named properties.

```sh
set Logging__Console__FormatterName="json"
set Logging__Console__FormatterOptions__IncludeScopes="true"
set Logging__Console__FormatterOptions__TimestampFormat="o"                                                                                              | Format of timestamp in console logs ("o" = 
```

## Containerised solution for collecting client telemetry

### Building the containerised solution

A containerised version of the application can be build from the command line:

```powershell
podman build --build-arg INFORMATIONAL_VERSION=$(dotnet gitversion /output json /showvariable InformationalVersion) --tag demo/app:latest --file container/Containerfile-app .
```

#### Test running the app

The built app container can be tested in isolation:

```powershell
podman run --name demo_app --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development demo/app:latest
```

Access at <http://localhost:8080>

### Running with reverse proxy and collector

Running using the OpenTelemetry collector, redirecting to Seq & Jaeger.

```powershell
podman-compose -f container/compose-app.yml up -d
```

* Access web app at <http://localhost:8180>
* Jaeger, for tracing: <http://localhost:16686/>
* Seq, for logging <http://localhost:8341/>, admin / seqdev123

#### OpenTelemetry web client export via hosted collector

TODO: Container compose solution with app + reverse proxy (e.g. nginx) + OpenTelemetry collector, allowing the client to send OTLP (forwarded to the back end destinations). Going through the reverse proxy, CORS is not used, although it can be used to demonstrate forwarding headers.



## App creation

```powershell
mkdir example-2024
cd example-2024
dotnet new sln 
dotnet new webapi -o Demo.WebApi
dotnet sln add Demo.WebApi
npx create-next-app demo-web-app

dotnet new apicontroller -n WeatherController -o Demo.WebApi/Controllers -p:n Demo.WebApi.Controllers
```

## Ideas

- https://timdeschryver.dev/blog/maybe-its-time-to-rethink-our-project-structure-with-dot-net-6
