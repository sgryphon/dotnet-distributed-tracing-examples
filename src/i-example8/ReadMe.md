# .NET 8 sample application




## App creation

```powershell
mkdir i-example8
cd i-example8
dotnet new sln 
dotnet new webapi -o Demo.Service
dotnet new webapi -o Demo.WebApi
dotnet sln add Demo.Service
dotnet sln add Demo.WebApi
npx create-next-app demo-web-app

dotnet new apicontroller -n WeatherDataController -o Demo.Service/Controllers -p:n Demo.Service.Controllers -ac true
dotnet new apicontroller -n WeatherController -o Demo.WebApi/Controllers -p:n Demo.WebApi.Controllers
```
