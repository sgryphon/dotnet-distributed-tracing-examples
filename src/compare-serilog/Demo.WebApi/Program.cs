using Serilog;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var logConfig = builder.Configuration.GetSection($"Log")?.Value;

if (string.Equals(logConfig, "serilog-seq", StringComparison.OrdinalIgnoreCase))
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.Seq("http://localhost:5341")
        .CreateLogger();
    Log.Information("Serilog Seq configured");
    builder.Services.AddSerilog();
}

if (string.Equals(logConfig, "serilog-otlp", StringComparison.OrdinalIgnoreCase))
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(options => {
            options.Endpoint = "http://localhost:5341/ingest/otlp/v1/logs";
            options.Protocol = OtlpProtocol.HttpProtobuf;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "weather-demo"
            };
        })
        .CreateLogger();
    Log.Information("Serilog OTLP configured");
    builder.Services.AddSerilog();
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
