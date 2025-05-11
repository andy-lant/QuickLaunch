using System.Windows;
using Microsoft.Extensions.Logging;
using QuickLaunch.Actions;
using QuickLaunch.Core.Config;
using QuickLaunch.Core.KeyEvents;
using QuickLaunch.Core.Logging;
using QuickLaunch.Core.Utils;
using QuickLaunch.UI;

namespace QuickLaunch.Application;

public class AppService
{
    #region ----- Static Properties. -----

    private static AppService _instance = new();

    public static AppService Instance => _instance;

    #endregion

    #region ----- Properties. -----

    /// <summary>
    /// Application's MainWindow.
    /// </summary>
    internal MainWindowVM? MainWindowVM { get; set; }

    /// <summary>
    /// Command dispatcher.
    /// </summary>
    public CommandDispatcher Dispatcher { get; private set; }

    #endregion

    #region ----- Constructors. -----
    private AppService()
    {
        Dispatcher = new();
        // Force execution of static constructors, which should register the actions.
        object[] _ = new[] {
            SettingsAction.ActionType,
            ReloadConfigurationAction.ActionType
        };
    }
    #endregion

    #region ----- Public Methods. -----
    public void OpenSettings()
    {
        MainWindowVM?.MainWindow.Map((window) =>
        {
            var config = new ConfigurationLoader().LoadConfig();
            var editorWindow = new ConfigEditorWindow(config) { Owner = window };
            bool? result = editorWindow.ShowDialog();
            if (result == true)
            {

                Log.Logger?.LogDebug("Config editor saved. Reloading configuration...");
                if (Dispatcher.SetupFromConfig(config))
                {
                    Log.Logger?.LogDebug("Configuration reloaded successfully.");
                }
                else
                {
                    MessageBox.Show(window, "Errors occurred during configuration reload. Check debug output.", "Reload Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                Log.Logger?.LogDebug("Config editor cancelled or closed without saving.");
            }
        });
    }

    internal void ReloadConfiguration()
    {
        Log.Logger?.LogInformation("(Re)loading command dispatcher configuration.");
        var config = new ConfigurationLoader().LoadConfig();
        Dispatcher.SetupFromConfig(config);
    }

    #endregion
}
