using System.Diagnostics;
using System.Reflection;

namespace Demo.WebApi;

public static class ServiceInformation
{
    static ServiceInformation()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly!.GetName();
        var versionAttribute = assembly
            .GetCustomAttributes(false)
            .OfType<AssemblyInformationalVersionAttribute>()
            .FirstOrDefault();
        InstanceId = Guid.NewGuid().ToString();
        ServiceName = assemblyName.Name!;
        ActivitySource = new ActivitySource(ServiceName);
        Version =
            versionAttribute?.InformationalVersion
            ?? assemblyName.Version?.ToString()
            ?? string.Empty;
    }

    public static ActivitySource ActivitySource { get; }

    public static string InstanceId { get; }

    public static string ServiceName { get; }

    public static string Version { get; }
}
