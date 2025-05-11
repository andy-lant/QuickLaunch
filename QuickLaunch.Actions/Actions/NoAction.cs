#nullable enable
using System;

// Use file-scoped namespace
namespace QuickLaunch.Core.Actions;


// Now implements IAction
public class NoAction : IAction
{
    public static ActionType ActionType { get; } = new ActionType(
        "NoAction", "No action.",
        typeof(NoAction), Array.Empty<ActionParameterInfo>()
    );

    public NoAction()
    {
    }

    public void Execute()
    {
    }
}

#nullable disable // Disable nullable context if it was enabled locally at the top
