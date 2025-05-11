using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Config; // For config classes
using QuickLaunch.Core.Logging;
using QuickLaunch.UI.Utils;
using QuickLaunch.UI.ViewModel;

// Use file-scoped namespace - QuickLaunch.UI
namespace QuickLaunch.UI;

/// <summary>
/// Interaction logic for ConfigEditorWindow.xaml
/// Allows editing the application configuration, focusing on commands and viewing their dispatchers/actions.
/// </summary>
public partial class ConfigEditorWindow : Window
{
    private readonly ConfigurationViewModel modelView;

    private ConfigurationLoader _configLoader = new();

    private AppConfig _config;

#if DEBUG   // for testing purposes only
    public ConfigEditorWindow()
    {
        InitializeComponent();
        _config = _configLoader.LoadConfig();
        modelView = new ConfigurationViewModel(_config);
        DataContext = modelView;
    }
#endif

    public ConfigEditorWindow(AppConfig config)
    {
        InitializeComponent();
        this._config = config;
        modelView = new ConfigurationViewModel(_config);
        DataContext = modelView;
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            var pos = ScreenHelpers.GetWindowDesktopPosition(this);
            Log.Logger?.LogDebug(pos.ToString());
        }
        catch (Exception ex)
        {
            Log.Logger?.LogDebug(ex.ToString());
        }
    }

    private void AddCommandButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Logger?.LogDebug("AddCommandButton_Click");
    }

    private void RemoveCommandButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Logger?.LogDebug("RemoveCommandButton_Click");
    }

    private void CommandListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        Console.WriteLine($"Closing window, config has {_config.CommandTriggers.Count} commands");
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        bool result = false;
        if (this._config.IsValid)
        {
            _configLoader.SaveConfig(_config);
            result = true;
        }

        if (!result)
        {
            MessageBox.Show(this, "Configuration is not valid. Please check the configuration.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        else
        {
            DialogResult = result;
            Close();
        }
    }

    private void SelectedDispatcherEditor_Loaded(object sender, RoutedEventArgs e)
    {

    }
}

