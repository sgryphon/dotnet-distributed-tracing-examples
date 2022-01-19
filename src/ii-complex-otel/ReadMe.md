# Dotnet Distributed Tracing Examples
Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.

## i) Open Telemetry

Example using Open Telemetry libraries.

### Requirements

* Dotnet 5.0
* Docker (with docker-compose), for local services
* Azure subscription, for cloud services
* Azure CLI, to create cloud resources
* Powershell, for running scripts

### Set up Azure resources

This example includes Service Bus messages, which you will need to create. There is a PowerShell script that will create the required resources, and output the required connection string.

```powershell
az login
$VerbosePreference = 'Continue'
./deploy-infrastructure.ps1
```

You can log in to the Azure portal to check your queue was created at `https://portal.azure.com`


### Add configuration

### Add libraries

### Enable OpenTelemetry


### Run all three applications

Instead of updating the `appsettings.json` file, you can also put the connection string into a PowerShell variable, and then pass it to the projects from the command line, or set via environment variables.

Console worker:

```powershell
./set-environment.ps1
dotnet run --project Demo.Worker
```

Back end service:

```powershell
./set-environment.ps1
dotnet run --project Demo.Service --urls "https://*:44301"
```

And web app + api:

```powershell
./set-environment.ps1
dotnet run --project Demo.WebApp --urls "https://*:44302"
```

Generate some activity from the front end at `https://localhost:44302/fetch-data`, and then
check the results in Azure Monitor.

