# Comparison of OpenTelemetry vs Serilog

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

First run the dependencies via a container framework:

* Seq, for logging <http://localhost:8341/>, admin / seqdev123
* TODO: Aspire

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

Access the app at <http://localhost:44302/weatherforecast>, then view the results in Seq.

## App creation

```powershell
mkdir compare-serilog
cd compare-serilog
dotnet new sln 
dotnet new webapi -o Demo.WebApi
dotnet sln add Demo.WebApi
```
