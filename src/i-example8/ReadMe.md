# .NET 8 sample application

## Run the app

Back end:

```powershell
dotnet run --project Demo.WebApi -- --urls "http://*:8002;https://*:44302" --environment Development
```

Front end, in a separate console:

```powershell
npm run dev --prefix demo-web-app -- --port 8003
```

## Server modules

### CORS module

Allows a basic CORS configuration to be applied from `appsettings.json`.

To enable in `Program.cs`:

```csharp
builder.ConfigureApplicationDefaultCors();
...
app.UseCors();
```

Example `appsettings.json`:

```json
{
  "Cors": {
    "AllowedOrigins": "http://localhost:8003",
    "AllowedHeaders": "*",
    "ExposedHeaders": "Content-Disposition"
  },
}
```

Note that configuration items can be a comma separated list; this allows them to be easily configured as environment variables, e.g.

```sh
set Cors__AllowedHeaders="traceparent,tracestate"
```

### Client Config module

Exposes a `Client` configuration section from `appsettings.json` as a JavaScript snippet, allowing it to be includes as run-time per-environment configuration by a web client application.

Web client application frameworks usually only support build-time configuration, so if you want to build once and then deploy multiple times you need a way to supply per-environment configuration. You can use placeholder substitution in files when deploying, although this still requires a re-deploy to change.

This module hooks into existing .NET server application configuration, which may be provided by your hosting environment, e.g. Azure can map web app settings, and makes themn available to the client.

To enable in `Program.cs`:

```csharp
builder.AddApplicationClientConfig();
...
app.UseApplicationClientConfig();
```

By default the settings is available at the endpoint `/client_config.js` and injects the variable `windows.config`. If you are using TypeScript, you may want to add a strongly typed wrapper around this value.

### Forwarded Headers module





### OpenTelementy configuration module


## Server configuration

### Logging configuration



## Web client React modules

### OpenTelementy web client module



## App creation

```powershell
mkdir i-example8
cd i-example8
dotnet new sln 
dotnet new webapi -o Demo.WebApi
dotnet sln add Demo.WebApi
npx create-next-app demo-web-app

dotnet new apicontroller -n WeatherController -o Demo.WebApi/Controllers -p:n Demo.WebApi.Controllers
```






## Ideas

- https://timdeschryver.dev/blog/maybe-its-time-to-rethink-our-project-structure-with-dot-net-6