/*
    // Define the placeholder string
    public const string NewDispatcherOption = "<Create New Dispatcher...>";

    public ObservableCollection<CommandTrigger> CommandDefinitions { get; set; } = new();

    private CommandTrigger? _selectedCommand;
    public CommandTrigger? SelectedCommand
    {
        get => _selectedCommand;
        set => SetField(ref _selectedCommand, value);
    }

    private DispatcherDefinition? _selectedCommandDispatcherDefinition;
    public DispatcherDefinition? SelectedCommandDispatcherDefinition
    {
        get => _selectedCommandDispatcherDefinition;
        set => SetField(ref _selectedCommandDispatcherDefinition, value);
    }

    private ObservableCollection<ActionRegistration> _selectedCommandDispatcherActions = new();
    public ObservableCollection<ActionRegistration> SelectedCommandDispatcherActions
    {
        get => _selectedCommandDispatcherActions;
        set => SetField(ref _selectedCommandDispatcherActions, value);
    }

    // New property to control if dispatcher type can be changed
    private bool _isSelectedDispatcherTypeChangeAllowed = true;
    public bool IsSelectedDispatcherTypeChangeAllowed
    {
        get => _isSelectedDispatcherTypeChangeAllowed;
        set => SetField(ref _isSelectedDispatcherTypeChangeAllowed, value);
    }


    // Holds the names of dispatchers defined in the config + the <New...> option
    public ObservableCollection<string> AvailableDispatcherNames { get; set; } = new();


    public ConfigEditorWindow()
    {
        InitializeComponent();
        _configFilePath = _configLoader.GetConfigFilePath();
        this.DataContext = this;
        if (Application.Current.MainWindow != null && Application.Current.MainWindow != this) { this.Owner = Application.Current.MainWindow; }

        // Subscribe to the event from the CommandEditorControl
        SelectedCommandEditor.CreateNewDispatcherRequested += SelectedCommandEditor_CreateNewDispatcherRequested;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadConfigData();
    }

    /// <summary>
    /// Loads the AppConfig from the file and populates internal collections.
    /// </summary>
    private void LoadConfigData()
    {
        //try
        //{
        //    _loadedConfig = _configLoader.LoadConfig();

        //    PopulateAvailableDispatcherNames(); // Use helper method

        //    CommandDefinitions.Clear();
        //    if (_loadedConfig.Commands != null)
        //    {
        //        foreach (var commandDef in _loadedConfig.Commands.Values.OrderBy(c => c.Name))
        //        {
        //            CommandDefinitions.Add(CloneCommand(commandDef)); // Add clones
        //        }
        //    }

        //    CommandListView.SelectedIndex = CommandDefinitions.Any() ? 0 : -1;
        //}
        //catch (Exception ex)
        //{
        //    Log.Logger?.LogDebug($"ERROR loading configuration data: {ex.Message}");
        //    MessageBox.Show(this, $"Failed to load or parse configuration file:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //    this.Close();
        //}
    }

    //private void ModelChanged()
    //{

    //}

    /// <summary>
    /// Populates the AvailableDispatcherNames collection from the loaded config.
    /// </summary>
    private void PopulateAvailableDispatcherNames()
    {
        AvailableDispatcherNames.Clear();
        AvailableDispatcherNames.Add(NewDispatcherOption); // Add placeholder first
        if (_loadedConfig.Dispatchers is not null)
        {
            foreach (var dispatcherDef in _loadedConfig.Dispatchers.Values.OrderBy(d => d.Name))
            {
                if (!string.IsNullOrWhiteSpace(dispatcherDef.Name)) { AvailableDispatcherNames.Add(dispatcherDef.Name); }
            }
        }
        Log.Logger?.LogDebug($"Populated AvailableDispatcherNames with {AvailableDispatcherNames.Count} items (including placeholder).");
    }

    /// <summary>
    /// Handles selection changes in the Command ListView.
    /// </summary>
    private void CommandListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        throw new NotImplementedException();
        //SelectedCommand = CommandListView.SelectedItem as CommandTrigger;
        //SelectedCommandDispatcherDefinition = null;
        //SelectedCommandDispatcherActions.Clear();
        //IsSelectedDispatcherTypeChangeAllowed = true; // Default to allowed

        //if (SelectedCommand != null && !string.IsNullOrEmpty(SelectedCommand.Dispatcher) && SelectedCommand.Dispatcher != NewDispatcherOption)
        //{
        //    SelectedCommandDispatcherDefinition = _loadedConfig.Dispatchers?
        //        .FirstOrDefault(d => d.Name.Equals(SelectedCommand.Dispatcher, StringComparison.OrdinalIgnoreCase));

        //    if (SelectedCommandDispatcherDefinition != null)
        //    {
        //        var actions = _loadedConfig.Actions?
        //           .Where(a => a.Dispatcher.Equals(SelectedCommandDispatcherDefinition.Name, StringComparison.OrdinalIgnoreCase))
        //           .OrderBy(a => a.Index ?? int.MaxValue)
        //           .ToList() ?? new List<ActionRegistration>();
        //        foreach (var action in actions) { SelectedCommandDispatcherActions.Add(action); }

        //        IsSelectedDispatcherTypeChangeAllowed = !actions.Any();
        //    }
        //    else { Log.Logger?.LogDebug($"Warning: Dispatcher definition '{SelectedCommand.Dispatcher}' not found."); IsSelectedDispatcherTypeChangeAllowed = true; }
        //}
        //Log.Logger?.LogDebug($"Selected command '{SelectedCommand?.Name ?? "None"}'. Associated dispatcher: '{SelectedCommandDispatcherDefinition?.Name ?? "None"}'.");
    }

    // --- Command List Button Handlers ---

    private void AddCommandButton_Click(object sender, RoutedEventArgs e)
    {
        var newCommand = CommandTrigger.Create(
            "NewCommand" + CommandDefinitions.Count,
            AvailableDispatcherNames.FirstOrDefault(n => n != NewDispatcherOption) ?? NewDispatcherOption
        );
        CommandDefinitions.Add(newCommand);
        CommandListView.SelectedItem = newCommand;
        CommandListView.ScrollIntoView(newCommand);
        Log.Logger?.LogDebug($"Added new command placeholder: {newCommand.Name}.");
        // Fix: Use this.Dispatcher to call BeginInvoke
        this.Dispatcher.BeginInvoke(new Action(() => { SelectedCommandEditor?.NameTextBox?.Focus(); SelectedCommandEditor?.NameTextBox?.SelectAll(); }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void RemoveCommandButton_Click(object sender, RoutedEventArgs e)
    {
        CommandTrigger? commandToRemove = SelectedCommand;
        if (commandToRemove == null) { MessageBox.Show(this, "Select command.", "Selection Required"); return; }
        if (MessageBox.Show(this, $"Remove command '{commandToRemove.Name}' ('{commandToRemove.Sequence}')?", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            string removedName = commandToRemove.Name;
            CommandDefinitions.Remove(commandToRemove);
            Log.Logger?.LogDebug($"Removed command: {removedName}");
        }
    }

    // --- Event Handler for New Dispatcher Request ---
    private void SelectedCommandEditor_CreateNewDispatcherRequested(object? sender, EventArgs e)
    {
        //Log.Logger?.LogDebug("Create New Dispatcher requested from Command Editor.");

        //string baseName = "NewDispatcher";
        //string newDispatcherName = baseName;
        //int counter = 1;
        //while (AvailableDispatcherNames.Contains(newDispatcherName, StringComparer.OrdinalIgnoreCase)) { counter++; newDispatcherName = $"{baseName}{counter}"; }

        //var newDispatcherDef = DispatcherDefinition.Create(newDispatcherName, "OpenFile", ReadOnlyCollection<(uint, ActionRegistration)>.Empty);

        //_loadedConfig.Dispatchers ??= new List<DispatcherDefinition>();
        //_loadedConfig.Dispatchers.Add(newDispatcherDef);

        //// Repopulate the list (this will include sorting and the placeholder)
        //PopulateAvailableDispatcherNames();

        //// Select the newly created item in the ComboBox for the current command
        //SelectedCommandEditor.DispatcherComboBox.SelectedItem = newDispatcherName;
        //if (SelectedCommand != null) { SelectedCommand.Dispatcher = newDispatcherName; }

        //MessageBox.Show(this, $"Created new dispatcher '{newDispatcherName}' of type 'OpenFile'.", "Dispatcher Created", MessageBoxButton.OK, MessageBoxImage.Information);
    }


    // --- Global Save/Cancel Handlers ---

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        //Log.Logger?.LogDebug("Save Button clicked in Config Editor Window.");

        //// 1. Validate & Update the currently selected command editor
        //if (SelectedCommand != null)
        //{
        //    if (!SelectedCommandEditor.ValidateInput()) { return; }
        //    SelectedCommandEditor.UpdateCommandFromControls();
        //}

        //// 2. Validate Dispatcher Editor (Read-Only - No validation needed)

        //// 3. Validate Command Names (Uniqueness)
        //var duplicateCmdNames = CommandDefinitions.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase).Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1).Select(g => g.Key).ToList();
        //if (duplicateCmdNames.Any()) { MessageBox.Show(this, $"Duplicate command names found: {string.Join(", ", duplicateCmdNames)}.", "Validation Error"); return; }

        //// 4. Validate Dispatcher Names (Uniqueness in the _loadedConfig list)
        //var duplicateDispNames = _loadedConfig.Dispatchers?
        //    .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
        //    .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
        //    .Select(g => g.Key).ToList() ?? new List<string>();
        //if (duplicateDispNames.Any())
        //{
        //    MessageBox.Show(this, $"Duplicate dispatcher names found: {string.Join(", ", duplicateDispNames)}.\nPlease resolve duplicates before saving.", "Validation Error");
        //    return;
        //}

        // 5. Check if any command still references the placeholder dispatcher (Should be caught by editor validation)

        // 6. Reconstruct the AppConfig object for saving
        //var configToSave = new AppConfig();
        //configToSave.Dispatchers.AddRange(_loadedConfig.Dispatchers ?? Enumerable.Empty<DispatcherDefinition>());
        //configToSave.Actions.AddRange(_loadedConfig.Actions ?? Enumerable.Empty<ActionRegistration>());
        //configToSave.Commands.AddRange(CommandDefinitions); // Add the edited commands (clones)

        //// 7. Serialize & Save
        //try
        //{
        //    string newTomlContent = Toml.FromModel(configToSave);
        //    File.WriteAllText(_configFilePath, newTomlContent);
        //    Log.Logger?.LogDebug($"Saved configuration to file: {_configFilePath}");
        //    this.DialogResult = true;
        //    this.Close();
        //}
        //catch (Exception ex)
        //{
        //    Log.Logger?.LogDebug($"ERROR saving configuration: {ex.Message}");
        //    MessageBox.Show(this, $"Failed to save configuration file:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //}
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Logger?.LogDebug("Cancel Button clicked in Config Editor Window.");
        this.DialogResult = false;
        this.Close();
    }


    // --- INotifyPropertyChanged Implementation ---
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(propertyName); return true; }
}
*/