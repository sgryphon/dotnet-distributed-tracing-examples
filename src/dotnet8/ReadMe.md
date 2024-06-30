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
"use client";
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
    <button
      className={
        "bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded"
      }
      onClick={fetchData}
    >
      Fetch Data
    </button>
  </div>
  <pre>{JSON.stringify(data, null, 2)}</pre>
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

## Adding OpenTelemetry tracing

In the back end you need to allow the `traceparent` header the the cross-origin policy. Note that cross-origin isn't needed if you bundle both the front and back end into the same endpoint.

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://localhost:8003")
                .WithHeaders("traceparent");
        });
});
```

In the front end reference the needed packages:

```powershell
npm add @opentelemetry/api @opentelemetry/context-zone @opentelemetry/instrumentation-fetch @opentelemetry/instrumentation-xml-http-request --prefix demo-web-app
```

Create a `tracing.tsx` file to handle the trace setup and span creation. Values such as the cross-origin setup are hard coded:

```tsx
import { Resource } from "@opentelemetry/resources";
import { SEMRESATTRS_SERVICE_NAME } from "@opentelemetry/semantic-conventions";
import { WebTracerProvider } from "@opentelemetry/sdk-trace-web";
import { ZoneContextManager } from "@opentelemetry/context-zone";
import { FetchInstrumentation } from "@opentelemetry/instrumentation-fetch";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
// import { ConsoleSpanExporter } from '@opentelemetry/sdk-trace-web';
// import { SimpleSpanProcessor } from '@opentelemetry/sdk-trace-base';

const provider = new WebTracerProvider({
  resource: new Resource({
    [SEMRESATTRS_SERVICE_NAME]: "demo-web-app",
  }),
});
//provider.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()));
provider.register({
  contextManager: new ZoneContextManager(),
});

const fetchInstrumentation = new FetchInstrumentation({
  propagateTraceHeaderCorsUrls: [/localhost:8002/i],
});
registerInstrumentations({
  instrumentations: [fetchInstrumentation],
  tracerProvider: provider,
});

export async function traceSpan(name: string, fn: () => Promise<void>) {
  const tracer = provider.getTracer("client-tracer");
  await tracer.startActiveSpan(name, async (span) => {
    try {
      await fn();
    } finally {
      span.end();
    }
  });
}

export function TraceProvider({ children }) {
  return <>{children}</>;
}
```

Import the reference in `page.tsx`:

```tsx
import { TraceProvider, traceSpan } from "./tracing";
import { trace } from "@opentelemetry/api";
```

Wrap the button handler in a trace span (or auto-instrument user behaviour):

```tsx
const fetchData = async () => {
  console.log("Fetching data");
  await traceSpan("fetch-data", async () => {
    const trace_id = trace.getActiveSpan()?.spanContext().traceId;
    console.log("Active span traceId:", trace_id);

    const res = await fetch("http://localhost:8002/weatherforecast");
    if (!res.ok) {
      throw new Error("Failed to fetch data");
    }
    const data = await res.json();
    setData(data);
  });
};
```

Wrap the page in the `TraceContext`:

```tsx
  return (
    <TraceProvider>
      <main className="flex min-h-screen flex-col items-center justify-between p-24">
        <div className="z-10 w-full max-w-5xl items-center justify-between font-mono text-sm lg:flex">
```

References:

- <https://opentelemetry.io/docs/demo/services/frontend/>
- <https://developers.redhat.com/articles/2023/03/22/how-enable-opentelemetry-traces-react-applications>
- <https://signoz.io/blog/opentelemetry-react/>
- <https://github.com/open-telemetry/opentelemetry-js/issues/3558>
