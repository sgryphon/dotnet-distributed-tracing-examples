using Demo.Service;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// DEMO - COMMON

// // Configure OpenTelemetry service resource details
// // See https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions
// var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
// var entryAssemblyName = entryAssembly?.GetName();
// var versionAttribute = entryAssembly?.GetCustomAttributes(false)
//     .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
//     .FirstOrDefault();
// var serviceName = entryAssemblyName?.Name;
// var serviceVersion = versionAttribute?.InformationalVersion ?? entryAssemblyName?.Version?.ToString();
// var attributes = new Dictionary<string, object>
// {
//     ["host.name"] = Environment.MachineName,
//     ["os.description"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
//     ["deployment.environment"] = builder.Environment.EnvironmentName.ToLowerInvariant()
// };
// var resourceBuilder = ResourceBuilder.CreateDefault()
//     .AddService(serviceName, serviceVersion: serviceVersion)
//     .AddTelemetrySdk()
//     .AddAttributes(attributes);

// DEMO - 2

// Configure tracing
// builder.Services.AddOpenTelemetryTracing(configure =>
// {
//     configure
//         .SetResourceBuilder(resourceBuilder)
//         .AddAspNetCoreInstrumentation()
//         .AddEntityFrameworkCoreInstrumentation()
//         .AddNpgsql()
//         .AddJaegerExporter();
// });

// DEMO - 3

// // Configure logging
// builder.Logging
//     .AddOpenTelemetry(configure =>
//     {
//         configure
//             .SetResourceBuilder(resourceBuilder)
//             .AddOtlpExporter(otlpExporterOptions =>
//             {
//                 builder.Configuration.GetSection("OpenTelemetry:OtlpExporter").Bind(otlpExporterOptions);
//             });
//         configure.IncludeFormattedMessage = true;
//         configure.IncludeScopes = true;
//         configure.ParseStateValues = true;
//     });

// // Configure tracing
// builder.Services.AddOpenTelemetryTracing(configure =>
// {
//     configure
//         .SetResourceBuilder(resourceBuilder)
//         .AddAspNetCoreInstrumentation()
//         .AddNpgsql()
//         .AddOtlpExporter(otlpExporterOptions =>
//         {
//             builder.Configuration.GetSection("OpenTelemetry:OtlpExporter").Bind(otlpExporterOptions);
//         });
// });

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add database
builder.Services.AddDbContext<WeatherContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WeatherContext")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
