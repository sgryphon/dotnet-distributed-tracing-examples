# .NET 8 distributed trace example

## Create web API with React from end

```powershell
mkdir dotnet8
cd dotnet8
dotnet new webapi -o Demo.WebApi
npx create-next-app demo-web-app
```

TODO: .NET Solution, other service

## Code changes

### Back end .NET app

Configure CORS in `Program.cs` to allow the front end to call (just hard code the URLs):

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://localhost:8003");
        });
});
```

Enable CORS and disable HTTPS redirect:

```csharp
app.UseCors();
//app.UseHttpsRedirection();
```

Create a logger:

```csharp
var logger = app.Services.GetService<ILogger<Program>>();
logger.LogInformation("Started");
```

Log each request:

```csharp
app.MapGet("/weatherforecast", () =>
{
    logger.LogInformation("Weather requested");
```

Enable scopes for the console logger in `appsettings.json` to output the Trace ID:

```json
{
  "Logging": {
    "Console": {
      "IncludeScopes": true
    }
```

### Front end client app

Configure `page.tsx` as a client component with state:

```typescript
'use client'
import { useState } from "react";
```

Add a function to fetch the server data and store in state (hard code the URL)?

```tsx
export default function Home() {
  const [data, setData] = useState(null)
  const fetchData = async () => {
    console.log("Fetching data")
    const res = await fetch('http://localhost:8002/weatherforecast')
    if (!res.ok) {
      throw new Error('Failed to fetch data')
    }
    const data = await res.json()
    setData(data)
  }
```

Add a button and output the data received:

```tsx
      <div className="m-5">
        <div className="m-5">
          <button className={'bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded'} onClick={fetchData}>Fetch Data</button>
        </div>
        <p><pre>{JSON.stringify(data, null, 2)}</pre></p>
      </div>
```

## Run the app

Back end:

```powershell
dotnet run --project Demo.WebApi -- --urls http://*:8002 --environment Development
```

Front end, in a separate console:

```powershell
npm run dev --prefix demo-web-app -- --port 8003
```

View the app at the client URL and click the button to fetch dat:: <http://localhost:8003>
