using Microsoft.Extensions.Logging;

namespace QuickLaunch.Core.Logging;

public static class Log
{
    public static ILogger? Logger { get; set; } = null;

}
