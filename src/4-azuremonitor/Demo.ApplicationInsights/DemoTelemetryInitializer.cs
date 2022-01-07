using System.Linq;
using System.Reflection;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Demo.ApplicationInsights
{
    public class DemoTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string? _entryAssemblyName;
        private readonly string? _version;

        public DemoTelemetryInitializer()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var entryAssemblyName = entryAssembly?.GetName();
            _entryAssemblyName = entryAssemblyName?.Name;
            var versionAttribute = entryAssembly?.GetCustomAttributes(false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();
            _version = versionAttribute?.InformationalVersion ?? entryAssemblyName?.Version?.ToString();
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Component.Version = _version;
            telemetry.Context.Cloud.RoleName = _entryAssemblyName;
        }
    }
}
