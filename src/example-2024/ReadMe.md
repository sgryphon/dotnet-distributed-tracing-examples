# Example application (2024): dice roller

* .NET 8 back end
* React Next.js front end

## Run the app

This sample is a basic dice-rolling application.

TODO: First run the dependencies via a container framework, e.g.:

```powershell
podman-compose up -d
```

To run the back end:

```powershell
dotnet run --project Demo.WebApi -- --urls "http://*:8002;https://*:44302" --environment Development
```

Front end, in a separate console:

First you need to have Node.js available, e.g. if you are using a manager you may need to initialise it:

```powershell
fnm env | Out-String | Invoke-Expression
```

Then run the front end client:

```powershell
npm run dev --prefix demo-web-app -- --port 8003
```

Access the app at <http://localhost:8003>

## Client application

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

After injecting the tokens, the generated file in `.next/server/app/index.html` (and other .html files) contain the injected tokens.

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
mkdir 'demo-web-app/out'
$replace = @{ "ClientConfig.ApiUrl" = "/"; "ClientConfig.Environment" = "Test" }
Get-Content 'demo-web-app/.next/server/app/index.html' | ForEach-Object { $line = $_; $replace.Keys | ForEach-Object { $line = $line -replace ("#{" + $_ + "}#"), $replace[$_] }; $line } | Set-Content 'demo-web-app/out/index.html'
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

#### Alternative client configuration approaches

**Runtime configuration:** Rather than replace tokens at deployment time, the `index.html` file can have token replacements done at runtime by the server. This allows configuration to be dynamically changed without having to redeploy.

**External script file:** Rather than embedding the configuration as an inline script, it can be contained in a referenced `client_config.js` script file. Referenced scripts are run in the order they are specified, so it still runs before the rest of the page. This can make it easier to do the replacement in a separate file, but it slightly more complex.

**Dynamically loaded configuration:** Rather than an embedded script, a server request can be made to load the configuration, e.g. as JSON. A drawback here is that you need to wait for the async request to complete (rather than having the config values available inline). You also have a reference problem if the server location needs configuration.

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


## Server configuration

### Logging configuration



## Web client React modules

### OpenTelementy web client module



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
