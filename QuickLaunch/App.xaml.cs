using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Logging;
using QuickLaunch.Core.Utils;

namespace QuickLaunch;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    #region Application mutex
    private static readonly string _mutexName = $"Global\\{{{GetAssemblyGuid()}}}";
    private static readonly bool _mutexCreated;

    private static readonly Mutex _mutex = new Mutex(true, _mutexName, out _mutexCreated);

    private static string GetAssemblyGuid()
    {
        var assembly = Assembly.GetEntryAssembly()?.GetCustomAttributes(typeof(GuidAttribute), false);
        if (assembly is null || assembly.Length == 0)
            return "bdf1fc8d-5923-4b85-88c2-8b32c27c58c6";
        else
        {
            return ((GuidAttribute)assembly[0]).Value;
        }
    }

    #endregion

    private ILogger? _logger;

    private void App_Startup(object sender, StartupEventArgs e)
    {

#if DEBUG
        Console.OpenConsole();
#endif

        // -- Setup logging.
        using ILoggerFactory loggerFactory = LoggerFactory.Create(static builder =>
            {
                builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                        options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                    });

#if DEBUG
                builder.AddDebug();
#endif

                builder.SetMinimumLevel(
#if DEBUG
                    LogLevel.Trace
#else
                    LogLevel.Information
#endif
                );
            }
        );

        _logger = loggerFactory.CreateLogger("QL");
        Log.Logger = _logger;

        _logger.LogDebug("Debugging started.");
        _logger.LogInformation($"Started QuickLaunch {AppVersionInfo.GetInformationalVersion()}.");

        if (!_mutexCreated)
        {
            MessageBox.Show("An instance of this application is already running.");
            this.Shutdown();
            return;
        }
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {

        if (_mutexCreated)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
        _logger?.LogDebug($"Exit: {e}");
#if DEBUG
        Console.CloseConsole();
#endif
    }
}
