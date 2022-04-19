using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Configure OpenTelemetry service resource details
var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
var entryAssemblyName = entryAssembly?.GetName();
var versionAttribute = entryAssembly?.GetCustomAttributes(false)
    .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
    .FirstOrDefault();
var attributes = new Dictionary<string, object>
{
    // See https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions
    ["host.name"] = Environment.MachineName,
    ["os.description"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
    ["deployment.environment"] = builder.Environment.EnvironmentName.ToLowerInvariant()
};
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(entryAssemblyName?.Name, serviceVersion: versionAttribute?.InformationalVersion ?? entryAssemblyName?.Version?.ToString())
    .AddTelemetrySdk()
    .AddAttributes(attributes);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
        .SetResourceBuilder(resourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddJaegerExporter();
});
builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews();

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
