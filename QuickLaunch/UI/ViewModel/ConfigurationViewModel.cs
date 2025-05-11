using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Config;
using QuickLaunch.Core.Logging;

namespace QuickLaunch.UI.ViewModel;

public partial class ConfigurationViewModel : ObservableObject, IDisposable
{

    #region ----- Properties. -----

    internal AppConfig Config { get; }


    #region --- Observable Properties. ---

    /// <summary>
    /// Gets the list of current validation errors directly from the AppConfig.
    /// </summary>
    public IReadOnlyList<string> ConfigValidationErrors => Config.ValidationErrors;

    /// <summary>
    /// Gets a value indicating whether the AppConfig is currently valid.
    /// </summary>
    public bool IsConfigValid => Config.IsValid;

    [ObservableProperty]
    private CommandListViewModel _commandListViewModel;

    [ObservableProperty]
    private DispatcherListViewModel _dispatcherListViewModel;

    [ObservableProperty]
    private ActionTypesViewModel _actionTypesViewModel;

    #endregion

    #endregion

    #region ----- Constructors. -----

    internal ConfigurationViewModel(AppConfig config)
    {
        Config = config;
        // Initialize the ViewModels
        _commandListViewModel = new CommandListViewModel(config);
        _dispatcherListViewModel = new DispatcherListViewModel(config);
        _actionTypesViewModel = new ActionTypesViewModel();

        // Subscribe to PropertyChanged events from child ViewModels for SYNCHRONIZATION only
        CommandListViewModel.PropertyChanged += CommandListViewModel_PropertyChanged; // Handles sync + subscribes to SelectedCommand
        DispatcherListViewModel.PropertyChanged += DispatcherListViewModel_PropertyChanged; // Handles sync

        // *** Subscribe directly to AppConfig property changes for validation state ***
        Config.PropertyChanged += Config_PropertyChanged;

        // Initial sync if needed
        SyncDispatcherSelectionFromCommand();
        // Initial state notification for validation properties (in case config is already invalid)
        OnPropertyChanged(nameof(IsConfigValid));
        OnPropertyChanged(nameof(ConfigValidationErrors));
    }
    #endregion

    #region ----- Event Handlers. -----

    /// <summary>
    /// Handler for property changes in CommandListViewModel. Primarily for synchronization
    /// and managing subscriptions to the selected command.
    /// </summary>
    private void CommandListViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CommandListViewModel.SelectedCommand))
        {
            Log.Logger?.LogDebug($"ConfigVM: SelectedCommand changed to {CommandListViewModel.SelectedCommand?.Name ?? "null"}");
            // Unsubscribe from the previous command
            if (CommandListViewModel.PreviousSelectedCommand != null)
            {
                CommandListViewModel.PreviousSelectedCommand.PropertyChanged -= SelectedCommand_PropertyChanged;
            }
            // Subscribe to the new command
            if (CommandListViewModel.SelectedCommand != null)
            {
                CommandListViewModel.SelectedCommand.PropertyChanged += SelectedCommand_PropertyChanged;
            }
            // Update dispatcher selection
            SyncDispatcherSelectionFromCommand();
        }
        // Note: No call to UpdateValidationErrors needed here. AppConfig should revalidate internally.
    }

    /// <summary>
    /// Handler for property changes in DispatcherListViewModel. Primarily for synchronization.
    /// </summary>
    private void DispatcherListViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DispatcherListViewModel.SelectedDispatcher))
        {
            Log.Logger?.LogDebug($"ConfigVM: DispatcherListViewModel.SelectedDispatcher changed.");
            // Update the command's dispatcher
            SyncCommandDispatcherFromSelection();
        }
        // Note: No call to UpdateValidationErrors needed here. AppConfig should revalidate internally.
    }

    /// <summary>
    /// Handler for when a property *on the currently selected command* changes.
    /// </summary>
    private void SelectedCommand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If the Dispatcher property *of the selected command* changes...
        if (e.PropertyName == nameof(CommandTrigger.Dispatcher))
        {
            Log.Logger?.LogDebug($"ConfigVM: SelectedCommand.Dispatcher property changed.");
            // Ensure the Dispatcher ComboBox selection reflects this change.
            SyncDispatcherSelectionFromCommand();
        }
    }

    /// <summary>
    /// Handler for when a property on the AppConfig changes.
    /// Used to notify the UI about changes to validation state.
    /// </summary>
    private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppConfig.IsValid))
        {
            // Forward the notification
            OnPropertyChanged(nameof(IsConfigValid));
        }
        else if (e.PropertyName == nameof(AppConfig.ValidationErrors))
        {
            // Forward the notification
            OnPropertyChanged(nameof(ConfigValidationErrors));
        }
    }

    // --- Synchronization Helper Methods ---

    private void SyncDispatcherSelectionFromCommand()
    {
        var commandDispatcher = CommandListViewModel.SelectedCommand?.Dispatcher;
        if (!ReferenceEquals(DispatcherListViewModel.SelectedDispatcher, commandDispatcher))
        {
            DispatcherListViewModel.SelectedDispatcher = commandDispatcher;
        }
    }

    private void SyncCommandDispatcherFromSelection()
    {
        var selectedItem = DispatcherListViewModel.SelectedDispatcher;
        if (CommandListViewModel.SelectedCommand != null)
        {
            if (selectedItem is DispatcherDefinition selectedDispatcherDef)
            {
                if (!ReferenceEquals(CommandListViewModel.SelectedCommand.Dispatcher, selectedDispatcherDef))
                {
                    CommandListViewModel.SelectedCommand.Dispatcher = selectedDispatcherDef;
                }
            }
        }
    }

    #endregion

    #region ----- Dispose. -----
    private bool _disposed = false; // Track disposal status

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return; // Already disposed

        if (disposing)
        {
            Log.Logger?.LogDebug("ConfigVM: Disposing...");
            // Unsubscribe from events
            if (Config != null)
            {
                Config.PropertyChanged -= Config_PropertyChanged; // *** Unsubscribe from Config ***
            }
            if (CommandListViewModel != null)
            {
                CommandListViewModel.PropertyChanged -= CommandListViewModel_PropertyChanged;
                if (CommandListViewModel.SelectedCommand != null) // Unsubscribe from last selected command
                {
                    CommandListViewModel.SelectedCommand.PropertyChanged -= SelectedCommand_PropertyChanged;
                }
                if (CommandListViewModel is IDisposable disposableCmdVm) disposableCmdVm.Dispose();
            }
            if (DispatcherListViewModel != null)
            {
                DispatcherListViewModel.PropertyChanged -= DispatcherListViewModel_PropertyChanged;
                if (DispatcherListViewModel is IDisposable disposableDispVm) disposableDispVm.Dispose();
            }
            if (ActionTypesViewModel is IDisposable disposableActVm) disposableActVm.Dispose();

            Log.Logger?.LogDebug("ConfigVM: Disposed.");
        }
        _disposed = true;
    }
    #endregion
}
