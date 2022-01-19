# Dotnet Distributed Tracing Examples

Example of distributed tracing in .NET, using W3C Trace Context and OpenTelemetry.

## b) Jaeger

An OpenTelemetry example, exporting to Jaeger.

### Requirements

* Dotnet 6.0
* Docker (with docker-compose), for local services

### Run local Jaeger service

You need to run the Jaeger service to send distributed tracing information to. For example on Linux a docker compose configuration is provided. For more details see https://www.jaegertracing.io/

To run the Jaeger service:

```sh
docker-compose -p demo up -d
```

To check the Jaeger console, browse to `http://localhost:16686`


### Configure Jaeger exporter



### Run the services

In separate terminals run the service:

```powershell
dotnet run --project Demo.Service --urls "https://*:44301" --environment Development
```

To run the web app front end you need to configure the web API address it will use via an environment variable:

```powershell
$ENV:ASPNETCORE_URLS = "http://localhost:8002"
npm run start --prefix Demo.WebApp/ClientApp
```

Then run the web api in a third terminal:

```powershell
dotnet run --project Demo.WebApp --urls "https://*:8002" --environment Development
```

Check the front end at `https://localhost:44303/fetch-data` and see the OpenTelemetry tracing details logged to the console.

![](images/opentelemetry-basic.png)

Activity traces are recorded when they complete, so you will see the inner Activity in the back end service listed first, and see that it's Activity.ParentId is the HttpClient Activity in the web API, and see the last it's parent (also in the web API) is the web API request Activity.

Also shown are the LogRecord entries, from both system logs and the application log in the back end service (the other logs are off the screen, as the console exporter is quite verbose). The SpanIds for the LogRecords match the Activity traces they are related to.

You can also see the service details with the name, version, and instance ID.


