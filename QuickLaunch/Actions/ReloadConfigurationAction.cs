using System;
using QuickLaunch.Application;
using QuickLaunch.Core.Actions;

// Use file-scoped namespace
namespace QuickLaunch.Actions;


// Now implements IAction
internal class ReloadConfigurationAction : IAction
{
    #region ----- Static Properties. -----

    public static ActionType ActionType { get; } = new ActionType(
        "ReloadConfiguration", "Reload Configuration.",
        typeof(ReloadConfigurationAction), Array.Empty<ActionParameterInfo>()
    );

    #endregion

    #region ----- Static Constructor. -----

    static ReloadConfigurationAction()
    {
        ActionFactory.RegisterAction(ActionType);
    }

    #endregion

    #region ----- Constructor. -----

    public ReloadConfigurationAction()
    {
    }

    #endregion

    #region ----- IAction. -----

    public void Execute()
    {
        AppService.Instance.ReloadConfiguration();
    }

    #endregion
}
