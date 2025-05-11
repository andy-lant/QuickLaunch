using System.Collections.Generic;
using System.Linq;

namespace QuickLaunch.Core.Actions;

public class ActionFactory
{
    #region ----- Static Properties. -----

    private static readonly Dictionary<string, ActionType> _actionRegistry = new[] {
        NoAction.ActionType,
        OpenFileAction.ActionType,
        OpenUrlAction.ActionType,
        RunProgram.ActionType,
    }.ToDictionary(actionType => actionType.Name);

    public static IReadOnlyDictionary<string, ActionType> ActionRegistry => _actionRegistry;

    #endregion

    #region ----- Public Static Methods. -----

    /// <summary>
    /// Register a new action type.
    /// </summary>
    /// <param name="actionType">ActionType to register</param>
    public static void RegisterAction(ActionType actionType)
    {
        _actionRegistry.Add(actionType.Name, actionType);
    }

    /// <summary>
    /// Find action type by name.
    /// </summary>
    /// <param name="name">action type name</param>
    /// <returns>ActionType</returns>
    public static ActionType? LookupActionType(string name)
    {
        // Find the action type by name
        return ActionRegistry[name];
    }

    /// <summary>
    /// Create a new dispatcher.
    /// </summary>
    /// <param name="name">dispatcher name</param>
    /// <returns>new dispatcher instance</returns>
    public static IActionDispatcher CreateDispatcher(string name)
    {
        // Create a new dispatcher instance
        return new ActionDispatcher(name);
    }

    #endregion
}


