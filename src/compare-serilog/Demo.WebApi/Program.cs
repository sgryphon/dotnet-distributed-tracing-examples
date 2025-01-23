using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using SerilogTracing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var logConfig = builder.Configuration.GetSection($"Log")?.Value;
var traceConfig = builder.Configuration.GetSection($"Trace")?.Value;

if (string.Equals(logConfig, "serilog-seq", StringComparison.OrdinalIgnoreCase))
{
    Log.Logger = new LoggerConfiguration()
        .Enrich.WithProperty("Application", "weather-demo-serilog-seq")
        .WriteTo.Console()
        .WriteTo.Seq("http://localhost:5341")
        .CreateLogger();
    Log.Information("Serilog Seq configured");
    builder.Services.AddSerilog();
}

if (string.Equals(logConfig, "serilog-otlpseq", StringComparison.OrdinalIgnoreCase))
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(options => {
            options.Endpoint = "http://localhost:5341/ingest/otlp/v1/logs";
            options.Protocol = OtlpProtocol.HttpProtobuf;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "weather-demo-serilog-otlpseq"
            };
        })
        .CreateLogger();
    Log.Information("Serilog OTLP configured");
    builder.Services.AddSerilog();
}

IDisposable? activityListenerHandle = null;
if (string.Equals(traceConfig, "serilog", StringComparison.OrdinalIgnoreCase))
{
    // Destination of the traces uses the corresponding log definition (above)
    activityListenerHandle  = new ActivityListenerConfiguration()
        .Instrument.AspNetCoreRequests()
        .Instrument.SqlClientCommands()
        .TraceToSharedLogger();
    Log.Information("Serilog tracing configured");
}

var otel = builder.Services.AddOpenTelemetry();
otel.ConfigureResource(resource => resource.AddService(serviceName: "weather-demo-otel"));

if (string.Equals(logConfig, "otel-otlpseq", StringComparison.OrdinalIgnoreCase))
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.AddOtlpExporter(opt => {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri("http://localhost:5341/ingest/otlp/v1/logs");
        });
    });
}

if (string.Equals(traceConfig, "otel-otlpseq", StringComparison.OrdinalIgnoreCase))
{
    otel.WithTracing(tracing =>
    {
        tracing.AddSource("Weather.Source");
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddOtlpExporter(opt => {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri("http://localhost:5341/ingest/otlp/v1/traces");
        });
    });
}

if (string.Equals(logConfig, "otel-otlp", StringComparison.OrdinalIgnoreCase))
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.AddOtlpExporter();
    });
}

if (string.Equals(traceConfig, "otel-otlp", StringComparison.OrdinalIgnoreCase))
{
    otel.WithTracing(tracing =>
    {
        tracing.AddSource("Weather.Source");
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddOtlpExporter();
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var activitySource = new ActivitySource("Weather.Source");
    using var activity = activitySource.StartActivity("Weather Trace {UnixTimeSeconds}");
    activity?.SetTag("UnixTimeSeconds", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(1001, "Weather Requested {WeatherGuid}", Guid.NewGuid());

    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
