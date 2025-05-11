using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Config;
using QuickLaunch.Core.Logging;

namespace QuickLaunch.UI.ViewModel;

// Inherit from ObservableObject to get INotifyPropertyChanged implementation
public partial class DispatcherListViewModel : ObservableObject, IDisposable
{
    #region ----- Static Members. -----

    /// <summary>
    /// Placeholder object to represent the "Add new dispatcher" option in the UI.
    /// </summary>
    public static readonly object AddNewDispatcherPlaceholder = new { Name = "<Add new dispatcher>" };

    #endregion

    #region ----- Properties. -----

    #region --- Observable Properties. ---

    /// <summary>
    /// The collection of DispatcherDefinition objects.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DispatcherDefinition> _dispatchers;


    /// <summary>
    /// Gets the collection of items to display in the UI, including the placeholder for adding a new dispatcher.
    /// </summary>
    /// <remarks>
    /// This property is made observable throught the Dispatchers property.
    /// </remarks>
    public IEnumerable<object> DisplayItems => Dispatchers.Cast<object>().Concat(new[] { AddNewDispatcherPlaceholder });


    // Backing field.
    private object? _selectedDispatcher;

    /// <summary>
    /// The currently selected dispatcher in the UI.
    /// </summary>
    public object? SelectedDispatcher
    {
        get => _selectedDispatcher;
        set
        {
            // Store previous value before SetProperty potentially changes it
            var oldValue = _selectedDispatcher;

            // Use SetProperty to update backing field and raise notifications
            if (SetProperty(ref _selectedDispatcher, value, nameof(SelectedDispatcher)))
            {
                OnSelectedDispatcherChanged(oldValue, value);
            }
        }
    }

    // Backing field.
    private DispatcherViewModel? _selectedDispatcherViewModel;

    /// <summary>
    /// The view model for the currently selected dispatcher.
    /// </summary>
    /// <remarks>
    /// This property is made observable through SelectedDispatcher setter.
    /// </remarks>
    public DispatcherViewModel? SelectedDispatcherViewModel
    {
        get => _selectedDispatcherViewModel;
    }


    /// <summary>
    /// Indicates whether the selected dispatcher is an actual DispatcherDefinition.
    /// </summary>
    /// <remarks>
    /// This property is made observable through SelectedDispatcher setter.
    /// </remarks>
    public bool IsActualDispatcherSelected => SelectedDispatcher is DispatcherDefinition;


    #endregion

    #endregion

    #region ----- Constructors. -----
    public DispatcherListViewModel(AppConfig config)
    {
        _dispatchers = config.Dispatchers;

        _dispatchers.CollectionChanged += Dispatchers_CollectionChanged;

        // Initial state update for DisplayItems if needed (though binding should handle it)
        OnPropertyChanged(nameof(DisplayItems));
    }

    #endregion

    #region ----- Event Handlers. -----

    /// <summary>
    /// Handles the CollectionChanged event for the Dispatchers collection.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Dispatchers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // When the underlying collection changes, notify the UI that DisplayItems needs to be requeried
        OnPropertyChanged(nameof(DisplayItems));

        // If the collection was cleared or the selected item was removed, clear selection
        if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
        {
            if (e.OldItems != null && e.OldItems.Contains(SelectedDispatcher))
            {
                SelectedDispatcher = null; // Clear selection if the selected item was removed
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            SelectedDispatcher = null; // Clear selection if the collection was reset
        }
    }

    /// <summary>
    /// Handles the logic when the SelectedDispatcher property changes.
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="value"></param>
    private void OnSelectedDispatcherChanged(object? oldValue, object? value)
    {
        // Property actually changed, now handle the placeholder case
        HandlePlaceholderSelection(oldValue, value);

        // Manually raise dependent property changes
        if (_selectedDispatcher is DispatcherDefinition dispatcherDefinition)
        {
            SetProperty(ref _selectedDispatcherViewModel,
                new DispatcherViewModel(dispatcherDefinition), nameof(SelectedDispatcherViewModel));
        }
        else
        {
            SetProperty(ref _selectedDispatcherViewModel, null, nameof(SelectedDispatcherViewModel));
        }

        OnPropertyChanged(nameof(IsActualDispatcherSelected));

        // Manually notify command CanExecute changes
        RemoveSelectedCommand?.NotifyCanExecuteChanged(); // Use generated command field name
    }

