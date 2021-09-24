# Dotnet Distributed Tracing Examples
Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.


## Requirements

* Dotnet 5.0
* Powershell, for running scripts

## Basic example

Front end is a little special, so lets just start with server to server calls.

**NOTE:** If you have trouble with HTTPS, or do not have certificats set up, then see the section at
the end of this file for HTTPS Developer Certificates.

### New back end service

Create a development certificate (if you are using a different shell, replace the PowerShell variable):

Create a directory for the project and a solution file

```sh
mkdir 1-basic
cd 1-basic
dotnet new sln
dotnet new webapi --output Demo.Service
dotnet sln add Demo.Service
```

Check it works:

```sh
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development
```

Test it in a browser at `https://localhost:44301/WeatherForecast`

### New web + api server

In another terminal:

```sh
cd 1-basic
dotnet new react --output Demo.WebApp
dotnet sln add Demo.WebApp
```

Check it works:

```sh
dotnet run --project Demo.WebApp --urls "https://*:44302" --environment Development
```

Test it in a browser at `https://localhost:44302` 

### Changes - web app front end

In the Demo.WebApp project, at the end of the main return in `FetchData.js`, add a text input field to send to the server (along with state to store the value), and a button to make it easy to call.

```javascript
  constructor(props) {
    super(props);
    this.state = { forecasts: [], loading: true, value: '' };
  }

  handleChangeValue = (e) => {
    this.setState({ value: e.target.value });
  }

  ...

  return (
    <div>
      ...
        <p><label>Input: <input type="text" value={this.state.value} onChange={this.handleChangeValue} /></label></p>
        <p><button className="btn btn-primary" onClick={() => this.populateWeatherData()}>Refresh</button></p>
    </div>
  );

  async populateWeatherData() {
    const response = await fetch('weatherforecast?' + new URLSearchParams({value: this.state.value, ts: Math.round(Date.now()/1000)}));
    ...
  }
```

### Changes - web app api

Rather than return the data directly, have the web app API forward the call to the service, and add some logging statements:

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Demo.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> GetAsync(string value, int ts,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(2001, "Weather forecast requested with value {Value} at timestamp {TimeStamp}",
                value, ts);
            var response = await _httpClient.GetAsync("https://localhost:44301/WeatherForecast", cancellationToken)
                .ConfigureAwait(false);
            var data = await response.Content
                .ReadFromJsonAsync<List<WeatherForecast>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken).ConfigureAwait(false);
            _logger.LogDebug(6001, "Weather forecast returning {Count} items", data?.Count);
            return data;
        }
    }
}
```

You also need to register the injected `HttpClient` in `StartUp.cs`. Use the built in factory registration to ensure the correct lifecyle is applied to the `HttpClient`.

```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            ...
            services.AddHttpClient();
        }
```

In `appSettings.Development.json`, configure logging to include Debug level and output scopes (which will output the trace correlation):

```json
{
  "Logging": {
    "Console": {
      "FormatterName": "systemd",
      "FormatterOptions": {
        "IncludeScopes": true,
        "SingleLine": true,
        "TimestampFormat": "HH:mm:ss "
      }
    },    
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

### Changes - service

Add log statements in the service `WeatherForecastController.cs`:

```csharp
  public IEnumerable<WeatherForecast> Get()
  {
      _logger.LogInformation(2100, "Service weather forecast requested");
  ...
  }
```

And configure logging in `appSettings.Development.json`:

```json
{
  "Logging": {
    "Console": {
      "FormatterName": "simple",
      "FormatterOptions": {
        "IncludeScopes": true,
        "SingleLine": true,
        "TimestampFormat": "HH:mm:ss "
      }
    },    
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

### Run the two services

In separate terminals run the service:

```sh
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development
```

And web app + api:

```sh
dotnet run --project Demo.WebApp --urls "https://*:44302" --environment Development
```

And check the front end at `https://localhost:44302/fetch-data`

### Distributed tracing is built in

Without any additional configuration, trace correlation is automatically passed between the services. In the logging output of the service you can see the same TraceId as the web app.

```
16:12:15 info: Demo.Service.Controllers.WeatherForecastController[2100] => SpanId:60373f4e6d002746, TraceId:c4c277866ee9e24486b01ce40a28a054, ParentId:d73ea5eec804ae43 => ConnectionId:0HMBV49007J8A => RequestPath:/WeatherForecast RequestId:0HMBV49007J8A:00000003 => Demo.Service.Controllers.WeatherForecastController.Get (Demo.Service) Service weather forecast requested
16:12:22 info: Demo.Service.Controllers.WeatherForecastController[2100] => SpanId:0ce868abf44dcf44, TraceId:09b9add09b018e45acf3893ef7e04cef, ParentId:d99cd1687283ff4d => ConnectionId:0HMBV49007J8A => RequestPath:/WeatherForecast RequestId:0HMBV49007J8A:00000004 => Demo.Service.Controllers.WeatherForecastController.Get (Demo.Service) Service weather forecast requested
```


## HTTPS Developer Certificates

### Windows and macOS

See: https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio#trust-the-aspnet-core-https-development-certificate-on-windows-and-macos

The certificate is automatically installed. To trust the certificate:

```
dotnet dev-certs https --trust
```

### Ubuntu

See: https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio#ubuntu-trust-the-certificate-for-service-to-service-communication

Create the HTTPS developer certificate for the current user personal certificate store (if not already initialised). 

```
dotnet dev-certs https
```

You can check the certificate exists for the current user; the file name is the SHA1 thumbprint. (If you want to clear out previous certificates use `dotnet dev-certs https --clean`, which will delete the file.)

```
ls ~/.dotnet/corefx/cryptography/x509stores/my
```

#### Trust the certificate for server communication

You need to have OpenSSL installed (check with `openssl version`).

Install the certificate. You need to use the `-E` flag with `sudo` when exporting the file, so that it exports the file for the current user (otherwise it will export the file for root, which will be different).

```
sudo -E dotnet dev-certs https -ep /usr/local/share/ca-certificates/aspnet/https.crt --format PEM
sudo update-ca-certificates
```

You can check the file exists, and then use open SSL to verify it has the same SHA1 thumbprint.

```
ls /usr/local/share/ca-certificates/aspnet
openssl x509 -noout -fingerprint -sha1 -inform pem -in /usr/local/share/ca-certificates/aspnet/https.crt
```

If the thumbprints do not match, you may have install the root (sudo user) certificate. You can check it at `sudo ls -la /root/.dotnet/corefx/cryptography/x509stores/my`.

#### Trust in Chrome

```
sudo apt-get install -y libnss3-tools
certutil -d sql:$HOME/.pki/nssdb -A -t "P,," -n localhost -i /usr/local/share/ca-certificates/aspnet/https.crt
certutil -d sql:$HOME/.pki/nssdb -A -t "C,," -n localhost -i /usr/local/share/ca-certificates/aspnet/https.crt
```

#### Trust in Firefox:

```
cat <<EOF | sudo tee /usr/lib/firefox/distribution/policies.json
{
    "policies": {
        "Certificates": {
            "Install": [
                "/usr/local/share/ca-certificates/aspnet/https.crt"
            ]
        }
    }
}
EOF
```
