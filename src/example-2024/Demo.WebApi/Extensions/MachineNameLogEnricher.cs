using Microsoft.Extensions.Diagnostics.Enrichment;
using OpenTelemetry.Resources;

namespace Demo.WebApi.Extensions;

public class MachineNameLogEnricher : IStaticLogEnricher
{
    const string AttributeHostName = "host.name";
    
    public void Enrich(IEnrichmentTagCollector collector)
    {
        collector.Add(AttributeHostName, Environment.MachineName);
    }
}
