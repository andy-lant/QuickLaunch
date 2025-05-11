using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Actions; // For ActionType
using QuickLaunch.Core.Config;
using QuickLaunch.Core.Logging;
using QuickLaunch.Core.Utils;   // For DispatcherDefinition, ActionRegistration

namespace QuickLaunch.UI.ViewModel;

/// <summary>
/// ViewModel wrapper for a DispatcherDefinition model object.
/// Provides properties suitable for binding in the DispatcherEditorControl.
/// </summary>
public partial class DispatcherViewModel : ObservableObject, IDisposable
{
    #region ----- Properties. -----

    // The underlying data model
    public DispatcherDefinition Model { get; }

    #region --- Observable Properties. ---

    /// <summary>
    /// Gets or sets the name of the dispatcher instance.
    /// </summary>
    public string Name
    {
        get => Model.Name;
        set
        {
            // Check if the value actually changed before setting
            if (Model.Name != value)
            {
                try
                {
                    Model.Name = value; // Set the model property (which should handle validation)
                    OnPropertyChanged(); // Notify UI of change
                }
                catch (ArgumentException ex)
                {
                    // Handle validation errors from the model (e.g., show a message)
                    Log.Logger?.LogDebug($"Validation Error setting Name: {ex.Message}");
                    // Optionally re-raise or display to user
                }
            }
        }
    }

    /// <summary>
    /// Gets a read-only collection of wrapper objects for the dispatcher's actions,
    /// suitable for binding to the ActionsListView.
    /// </summary>
    public ExtendedObservableCollection<DispatcherActionEntry> Actions { get; private set; }

    /// <summary>
    /// The currently selected action in the UI.
    /// </summary>
    [ObservableProperty]
    public DispatcherActionEntry? _selectedAction;

    #endregion

    #endregion


    #region ----- Constructors. -----

    /// <summary>
    /// Initializes a new instance of the DispatcherViewModel class.
    /// </summary>
    /// <param name="model">The DispatcherDefinition model to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown if the model is null.</exception>
    public DispatcherViewModel(DispatcherDefinition model)
    {
        Model = model;
        Actions = model.Actions;
    }

    #endregion

    #region ----- Event Handlers. -----
    partial void OnSelectedActionChanged(DispatcherActionEntry? value)
    {
    }

    #endregion

    #region ----- Commands. -----

    /// <summary>
    /// Command to add a new default action (NoAction) to the list.
    /// </summary>
    [RelayCommand]
    private void AddAction()
    {
        // 1. Calculate the next available index
        uint nextIndex = 1; // Default if list is empty
        if (Actions != null && Actions.Count > 0)
        {
            // Find the maximum current index and add 1
            // Add safety check in case indices are not sequential or empty list has non-zero index somehow
            try
            {
                nextIndex = Actions.Max(a => a.Index) + 1;
            }
            catch (InvalidOperationException)
            {
                // This can happen if Actions is empty, handle gracefully
                nextIndex = 1;
            }
        }

        // 2. Create the default action (NoAction)
        // Ensure QuickLaunch.Actions.NoAction exists and is the correct type
        var defaultAction = ActionRegistration.Create(NoAction.ActionType, new List<ActionParameter>());

        // 3. Create the DispatcherActionEntry wrapper
        var newActionEntry = DispatcherActionEntry.Create(nextIndex, defaultAction);

        // 4. Add to the collection (assuming Actions is ObservableCollection or similar)
        Actions?.Add(newActionEntry); // Add null check for safety
        Log.Logger?.LogDebug($"Added new action with Index {nextIndex}");

        // 5. Optionally select the newly added action
        SelectedAction = newActionEntry;
    }

    /// <summary>
    /// Command to remove the currently selected action.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemoveAction))]
    private void RemoveAction()
    {
        if (SelectedAction != null && Actions != null)
        {
            uint removedIndex = SelectedAction.Index;
            bool removed = Actions.Remove(SelectedAction); // Remove from the collection
            if (removed)
            {
                Log.Logger?.LogDebug($"Removed action with Index {removedIndex}");
                // After removal, SelectedAction should ideally become null.
                // If the binding is TwoWay, this might happen automatically.
                // If not, or for explicit clarity:
                // SelectedAction = null;
            }
            else
            {
                Log.Logger?.LogDebug($"Failed to remove action with Index {removedIndex}");
            }
        }
    }

    /// <summary>
    /// Determines if the RemoveAction command can execute.
    /// </summary>
    /// <returns>True if an action is selected, false otherwise.</returns>
    private bool CanRemoveAction() => SelectedAction != null;

    #endregion


    #region ----- Dispose Pattern. -----
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Unsubscribe from model events
                if (Model != null)
                {
                    //Model.DispatcherChanged -= Model_DispatcherChanged;
                }
                Log.Logger?.LogDebug($"DispatcherViewModel ({Name}) disposed.");
            }
            _disposed = true;
        }
    }
    #endregion
}
