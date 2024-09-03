**Dotnet Distributed Tracing Examples**

Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.

(1) Basic example
=================

Front end is a little special, so lets just start with server to server calls. Distributed trace correlation is already built into the recent versions of dotnet.

**NOTE:** If you have trouble with HTTPS, or do not have certificates set up, then see the section at the end of this file for HTTPS Developer Certificates.


Requirements
------------

* Dotnet 6.0 LTS

Demonstration 1
---------------

This demonstration uses the complete application in this project directory. To build it yourself from scratch, see below.

The `appsettings.Development.json` files are configured without the scope setting.

Run the application (without scopes):

```bash
./start-demo1.sh
```

Browse to `https://localhost:44302`, and then Fetch Data to see messages.

Stop the demo (CTRL-C in each window).

Modify `appsettings.Development.json`, in both projects, by cutting and pasting in the `Console` section from `appsettings.Demo.json`

Run the apps again:

```bash
./start-demo1.sh
```

Refresh the data to see the `TraceId` being output.

### Reset the demo

Delete the `Console` section from the two files.

### Multiple windows

Rather than use `tmux`, you can just run the two parts in separate console windows:

```pwsh
dotnet run --project Demo.Service --urls "https://*:44301"
```

```pwsh
nvm use 18.20.4
pushd Demo.WebApp\ClientApp; npm install; popd

dotnet run --project Demo.WebApp --urls "https://*:44302"
```

#### Include Scope ID in log output

```pwsh
dotnet run --project Demo.Service --urls "https://*:44301" -- --Logging:Console:FormatterName=simple --Logging:Console:FormatterOptions:IncludeScopes=true
```

```pwsh
nvm use 18.20.4
pushd Demo.WebApp\ClientApp; npm install; popd

dotnet run --project Demo.WebApp --urls "https://*:44302" -- --Logging:Console:FormatterName=simple --Logging:Console:FormatterOptions:IncludeScopes=true
```



Details
=======

Basic application
-----------------

This is one of the simplest examples possible for distributed tracing, with two .NET components: a web application, which then calls a back end service. Both the web app and service output logging,
with correlated trace IDs.

![Diagram with three components: Browser, Demo.WebApp, Demo.Service](docs/generated/basic-demo.png)

### New back end service

Ensure you have a development certificate (see notes at end if you need to).

Create a directory for the project and a solution file:

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

### Changes - service

Add log statements in the service `WeatherForecastController.cs` (make it a warning so that it
stands out):

```csharp
  public IEnumerable<WeatherForecast> Get()
  {
    _logger.LogWarning(4002, "TRACING DEMO: Back end service weather forecast requested");
    ...
  }
```

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

In the Demo.WebApp project, at the end of the main return in `FetchData.js`, add a button to make it easy to call the server.

```javascript
  return (
    <div>
      ...
      <p><button className="btn btn-primary" onClick={() => this.populateWeatherData()}>Refresh</button></p>
    </div>
  );
```

### Changes - web app api

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

Then replace the `Get()` method with the following, to log a message and then call the back end service.

```csharp
  [HttpGet]
  public Task<string> Get(System.Threading.CancellationToken cancellationToken)
  {
      _logger.LogWarning(4001, "TRACING DEMO: WebApp API weather forecast request forwarded");
      return _httpClient.GetStringAsync("https://localhost:44301/WeatherForecast", cancellationToken);
  }
```


View distributed trace identifiers
----------------------------------

At this point, you have a standard .NET application, with two server components: a web app,
which then calls a back end service. They have standard .NET `ILogger<T>` logging, but
nothing extra.

If you run both components now, you won't see the distributed tracing, but in the latest
version of .NET it is happening behind the scenes, built into `HttpClient` and ASP.NET.

### Configure to show distributed traces

To see the distributed traces, add configuration settings to output scopes:

Add this section to `appSettings.Development.json`, in both projects:

```json
{
  "Logging": {
    "Console": {
      "FormatterName": "simple",
      "FormatterOptions": {
        "IncludeScopes": true
      }
    },
    ...
  }
}
```

### Run the two services

In separate terminals run the service:

```bash
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development
```

And web app + api:

