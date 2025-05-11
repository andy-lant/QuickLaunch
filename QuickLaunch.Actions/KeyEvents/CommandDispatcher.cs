using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Actions;
using QuickLaunch.Core.Config;
using QuickLaunch.Core.Logging;

namespace QuickLaunch.Core.KeyEvents;

public class CommandDispatcher : IDisposable
{
    private readonly WpfSequenceKeyCommandParser _cmdParser = new();

    // Dictionary to hold dispatcher instances, keyed by their *name* from config
    private readonly Dictionary<string, IActionDispatcher> _dispatchersByName = new();

    /// <summary>
    /// Event triggered when the Escape key is pressed when no command parsing is active.
    /// </summary>
    public event EventHandler? EscapePressed;

    /// <summary>
    /// Event triggered when a new kew is pressed.
    /// </summary>
    public event EventHandler<SequenceEventArgs>? CommandSequenceUpdated;

    /// <summary>
    /// Event triggered when a command sequence is complete.
    /// </summary>
    public event EventHandler<SequenceCompleteEventArgs>? CommandSequenceCompleted;

    /// <summary>
    /// Event triggered when a dispatcher was invoked.
    /// </summary>
    public event EventHandler<DispatcherInvokedEventArgs>? CommandDispatcherInvoked;


    public CommandDispatcher()
    {
    }

    public void CancelSequence()
    {
        _cmdParser.ResetSequence();
    }

    private void resetCmdParserSubscriptions()
    {
        _cmdParser.EscapePressed -= OnEscapePressed;
        _cmdParser.SequenceComplete -= ExecuteCommandSequence;
        _cmdParser.SequenceProgress -= OnCommandSequenceUpdated;
    }

    private void installCmdParserSubscriptions()
    {
        _cmdParser.EscapePressed += OnEscapePressed;
        _cmdParser.SequenceComplete += ExecuteCommandSequence;
        _cmdParser.SequenceProgress += OnCommandSequenceUpdated;
    }

    public void KeyPressed(object? sender, KeyEventArgs e)
    {
        _cmdParser.ProcessKeyDown(e);
    }

    private void ExecuteCommandSequence(object? sender, SequenceCompleteEventArgs e)
    {
        OnCommandSequenceCompleted(sender, e);
        (uint? number, string? dispatcherName) = (e.NumericArg, e.Tag);
        if (_dispatchersByName.TryGetValue(dispatcherName ?? "", out IActionDispatcher? dispatcher))
        {
            try
            {
                dispatcher.Execute(number);
            }
            catch (Exception ex)
            {
                Log.Logger?.LogError(ex, $"Error invoking dispatcher {dispatcherName} with index {number}.");
            }
        }
        else
        {
            Log.Logger?.LogDebug($"Dispatcher '{dispatcherName}' not found for command execution.");
        }

    }

    private void OnEscapePressed(object? sender, EventArgs e)
    {
        EscapePressed?.Invoke(sender, e);
    }

    private void OnCommandSequenceCompleted(object? sender, SequenceCompleteEventArgs e)
    {
        CommandSequenceCompleted?.Invoke(sender, e);
    }

    private void OnCommandSequenceUpdated(object? sender, SequenceEventArgs e)
    {
        CommandSequenceUpdated?.Invoke(this, e);
    }

    private void OnCommandDispatcherInvoked(object? sender, DispatcherInvokedEventArgs e)
    {
        CommandDispatcherInvoked?.Invoke(sender, e);
    }

    /// <summary>
    /// Sets up the command dispatcher from a configuration.
    /// </summary>
    public bool SetupFromConfig(AppConfig config)
    {
        Log.Logger?.LogDebug("--- Starting set-up of command dispatcher ---");

        // Reset event subscriptions.
        resetCmdParserSubscriptions();

        // Clear previous state
        _cmdParser.ClearBindings();
        _dispatchersByName.Clear();

        // Load and configure sequence commands
        if (SetupDispatchers(config) && SetupCommands(config))
        {
            // Install event subscriptions
            installCmdParserSubscriptions();

            Log.Logger?.LogDebug($"Configuration processing complete.");
            foreach (var kvp in _dispatchersByName)
            {
                Log.Logger?.LogDebug($" - Dispatcher '{kvp.Key}': {kvp.Value.ActionCount} actions registered.");
            }
            return true;
        }
        else
        {
            Log.Logger?.LogDebug("Failed to set up command dispatcher.");
            return false;
        }
    }

    private bool SetupDispatchers(AppConfig config)
    {
        foreach (var dispatcher in config.Dispatchers ?? Enumerable.Empty<DispatcherDefinition>())
        {
            try
            {
                var name = dispatcher.Name;
                if (_dispatchersByName.ContainsKey(name))
                {
                    Log.Logger?.LogDebug($"Warning: Duplicate dispatcher name '{name}'. Previous definition is overwritten.");
                }
                IActionDispatcher newDispatcher = ActionFactory.CreateDispatcher(dispatcher.Name);
                newDispatcher.DispatcherInvoked += DispatcherInvoked;
                _dispatchersByName.Add(dispatcher.Name, newDispatcher);
                // Register actions with the dispatcher
                foreach (var entry in dispatcher.Actions)
                {
                    newDispatcher.AddAction(entry.Index, entry.Action);
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.LogDebug($"Error creating dispatcher '{dispatcher.Name}': {ex.Message}");
                return false;
            }
        }
        return true;
    }

    private bool SetupCommands(AppConfig config)
    {
        foreach (var command in config.CommandTriggers ?? Enumerable.Empty<CommandTrigger>())
        {
            try
            {
                var dispatcher = command.Dispatcher;

                _cmdParser.RegisterSequence(command.Sequence.GetBindingsList(), tag: dispatcher.Name);
                Log.Logger?.LogDebug($"Registered sequence '{command.Sequence}' to trigger dispatcher '{command.Dispatcher}'.");
            }
            catch (Exception ex)
            {
                Log.Logger?.LogDebug($"Error creating command trigger '{command.Sequence}' to dispatcher '{command.Dispatcher}': {ex.Message}");
                return false;
            }
        }
        return true;
    }

    private void DispatcherInvoked(object? sender, DispatcherInvokedEventArgs e)
    {
        OnCommandDispatcherInvoked(sender, e);
    }

    #region ----- IDisposable. -----
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var dispatcher in _dispatchersByName.Values)
                {
                    dispatcher.DispatcherInvoked -= DispatcherInvoked;
                }
            }

            _dispatchersByName.Clear();
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
