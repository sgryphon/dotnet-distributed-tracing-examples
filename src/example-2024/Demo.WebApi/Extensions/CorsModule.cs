namespace Demo.WebApi.Extensions;

public static class CorsModule
{
    public const string Section = "Cors";

    public static void ConfigureApplicationDefaultCors(this IHostApplicationBuilder builder, string key = Section, string policyName = "")
    {
        builder.Services.AddCors(options =>
        {
            // Support as either an array of strings, for JSON settings
            // e.g. "AllowedOrigins": [ "http://localhost:8003", "https://localhost:44303" ]
            // Or a comma separated string, for easy environment variable / argument support 
            // e.g. Cors__AllowedOrigins = "http://localhost:8003,https://localhost:44303"

            var allowedOrigins = GetStringArray(builder, $"{key}:AllowedOrigins");
            var allowedHeaders = GetStringArray(builder, $"{key}:AllowedHeaders");
            var exposedHeaders = GetStringArray(builder, $"{key}:ExposedHeaders");

            var allowCredentials = builder.Configuration.GetSection(key).GetValue<bool?>("AllowCredentials");

            if (string.IsNullOrEmpty(policyName))
            {
                policyName = options.DefaultPolicyName;
            }

            options.AddPolicy(policyName, builder =>
                {
                    builder
                        .AllowAnyMethod()
                        .WithExposedHeaders(exposedHeaders);

                    if (allowCredentials == true)
                    {
                        builder.AllowCredentials();
                    }
                    
                    if (allowedOrigins.Contains("*"))
                    {
                        builder.AllowAnyOrigin();
                    }
                    else
                    {
                        builder.WithOrigins(allowedOrigins);
                    }
                    
                    if (allowedHeaders.Contains("*"))
                    {
                        builder.AllowAnyHeader();
                    }
                    else
                    {
                        builder.WithHeaders(allowedHeaders);
                    }
                }
            );
        });
    }

    private static string[] GetStringArray(IHostApplicationBuilder builder, string key)
    {
        var section = builder.Configuration.GetSection(key);
        string[] values;
        if (section.Value != null)
        {
            values = section.Value.Split(',').Select(x => x.Trim()).ToArray();
        }
        else
        {
            values = section.GetChildren().Select(x =>x.Value!).ToArray();
        }

        return values;
    }
}
