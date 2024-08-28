using System.Net;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Demo.WebApi.Extensions;

public static class ForwardedHeadersModule
{
    public const string Section = "ForwardedHeadersOptions";
    
    public static void ConfigureApplicationForwardedHeaders(this IHostApplicationBuilder builder, string key = Section)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            builder.Configuration.GetSection(key).Bind(options);
            
            // TODO: Support KnownProxies / KnownNetworks as comma separated string,
            // for ease of setting from environment variables 
            // e.g. ForwardedHeadersOptions__KnownNetworks = "fd00::/7,10.0.0.0/8"
            
            foreach (
                var knownProxy in builder.Configuration
                    .GetSection($"{key}:KnownProxies")
                    .GetChildren()
            )
            {
                options.KnownProxies.Add(IPAddress.Parse(knownProxy.Value!));
            }

            foreach (
                var knownNetwork in builder.Configuration
                    .GetSection($"{key}:KnownNetworks")
                    .GetChildren()
            )
            {
                var ipAddress = IPAddress.Parse(knownNetwork.GetValue<string>("Prefix")!);
                var prefixLength = knownNetwork.GetValue<int>("PrefixLength");
                var ipNetwork = new IPNetwork(ipAddress, prefixLength);
                options.KnownNetworks.Add(ipNetwork);
            }
        });
    }

}
