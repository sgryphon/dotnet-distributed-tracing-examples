# .NET 8 distributed trace example

## Create web API with React from end

```powershell
mkdir dotnet8
cd dotnet8
dotnet new webapi -o Demo.WebApi
npx create-next-app demo-web-app
```

TODO: .NET Solution, other service

## Run apps

Back end:

```powershell
dotnet run --project Demo.WebApi
```

Front end, in a separate console:

```powershell
npm run dev --prefix demo-web-app
```
