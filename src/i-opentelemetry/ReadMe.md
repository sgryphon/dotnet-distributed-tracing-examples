# Dotnet Distributed Tracing Examples

Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.

## i) OpenTelemetry

This is a basic example using OpenTelemetry.

**NOTE:** If you have trouble with HTTPS, or do not have certificates set up, then see the section at
the end of this file for HTTPS Developer Certificates.

### Requirements

* Dotnet 5.0
* Docker (with docker-compose), for local services

### Basic app creation

This is the same as for the basic example.

#### New back end service

Create a development certificate (if you are using a different shell, replace the PowerShell variable):

Create a directory for the project and a solution file

```sh
mkdir i-opentelemetry
cd i-opentelemetry
dotnet new sln
dotnet new webapi --output Demo.Service
dotnet sln add Demo.Service
```

Check it works:

```sh
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development
```

Test it in a browser at `https://localhost:44301/WeatherForecast`

#### New web + api server

In another terminal:

```sh
cd i-opentelemetry
dotnet new react --output Demo.WebApp
dotnet sln add Demo.WebApp
```

Check it works:

```sh
dotnet run --project Demo.WebApp --urls "https://*:44302" --environment Development
```

Test it in a browser at `https://localhost:44302` 

#### Changes - web app front end

In the Demo.WebApp project, at the end of the main return in `FetchData.js`, add a button to make it easy to call the server.

```javascript
  return (
    <div>
      ...
      <p><button className="btn btn-primary" onClick={() => this.populateWeatherData()}>Refresh</button></p>
    </div>
  );
```

#### Changes - web app api

Rather than return the data directly, have the web app API log a message and forward the call to the service.

Note that `HttpClient` should never be used directly, but via the built in factory to ensure the correct lifecycle is applied. Register the system factory in `Startup.cs`:

```csharp
  public void ConfigureServices(IServiceCollection services)
  {
      ...
      services.AddHttpClient();
  }
```

Modify `WeatherForecastController.cs` in the web app to inject `HttpClient`:


```csharp
  private readonly System.Net.Http.HttpClient _httpClient;
  private readonly ILogger<WeatherForecastController> _logger;

  public WeatherForecastController(ILogger<WeatherForecastController> logger, 
      System.Net.Http.HttpClient httpClient)
  {
      _logger = logger;
      _httpClient = httpClient;
  }
```

Then replace the `Get()` method with the following:

```csharp
  [HttpGet]
  public Task<string> Get(System.Threading.CancellationToken cancellationToken)
  {
      _logger.LogInformation(2001, "TRACING DEMO: WebApp API weather forecast request forwarded");
      return _httpClient.GetStringAsync("https://localhost:44301/WeatherForecast", cancellationToken);
  }
```

### Changes - service

Add log statements in the service `WeatherForecastController.cs`:

```csharp
  public IEnumerable<WeatherForecast> Get()
  {
    _logger.LogInformation(2002, "TRACING DEMO: Back end service weather forecast requested");
    ...
  }
```

### Add libraries

### Enable OpenTelemetry

### Add configuration

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

Without any additional configuration, trace correlation is automatically passed between the services. In the logging output of the back end service you can see the same TraceId as the web app.

```
info: Demo.Service.Controllers.WeatherForecastController[2002]
      => SpanId:79f874d8bb5c7745, TraceId:4cc0769223865d41924eb5337778be25, ParentId:cf6a9d1f30334642 => ConnectionId:0HMC18204SUS0 => RequestPath:/WeatherForecast RequestId:0HMC18204SUS0:00000002 => Demo.Service.Controllers.WeatherForecastController.Get (Demo.Service)
      Back end service weather forecast requested
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
