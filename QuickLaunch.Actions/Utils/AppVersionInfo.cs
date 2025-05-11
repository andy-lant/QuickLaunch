using System.Reflection;

namespace QuickLaunch.Core.Utils;
public static class AppVersionInfo
{
    public static string GetAssemblyVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "N/A";
    }

    public static string GetAssemblyFileVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.FileVersion ?? "N/A";
        }
        return "N/A";
    }

    public static string GetInformationalVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "N/A";
        }
        return "N/A";
    }
}

