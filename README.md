# Dotnet Distributed Tracing Examples
Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.


## Requirements

* Dotnet 5.0
* Powershell, for running scripts


## Basic example

Front end is a little special, so lets just start with server to server calls:

### Trust development certificates

See: https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio#ssl-linux

```
sudo apt-get install libnss3-tools
sudo dotnet dev-certs https -ep /usr/local/share/ca-certificates/aspnet/https.crt --format PEM
sudo update-ca-certificates
certutil -d sql:$HOME/.pki/nssdb -A -t "P,," -n localhost -i /usr/local/share/ca-certificates/aspnet/https.crt
certutil -d sql:$HOME/.pki/nssdb -A -t "C,," -n localhost -i /usr/local/share/ca-certificates/aspnet/https.crt
```

### New back end service

Create a development certificate (if you are using a different shell, replace the PowerShell variable):

```pwsh
dotnet dev-certs https -ep ~/.aspnet/https/Demo.Service.pfx -p Password01
dotnet dev-certs https --trust
```

Create a directory for the project and a solution file

```pwsh
mkdir 1-basic
cd 1-basic
dotnet new sln
dotnet new webapi --output Demo.Service
dotnet user-secrets -p Demo.Service/Demo.Service.csproj init
dotnet user-secrets -p Demo.Service/Demo.Service.csproj set "Kestrel:Certificates:Development:Password" "Password01"
dotnet sln add Demo.Service
```

Check it works:

```
dotnet run --project Demo.Service --urls "https://[::]:44301" --environment Development --Logging:LogLevel:Default Debug
```

Test it in a browser at `https://[::1]:44301/WeatherForecast`

### New web + api server

In another terminal:

```
dotnet dev-certs https -ep ~/.aspnet/https/Demo.WebApp.pfx -p Password01
dotnet dev-certs https --trust
```

```
cd 1-basic
dotnet new react --output Demo.WebApp
dotnet user-secrets -p Demo.WebApp/Demo.WebApp.csproj init
dotnet user-secrets -p Demo.WebApp/Demo.WebApp.csproj set "Kestrel:Certificates:Development:Password" "Password02"
dotnet sln add Demo.WebApp
```

Check it works:

```
dotnet run --project Demo.WebApp --urls "https://[::]:44302" --environment Development --Logging:LogLevel:Default Debug
```

Test it in a browser at `https://localhost:44302` (websockets does not like `wss://[::1]:44302`) 



* Add/modify button on web to send text box value; server to call back end service (injected HttpClient)
  - mention special magic about HttpClient; use the pre-configured IoC version
* Add logging (console, with context)
* It just works


