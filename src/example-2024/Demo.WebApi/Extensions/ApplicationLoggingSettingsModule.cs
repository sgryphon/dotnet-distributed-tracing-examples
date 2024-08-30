namespace Demo.WebApi.Extensions;

public static class ApplicationLoggingSettingsModule
{
    private const string Section = "Logging";
    
    public static void ConfigureApplicationLoggingOptions(this IHostApplicationBuilder builder, string key = Section)
    {
        builder.Logging.Configure(options =>
        {
            builder.Configuration.Bind($"{key}:LoggerFactory", options);
        });

        // Enrichers are used for standard logger providers such as Console JSON formatter (for AWS)
        // In OTLP, this adds them as log properties (in additional to being resource attributes)
        builder.Logging.EnableEnrichment(builder.Configuration.GetSection($"{key}:Enrichment"));
        
        builder.Services.AddProcessLogEnricher(
            builder.Configuration.GetSection($"{key}:ProcessLogEnricher")
        );
        builder.Services.AddServiceLogEnricher(
            builder.Configuration.GetSection($"{key}:ServiceLogEnricher")
        );
    }
}
