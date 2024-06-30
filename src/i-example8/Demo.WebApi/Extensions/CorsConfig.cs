namespace Demo.WebApi.Extensions;

public class CorsConfig
{
    public const string Section = "Cors";
    // Use a single string, to facilitate configuration via environment variables
    // (even though JSON configuration supports an array of children)
    public string AllowedOrigins { get; init; } = string.Empty;
    public string AllowedHeaders { get; init; } = string.Empty;
    public string ExposedHeaders { get; init; } = string.Empty;
}
