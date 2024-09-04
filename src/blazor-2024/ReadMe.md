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
dotnet run --project Demo.BlazorApp -- --urls "http://*:8004;https://*:44304/"
```

Access the app at <https://localhost:44304/>

## App creation

From <https://dotnet.microsoft.com/en-us/learn/aspnet/blazor-cli-tutorial/intro>

```powershell
mkdir blazor-2024
cd blazor-2024
dotnet new sln 
dotnet new blazor -o Demo.BlazorApp
dotnet sln add Demo.BlazorApp

```

## Ideas

- https://timdeschryver.dev/blog/maybe-its-time-to-rethink-our-project-structure-with-dot-net-6
