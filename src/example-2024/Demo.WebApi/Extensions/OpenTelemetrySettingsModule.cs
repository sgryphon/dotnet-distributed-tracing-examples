using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Demo.WebApi.Extensions;

public static class OpenTelemetrySettingsModule
{
    private const string Section = "OpenTelemetry";
    const string AttributeHostName = "host.name";
    const string AttributeOsDescription = "os.description";
    const string AttributeDeploymentEnvironment = "deployment.environment";
    
    public static void ConfigureApplicationTelemetry(this IHostApplicationBuilder builder, string key = Section, Action<TracerProviderBuilder>? configureTracing = default)
    {
        var openTelemetry = builder
            .Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    ServiceInformation.ServiceName,
                    serviceVersion: ServiceInformation.Version,
                    serviceInstanceId: ServiceInformation.InstanceId
                );
                var attributes = new Dictionary<string, object>
                {
                    [AttributeHostName] = Environment.MachineName,
                    [AttributeOsDescription] =
                        RuntimeInformation.OSDescription,
                    [AttributeDeploymentEnvironment] =
                        builder.Environment.EnvironmentName.ToLowerInvariant()
                };
                resource.AddAttributes(attributes);
                resource.AddEnvironmentVariableDetector();
            });

        var traceExporters = builder
            .Configuration.GetSection($"{key}:Traces:Exporters")
            .Get<string[]>();
        if (traceExporters?.Length > 0)
        {
            openTelemetry.WithTracing(tracing =>
            {
                // Use this to add the auto instrumentation specific to this applciation
                configureTracing?.Invoke(tracing);

                // Default OTLP exporter configuration is GRPC "localhost:4317"
                // or direct from environment variables e.g. OTEL_EXPORTER_OTLP_ENDPOINT.
                // Values set here override the OTEL environment variables
                // (but could be set by their own environment variables, e.g. OpenTelemetry__Traces__OtlpExporter__Endpoint).
                // See: https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol

                foreach (var exporterTag in traceExporters)
                {
                    var (exporterType, exporterName) = ParseExporterTag(exporterTag);
                    switch (exporterType)
                    {
                        case "console":
                            builder.Services.Configure<ConsoleExporterOptions>(
                                exporterName,
                                builder.Configuration.GetSection(
                                    $"{key}:Exporters:{exporterTag}"
                                )
                            );
                            tracing.AddConsoleExporter(exporterName, configure: null);
                            break;
                        case "otlp":
                            tracing.AddOtlpExporter(
                                exporterName,
                                options =>
                                {
                                    if (exporterName == Options.DefaultName)
                                    {
                                        // Add default OtlpExporter, without binding .NET config.
                                        // This allows the default exporter to be configured via environment
                                        // variables. In particular we want to avoid using Bind on the
                                        // Endpoint as even if the value doesn't change it triggers an
                                        // internal "set programmatically" flag which prevents adding
                                        // the signal path suffixes (e.g. v1/traces).
                                        // However, do manually bind the Headers property, if it exists,
                                        // so we can pass in using user-secrets in a Development environment
                                        // (rather than as an environment variable)
                                        var headersSection = builder.Configuration.GetSection(
                                            $"{key}:Exporters:{exporterTag}:Headers"
                                        );
                                        if (headersSection.Exists())
                                        {
                                            options.Headers = headersSection.Value;
                                        }
                                    }
                                    else
                                    {
                                        // Named options use Bind, which means you need one per signal,
                                        // with the correct Endpoint (as the signal paths will not be added
                                        // automatically when set from configuration)
                                        builder.Configuration.Bind(
                                            $"{key}:Exporters:{exporterTag}",
                                            options
                                        );
                                    }
                                }
                            );
                            break;
                    }
                }
            });
        }

        var loggingExporters = builder
            .Configuration.GetSection($"{key}:Logs:Exporters")
            .Get<string[]>();
        if (loggingExporters?.Length > 0)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                // TODO: take these from configuration options (maybe bind?)
                logging.IncludeScopes = true;
                logging.IncludeFormattedMessage = true;
                logging.ParseStateValues = true;

                foreach (var exporterTag in loggingExporters)
                {
                    var (exporterType, exporterName) = ParseExporterTag(exporterTag);
                    switch (exporterType)
                    {
                        case "console":
                            logging.AddConsoleExporter(options =>
                                builder.Configuration.Bind(
                                    $"{key}:Exporters:{exporterTag}",
                                    options
                                )
                            );
                            break;
                        case "otlp":
                            logging.AddOtlpExporter(options =>
                            {
                                if (exporterName == Options.DefaultName)
                                {
                                    var headersSection = builder.Configuration.GetSection(
                                        $"{key}:Exporters:{exporterTag}:Headers"
                                    );
                                    if (headersSection.Exists())
                                    {
                                        options.Headers = headersSection.Value;
                                    }
                                }
                                else
                                {
                                    builder.Configuration.Bind(
                                        $"{key}:Exporters:{exporterTag}",
                                        options
                                    );
                                }
                            });
                            break;
                    }
                }
            });
        }

        // If necessary, OpenTelemetry / OTLP debugging can be enabled
        if (
            builder.Configuration.GetValue<bool>($"{key}:Debug")
            || builder.Configuration.GetValue<bool>($"{key}:OtlpDebug")
        )
        {
            builder.Services.AddHostedService<OpenTelemetryDebugService>();
        }
    }

    private static (string exporterType, string exporterName) ParseExporterTag(string exporterTag)
    {
        var exporterDetails = exporterTag.Split(':', 2);
        var exporterType = exporterDetails[0].ToLower();
        var exporterName = exporterDetails.Length > 1 ? exporterDetails[1] : Options.DefaultName;
        return (exporterType, exporterName);
    }
}
