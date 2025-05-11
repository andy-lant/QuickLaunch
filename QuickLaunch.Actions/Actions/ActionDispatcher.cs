using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Config;
using QuickLaunch.Core.Logging;

namespace QuickLaunch.Core.Actions;

/// <summary>
/// An action dispatcher.
/// </summary>
internal class ActionDispatcher : IActionDispatcher
{
    public event EventHandler<DispatcherInvokedEventArgs>? DispatcherInvoked = null;

    public string Name { get; private set; }

    private readonly Dictionary<uint, IAction> _actions = new();

    public int ActionCount => _actions.Count;

    public ActionDispatcher(string name)
    {
        Name = name;
    }

    public void AddAction(uint index, IAction action)
    {
        _actions.Add(index, action);
    }

    public void AddAction(uint index, ActionRegistration action)
    {
        // Create an instance of the action type
        Type type = action.ActionType.AssociatedType;
        if (type is null)
        {
            throw new NullReferenceException($"Action type {action.ActionType.Name} has no associated implementation type.");
        }
        else
        {
            if (Activator.CreateInstance(type, action.Parameters.Select(p => p.Value).ToArray()) is not IAction actionInstance)
            {
                throw new InvalidOperationException($"Failed to create action instance of type {type.FullName}.");
            }
            AddAction(index, actionInstance);
        }
    }

    public void ClearActions()
    {
        _actions.Clear();
    }

    public void Execute(uint? index)
    {
        var realIndex = index ?? 1; // Default to 1 if null
        if (_actions.TryGetValue(realIndex, out IAction? action))
        {
            Log.Logger?.LogInformation($"{Name} executing action {realIndex} => {action.GetType().Name}");
            try
            {
                action.Execute();
                OnDispatcherInvoked(realIndex, action, true);
            }
            catch (Exception ex)
            {
                Log.Logger?.LogError(ex, $"Error executing action for dispatcher {Name} for index {realIndex}");
                OnDispatcherInvoked(realIndex, action, true, ex);
            }
        }
        else
        {
            OnDispatcherInvoked(index, null, false);
            throw new KeyNotFoundException($"No action found for index {realIndex}.");
        }
    }

    public bool RemoveAction(uint index)
    {
        return _actions.Remove(index);
    }

    private void OnDispatcherInvoked(uint? index, IAction? action, bool actionExecuted, Exception? ex = null)
    {
        DispatcherInvoked?.Invoke(this, new DispatcherInvokedEventArgs(index, action, actionExecuted, ex));
    }
}