    /// <summary>
    /// Handles the logic when the SelectedDispatcher changes, specifically checking for the placeholder.
    /// </summary>
    /// <param name="oldValue">The previous value of SelectedDispatcher.</param>
    /// <param name="newValue">The new value of SelectedDispatcher.</param>
    private void HandlePlaceholderSelection(object? oldValue, object? newValue)
    {
        // Check if the *new* selection is the placeholder
        if (newValue == AddNewDispatcherPlaceholder)
        {
            // Execute the command to add a new dispatcher
            if (AddNewDispatcherCommand.CanExecute(null))
            {
                AddNewDispatcherCommand.Execute(null);

                // The command itself should handle adding to the collection and selecting the new item.
                // We reset the selection *back* to the previous valid item here,
                // because the AddNewDispatcherCommand will likely change the selection to the *newly created* dispatcher.
                // If AddNewDispatcher fails or is cancelled, we revert to the old selection.
                // We use BeginInvoke to ensure this happens after the current UI update cycle completes.
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    // Check if the command actually changed the selection to a new dispatcher
                    if (SelectedDispatcher == AddNewDispatcherPlaceholder) // If selection didn't change (e.g., user cancelled)
                    {
                        SelectedDispatcher = oldValue; // Revert to the old value
                        Log.Logger?.LogDebug($"Add dispatcher cancelled or failed, reverting selection to: {oldValue?.GetType().GetProperty("Name")?.GetValue(oldValue) ?? "null"}");
                    }
                    else
                    {
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            else
            {
                // Cannot execute the command, revert selection immediately
                SelectedDispatcher = oldValue;
            }
        }
    }

    /// <summary>
    /// Handles selection changes in the UI.
    /// </summary>
    /// <remarks>Needed to intercept the "add new" placeholder.</remarks>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Debug.Assert(e.RemovedItems.Count < 2, $"Unexpected RemovedItems.Count {e.RemovedItems.Count} (> 1)");
        object? oldValue = e.RemovedItems.Count == 0 ? null : e.RemovedItems[0];

        Debug.Assert(e.AddedItems.Count < 2, $"Unexpected AddedItems.Count {e.AddedItems.Count} (> 1)");
        object? newValue = e.AddedItems.Count == 0 ? null : e.AddedItems[0];

        if (newValue != oldValue && newValue == AddNewDispatcherPlaceholder)
        {
            OnSelectedDispatcherChanged(oldValue, newValue);
        }
    }

    #endregion

    #region ----- Commands. -----

    /// <summary>
    /// Command to add a new dispatcher.
    /// </summary>
    [RelayCommand]
    private void AddNewDispatcher()
    {
        const string baseName = "New Dispatcher";
        string newName = baseName;
        int counter = 1;

        // Check if the base name or numbered names already exist
        // Use a HashSet for efficient lookups
        var existingNames = new HashSet<string>(Dispatchers.Select(d => d.Name));

        while (existingNames.Contains(newName))
        {
            counter++;
            newName = $"{baseName} {counter}";
        }

        // Logic to create and add a new DispatcherDefinition
        var newDispatcher = new DispatcherDefinition
        {
            Name = newName
        };

        Dispatchers.Add(newDispatcher); // Add to the underlying collection

        // Select the newly added dispatcher
        SelectedDispatcher = newDispatcher;
    }

    /// <summary>
    /// Command to remove the selected dispatcher.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemoveSelected))]
    private void RemoveSelected()
    {
        if (SelectedDispatcher is DispatcherDefinition dispatcherToRemove)
        {
            string removedName = dispatcherToRemove.Name;
            Dispatchers.Remove(dispatcherToRemove);
            Log.Logger?.LogDebug($"Removed dispatcher: {removedName}");
            // Selection will be cleared automatically by CollectionChanged handler if the selected item was removed.
        }
    }

    /// <summary>
    /// Checks if the selected item can be removed.
    /// </summary>
    /// <returns></returns>
    private bool CanRemoveSelected()
    {
        // Can only remove if the selected item is an actual DispatcherDefinition
        return SelectedDispatcher is DispatcherDefinition;
    }

    #endregion

    #region ----- IDisposable. -----

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Dispatchers != null)
            {
                Dispatchers.CollectionChanged -= Dispatchers_CollectionChanged;
            }
        }
    }

    #endregion
}
