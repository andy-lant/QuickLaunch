using System;
using QuickLaunch.Application;
using QuickLaunch.Core.Actions;

// Use file-scoped namespace
namespace QuickLaunch.Actions;


// Now implements IAction
internal class SettingsAction : IAction
{
    #region ----- Static Properties. -----

    public static ActionType ActionType { get; } = new ActionType(
        "Settings", "Open settings.",
        typeof(SettingsAction), Array.Empty<ActionParameterInfo>()
    );

    #endregion

    #region ----- Static Constructor. -----

    static SettingsAction()
    {
        ActionFactory.RegisterAction(ActionType);
    }

    #endregion

    #region ----- Constructor. -----

    public SettingsAction()
    {
    }

    #endregion

    #region ----- IAction. -----

    public void Execute()
    {
        AppService.Instance.OpenSettings();
    }

    #endregion
}
