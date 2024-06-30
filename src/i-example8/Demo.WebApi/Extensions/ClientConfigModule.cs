using System.Text.Json;

namespace Demo.WebApi.Extensions;

public static class ClientConfigModule
{
    private const string ConfigSection = "ClientConfig";
    private const string EndpointPattern = "/client_config.js";
    private static Dictionary<string, string> config;
    private static string endpoint;
    
    public static IHostApplicationBuilder AddApplicationClientConfig(this IHostApplicationBuilder builder, string key = ConfigSection, string endpoint = EndpointPattern)
    {
        // TODO: Also allow override the variable name used (i.e. instead of window.config)
        ClientConfigModule.endpoint = endpoint;
        var clientConfig = builder.Configuration.GetSection(key);
        config = new Dictionary<string, string>();
        foreach (var child in clientConfig.GetChildren())
        {
            if (child.Value != null)
            {
                config.Add(child.Key, child.Value);
            }
        }
        
        return builder;
    }
    
    public static IEndpointRouteBuilder UseApplicationClientConfig(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(endpoint, async () =>
        {
            return $"window.config = {JsonSerializer.Serialize(config)};";
        })
        .WithName("GetClientConfig")
        .WithOpenApi();
        
        return endpoints;
    }    
}
