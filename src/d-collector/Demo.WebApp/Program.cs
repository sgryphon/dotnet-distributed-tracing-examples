using MassTransit;
using MassTransit.Logging;
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
// builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
// {
//     tracerProviderBuilder
//         .SetResourceBuilder(resourceBuilder)
//         .AddAspNetCoreInstrumentation()
//         .AddHttpClientInstrumentation()
//         .AddSource("MassTransit")
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
// builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
// {
//     tracerProviderBuilder
//         .SetResourceBuilder(resourceBuilder)
//         .AddAspNetCoreInstrumentation()
//         .AddHttpClientInstrumentation()
//         .AddSource("MassTransit")
//         .AddOtlpExporter(otlpExporterOptions =>
//         {
//             builder.Configuration.GetSection("OpenTelemetry:OtlpExporter").Bind(otlpExporterOptions);
//         });
// });

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();

builder.Services.AddMassTransit(mtConfig => {
    mtConfig.UsingRabbitMq((context, rabbitConfig) => {
        rabbitConfig.Host(builder.Configuration.GetValue<string>("MassTransit:RabbitMq:Host"),
            builder.Configuration.GetValue<ushort>("MassTransit:RabbitMq:Port"),
            builder.Configuration.GetValue<string>("MassTransit:RabbitMq:VirtualHost"),
            hostConfig => {
                hostConfig.Username(builder.Configuration.GetValue<string>("MassTransit:RabbitMq:Username"));
                hostConfig.Password(builder.Configuration.GetValue<string>("MassTransit:RabbitMq:Password"));
            }
        );
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");;

app.Run();