```bash
dotnet run --project Demo.WebApp --urls "https://*:44302" --environment Development
```

And check the front end at `https://localhost:44302/fetch-data`

#### Using tmux

There is also a combined script that will use **tmux** to open a split window with both projects running:

```bash
./start-demo1.sh
```


Distributed tracing is built in
-------------------------------

Without any additional configuration, trace correlation is automatically passed between the services. In the logging output of the back end service you can see the same TraceId as the web app.

```text
info: Demo.Service.Controllers.WeatherForecastController[2002]
      => SpanId:79f874d8bb5c7745, TraceId:4cc0769223865d41924eb5337778be25, ParentId:cf6a9d1f30334642 => ConnectionId:0HMC18204SUS0 => RequestPath:/WeatherForecast RequestId:0HMC18204SUS0:00000002 => Demo.Service.Controllers.WeatherForecastController.Get (Demo.Service)
      Back end service weather forecast requested
```

Troubleshooting
---------------

"System limit for number of file watchers reached"

Default is 65536 (`sudo sysctl fs.inotify.max_user_watches`). Put an increased limit into the
system configuration and reload.

```
echo fs.inotify.max_user_watches=524288 | sudo tee -a /etc/sysctl.d/local.conf
sudo systemctl restart systemd-sysctl.service
```

"The remote certificate is invalid because of errors in the certificate chain: PartialChain"

For dotnet-to-dotnet communications you need to have an openssl version equal or higher than 1.1.1h (`openssl version`).

You may need to download, build and install. First configure dependencies (on Ubuntu 20.04):

```shell
sudo apt install build-essential checkinstall zlib1g-dev -y
```

Download from https://www.openssl.org/source/ and extract, then check the latest INSTALL.md. It will instruct you to do something similar to the following to configure, make (build), test, and install:

```shell
./Configure '-Wl,-rpath,$(LIBRPATH)'
make
make test
sudo make install
```

This installed into `/usr/local/lib64` (but could be different depending on the system). Add this path to the loader.

```
cat <<EOF | sudo tee /etc/ld.so.conf.d/openssl.conf
/usr/local/lib64
EOF
sudo ldconfig -v
```

HTTPS Developer Certificates
----------------------------

### Windows and macOS

See: https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio#trust-the-aspnet-core-https-development-certificate-on-windows-and-macos

The certificate is automatically installed. To trust the certificate:

```shell
dotnet dev-certs https --trust
```

### Ubuntu

See: https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio#ubuntu-trust-the-certificate-for-service-to-service-communication

Create the HTTPS developer certificate for the current user personal certificate store (if not already initialised). 

```shell
dotnet dev-certs https
```

You can check the certificate exists for the current user; the file name is the SHA1 thumbprint. (If you want to clear out previous certificates use `dotnet dev-certs https --clean`, which will delete the file.)

```shell
ls ~/.dotnet/corefx/cryptography/x509stores/my
```

#### Trust the certificate for server communication

You need to have OpenSSL installed (check with `openssl version`).

Install the certificate. You need to use the `-E` flag with `sudo` when exporting the file, so that it exports the file for the current user (otherwise it will export the file for root, which will be different).

```shell
sudo -E dotnet dev-certs https -ep /usr/local/share/ca-certificates/aspnet/https.crt --format PEM
sudo update-ca-certificates
```

You can check the file exists, and then use open SSL to verify it has the same SHA1 thumbprint.

```shell
ls /usr/local/share/ca-certificates/aspnet
openssl x509 -noout -fingerprint -sha1 -inform pem -in /usr/local/share/ca-certificates/aspnet/https.crt
```

If the thumbprints do not match, you may have install the root (sudo user) certificate. You can check it at `sudo ls -la /root/.dotnet/corefx/cryptography/x509stores/my`.

#### Trust in Chrome

```shell
sudo apt-get install -y libnss3-tools
certutil -d sql:$HOME/.pki/nssdb -A -t "P,," -n localhost -i /usr/local/share/ca-certificates/aspnet/https.crt
certutil -d sql:$HOME/.pki/nssdb -A -t "C,," -n localhost -i /usr/local/share/ca-certificates/aspnet/https.crt
```

#### Trust in Firefox:

```shell
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
