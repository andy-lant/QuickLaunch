using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickLaunch.Core.Config;

namespace QuickLaunch.UI.ViewModel;

// Inherit from ObservableObject to get INotifyPropertyChanged implementation
public partial class CommandListViewModel : ObservableObject
{
    #region ----- Properties. -----

    #region --- Observable Properties. ---

    /// <summary>
    /// The collection of CommandTrigger objects.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CommandTrigger> _commands;

    // Backing field.
    private CommandTrigger? _selectedCommand;

    /// <summary>
    /// The currently selected command in the UI.
    /// </summary>
    public CommandTrigger? SelectedCommand
    {
        get => _selectedCommand;
        set
        {
            // Store the current value to be used as 'previous'
            var oldValue = _selectedCommand;

            bool valueChanged = SetProperty(ref _selectedCommand, value, nameof(SelectedCommand));

            if (valueChanged)
            {
                // Update the PreviousSelectedCommand property
                PreviousSelectedCommand = oldValue;

                OnPropertyChanged(nameof(IsActualCommandSelected)); // Notify that the selected command status has changed

                // Explicitly notify the command that its CanExecute status might have changed
                // because the SelectedCommand property (which it likely depends on) has changed.
                RemoveCommandCommand?.NotifyCanExecuteChanged();
            }

        }
    }

    /// <summary>
    /// Indicates whether the selected command is an actual CommandTrigger or not.
    /// </summary>
    public bool IsActualCommandSelected => SelectedCommand is CommandTrigger;

    /// <summary>
    /// The command that was previously selected before the current selection.
    /// </summary>
    public CommandTrigger? PreviousSelectedCommand { get; private set; }

    #endregion

    #endregion


    #region ----- Constructors. -----

    public CommandListViewModel(AppConfig config)
    {
        // Initialize the collection
        _commands = config.CommandTriggers;
    }

    #endregion

    #region ----- Event Handlers. -----


    #endregion

    #region ----- Commands. -----

    /// <summary>
    /// Command to add a new item.
    /// </summary>
    /// <param name="command"></param>
    [RelayCommand]
    public void AddCommand()
    {
        CommandTrigger commandTrigger = new();
        commandTrigger.Name = "New Command";
        Commands.Add(commandTrigger);
        SelectedCommand = commandTrigger; // Select the newly added command
    }

    /// <summary>
    /// Command to remove the selected item.
    /// </summary>
    /// <param name="command"></param>
    [RelayCommand(CanExecute = nameof(CanRemoveCommand))]
    public void RemoveCommand(CommandTrigger? command)
    {
        // Check if an item is actually selected before trying to remove
        if (command != null)
        {
            Commands.Remove(command);
            SelectedCommand = null;
        }
    }

    /// <summary>
    /// Method to determine if the RemoveSelectedItem command can execute. 
    /// </summary>
    /// <param name="command"></param>
    /// <returns>Returns true if an item is selected, false otherwise.</returns>
    private bool CanRemoveCommand(CommandTrigger command)
    {
        return command != null;
    }

    #endregion

}
