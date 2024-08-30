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
            
            // Support as either an array of strings, for JSON settings
            // e.g. "KnownProxies": ["2001:DB8::1", "203.0.113.1"]
            // Or a comma separated string, for easy environment variable / argument support 
            // e.g. ForwardedHeadersOptions__KnownProxies = "2001:DB8::1,203.0.113.1"

            var knownProxiesSection = builder.Configuration.GetSection($"{key}:KnownProxies");
            IEnumerable<IPAddress> knownProxies;
            if (knownProxiesSection.Value != null)
            {
                knownProxies = knownProxiesSection.Value.Split(',').Select(x => IPAddress.Parse(x.Trim()));
            }
            else
            {
                knownProxies = knownProxiesSection.GetChildren().Select(x => IPAddress.Parse(x.Value!));
            }
            foreach (var ipAddress in knownProxies)
            {
                options.KnownProxies.Add(ipAddress);
            }
            
            var knownNetworksSection = builder.Configuration.GetSection($"{key}:KnownNetworks");
            IEnumerable<IPNetwork> knownNetworks;
            if (knownNetworksSection.Value != null)
            {
                knownNetworks = knownNetworksSection.Value.Split(',').Select(x => IPNetwork.Parse(x.Trim()));
            }
            else
            {
                knownNetworks = knownNetworksSection.GetChildren().Select(knownNetwork =>
                {
                    var ipAddress = IPAddress.Parse(knownNetwork.GetValue<string>("Prefix")!);
                    var prefixLength = knownNetwork.GetValue<int>("PrefixLength");
                    return new IPNetwork(ipAddress, prefixLength);
                });
            }
            foreach (var ipNetwork in knownNetworks)
            {
                options.KnownNetworks.Add(ipNetwork);
            }
        });
    }

}
