#nullable enable
using System;
using QuickLaunch.Core.Config;

// Use file-scoped namespace
namespace QuickLaunch.Core.Actions;

/// <summary>
/// Defines a common interface for action dispatchers.
/// </summary>
public interface IActionDispatcher // Removed generic type parameter
{

    event EventHandler<DispatcherInvokedEventArgs> DispatcherInvoked;

    /// <summary>
    /// Adds or updates an action associated with a specific index.
    /// </summary>
    /// <param name="index">The integer index.</param>
    /// <param name="action">The action (implementing IAction) to add/update.</param> // Changed TAction back to IAction
    /// <exception cref="ArgumentNullException">Thrown if action is null.</exception>
    /// <exception cref="ArgumentException">Thrown if index is invalid or action type is incompatible (implementation specific).</exception>
    void AddAction(uint index, IAction action);

    /// <summary>
    /// Adds or updates an action associated with a specific index, from its ActionRegistration data.
    /// </summary>
    /// <param name="index">The integer index.</param>
    /// <param name="action">The action registration data to add/update</param>
    void AddAction(uint index, ActionRegistration action);

    /// <summary>
    /// Executes the action associated with the specified index.
    /// Uses a default index (e.g., 1) if the provided index is null.
    /// </summary>
    /// <param name="index">The index of the action to execute. Defaults if null.</param>
    /// <exception cref="KeyNotFoundException">Thrown if the index is not found.</exception>
    void Execute(uint? index);

    /// <summary>
    /// Gets the number of actions currently registered.
    /// </summary>
    int ActionCount { get; }

    /// <summary>
    /// Clears all actions from the dispatcher.
    /// </summary>
    void ClearActions();

    /// <summary>
    /// Remove action by index.
    /// </summary>
    bool RemoveAction(uint index);
}

/// <summary>
/// Arguments for DispatcherInvokedEvent.
/// </summary>
public class DispatcherInvokedEventArgs : EventArgs
{
    public uint? Index { get; }

    public IAction? Action { get; }

    public bool ActionWasExecuted { get; }

    public Exception? Exception { get; }

    internal DispatcherInvokedEventArgs(uint? index, IAction? action, bool actionExecuted, Exception? ex)
    {
        Index = index;
        Action = action;
        ActionWasExecuted = actionExecuted;
        Exception = ex;
    }
}

#nullable disable // Disable nullable context if it was enabled locally at the top
