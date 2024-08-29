using Demo.WebApi;
using Demo.WebApi.Extensions;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.ConfigureApplicationDefaultCors();
builder.ConfigureApplicationForwardedHeaders();
builder.ConfigureApplicationTelemetry(configureTracing: tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
});
builder.ConfigureApplicationLoggingOptions();
builder.Services.AddStaticLogEnricher<MachineNameLogEnricher>();

// Used for enrichment of loggers that don't support OTLP
builder.Services.AddApplicationMetadata(metadata =>
{
    metadata.ApplicationName = ServiceInformation.ServiceName;
    metadata.BuildVersion = ServiceInformation.Version;
    metadata.EnvironmentName = builder.Environment.EnvironmentName.ToLowerInvariant();
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseForwardedHeaders();
app.UseCors();
app.MapControllers();

app.Run();
