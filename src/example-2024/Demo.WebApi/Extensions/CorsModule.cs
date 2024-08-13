using Microsoft.Net.Http.Headers;

namespace Demo.WebApi.Extensions;

public static class CorsModule
{
    public static void ConfigureApplicationDefaultCors(this IHostApplicationBuilder builder, string key = CorsConfig.Section)
    {
        builder.Services.AddCors(options =>
        {
            var corsConfig =
                builder.Configuration.GetSection(key).Get<CorsConfig>() ?? new CorsConfig();
            // TODO: Dynamically support both an array of strings (e.g. from JSON) and a single string with commas (from environment variable)
            var allowedOrigins = corsConfig.AllowedOrigins.Split(',');
            var allowedHeaders = corsConfig.AllowedHeaders.Split(',');
            var exposedHeaders = corsConfig.ExposedHeaders.Split(',');
            options.AddDefaultPolicy(builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins(allowedOrigins)
                        .WithExposedHeaders(exposedHeaders);
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
}
