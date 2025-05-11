using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickLaunch.Core.Actions;
using QuickLaunch.Core.KeyEvents;
using QuickLaunch.Core.Utils;

namespace QuickLaunch.Core.Config;

/// <summary>
/// Represents the overall application configuration loaded from TOML.
/// Contains lists of dispatcher definitions and command triggers.
/// Ensures validity upon creation via the static Create factory method.
/// Allows re-validation of the current state via the IValidate interface.
/// </summary>
public partial class AppConfig : ObservableObject, IDisposable, IValidate
{
    // Backing fields for observable properties
    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private IReadOnlyList<string> _validationErrors = new List<string>(); // Initialize to empty

    // Backing field
    private readonly ExtendedObservableCollection<DispatcherDefinition> _dispatchers;
    /// <summary>
    /// The collection of dispatcher definitions.
    /// </summary>
    public ExtendedObservableCollection<DispatcherDefinition> Dispatchers => _dispatchers;

    // Backing field
    private readonly ExtendedObservableCollection<CommandTrigger> _commandTriggers;
    /// <summary>
    /// The collection of command trigger definitions.
    /// </summary>
    public ExtendedObservableCollection<CommandTrigger> CommandTriggers => _commandTriggers;

    // ----- Constructors -----

    /// <summary>
    /// Private constructor. Initializes collections, subscribes to events, and validates the initial state.
    /// </summary>
    /// <param name="dispatchers">The collection of dispatchers.</param>
    /// <param name="commandTriggers">The collection of command triggers.</param>
    /// <exception cref="ArgumentException">Thrown if initial validation fails.</exception>
    private AppConfig(IEnumerable<DispatcherDefinition> dispatchers, IEnumerable<CommandTrigger> commandTriggers)
    {
        // Initialize collections first
        _dispatchers = new(dispatchers);
        _commandTriggers = new(commandTriggers);

        // Subscribe to collection changes
        _dispatchers.CollectionChanged += OnDispatchersChanged;
        _commandTriggers.CollectionChanged += OnCommandTriggersChanged;

        // Subscribe to PropertyChanged for existing items
        SubscribeToItemChanges(_dispatchers);
        SubscribeToItemChanges(_commandTriggers);

        // Validate the initial state and set IsValid/ValidationErrors properties.
        // This replaces the exception throwing directly in the constructor.
        TriggerRevalidation();
        // If the initial state must throw, check IsValid here:
        if (!IsValid)
        {
            // Use the more general ThrowIfErrors as the errors could come from either input list or relationships
            Errors.ThrowIfErrors(typeof(ArgumentException), ValidationErrors);
        }
    }

    /// <summary>
    /// Initializes an empty, valid <see cref="AppConfig"/> instance.
    /// </summary>
    public AppConfig()
    {
        _dispatchers = new();
        _commandTriggers = new();

        // Subscribe to collection changes for the initially empty collections
        _dispatchers.CollectionChanged += OnDispatchersChanged;
        _commandTriggers.CollectionChanged += OnCommandTriggersChanged;

        // Initial state is valid
        _isValid = true;
    }


    /// <summary>
    /// Creates, validates, and returns a new instance of the <see cref="AppConfig"/> class.
    /// </summary>
    /// <param name="dispatchers">An enumerable collection of dispatcher definitions.</param>
    /// <param name="commandTriggers">An enumerable collection of command trigger definitions.</param>
    /// <returns>A validated AppConfig instance.</returns>
    /// <exception cref="ArgumentException">Thrown if validation fails (e.g., duplicate names, invalid references). Collected errors are joined in the message.</exception>
    public static AppConfig Create(IEnumerable<DispatcherDefinition>? dispatchers, IEnumerable<CommandTrigger>? commandTriggers)
    {
        // The private constructor handles the validation check.
        // Ensure null inputs become empty enumerables.
        return new AppConfig(dispatchers ?? Array.Empty<DispatcherDefinition>(), commandTriggers ?? Array.Empty<CommandTrigger>());
    }

    // ----- Event Handlers -----

    private void OnDispatchersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HandleCollectionChanges<DispatcherDefinition>(e, SubscribeToDispatcherChanges, UnsubscribeFromDispatcherChanges);
    }

    private void OnCommandTriggersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HandleCollectionChanges<CommandTrigger>(e, SubscribeToTriggerChanges, UnsubscribeFromTriggerChanges);
    }

    private void HandleCollectionChanges<T>(NotifyCollectionChangedEventArgs e, Action<T> subscribe, Action<T> unsubscribe) where T : class, INotifyPropertyChanged
    {
        // Unsubscribe from old items
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<T>())
            {
                unsubscribe(item);
            }
        }

        // Subscribe to new items
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<T>())
            {
                subscribe(item);
            }
        }

        // Re-validate whenever the collection changes
        TriggerRevalidation();
    }

    private void OnDispatcherPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        TriggerRevalidation();
    }

    private void OnCommandTriggerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        TriggerRevalidation();
    }


    // ----- Subscription Helpers -----

    private void SubscribeToItemChanges<T>(IEnumerable<T> items) where T : class, INotifyPropertyChanged
    {
        foreach (var item in items.OfType<DispatcherDefinition>()) SubscribeToDispatcherChanges(item);
        foreach (var item in items.OfType<CommandTrigger>()) SubscribeToTriggerChanges(item);
    }

    private void UnsubscribeFromItemChanges<T>(IEnumerable<T> items) where T : class, INotifyPropertyChanged
    {
        foreach (var item in items.OfType<DispatcherDefinition>()) UnsubscribeFromDispatcherChanges(item);
        foreach (var item in items.OfType<CommandTrigger>()) UnsubscribeFromTriggerChanges(item);
    }

    private void SubscribeToDispatcherChanges(DispatcherDefinition item)
    {
        if (item != null) item.PropertyChanged += OnDispatcherPropertyChanged;
        // Potentially subscribe to Action collection changes within dispatcher if needed
    }
    private void UnsubscribeFromDispatcherChanges(DispatcherDefinition item)
    {
        if (item != null) item.PropertyChanged -= OnDispatcherPropertyChanged;
        // Potentially unsubscribe from Action collection changes
    }

    private void SubscribeToTriggerChanges(CommandTrigger item)
    {
        if (item != null) item.PropertyChanged += OnCommandTriggerPropertyChanged;
    }
    private void UnsubscribeFromTriggerChanges(CommandTrigger item)
    {
        if (item != null) item.PropertyChanged -= OnCommandTriggerPropertyChanged;
    }



    // ----- Validation -----

    /// <summary>
    /// Triggers re-validation of the configuration and updates the IsValid
    /// and ValidationErrors properties.
    /// </summary>
    private void TriggerRevalidation()
    {
        IsValid = Validate(out var errors);
        ValidationErrors = errors; // Update the observable property
    }

    /// <summary>
    /// Validates the current state of the AppConfig instance according to the IValidate interface.
    /// Checks for duplicate names, null references, invalid dispatcher links, and validates child items.
    /// </summary>
    /// <param name="errors">An output list populated with validation error messages if validation fails.</param>
    /// <returns>True if the configuration is currently valid, false otherwise.</returns>
    public bool Validate(out IReadOnlyList<string> errors)
    {
        List<string> currentErrors = new();

        // Use the current state of the observable collections
        var currentDispatchers = _dispatchers.ToList(); // Take snapshot for consistent validation run
        var currentTriggers = _commandTriggers.ToList();

        // 1. Validate Dispatchers (check for nulls, names, duplicates, call Validate on children)
        var dispatcherDict = new Dictionary<string, DispatcherDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var dispatcher in currentDispatchers)
        {
            // Check for null items
            if (dispatcher is null)
            {
                currentErrors.Add($"Dispatcher in the collection is null.");
                continue; // Skip further validation for this item
            }
            // Check name validity
            if (string.IsNullOrWhiteSpace(dispatcher.Name))
            {
                currentErrors.Add($"Dispatcher '{dispatcher}' has a null or empty name.");
            }
            // Check for duplicate names within the current collection
            else if (!dispatcherDict.TryAdd(dispatcher.Name, dispatcher))
            {
                currentErrors.Add($"Duplicate dispatcher name found: '{dispatcher.Name}'. Names must be unique (case-insensitive).");
            }

            // Validate the dispatcher itself (assuming DispatcherDefinition implements IValidate)
            if (dispatcher is IValidate validatableDispatcher) // Check if it implements IValidate
            {
                if (!validatableDispatcher.Validate(out var dispatcherErrors))
                {
                    currentErrors.AddRange(dispatcherErrors.Select(e => $"Dispatcher '{dispatcher.Name}': {e}"));
                }
            }
            // else { /* Optional: Add warning if dispatcher doesn't implement IValidate */ }
        }

        // 2. Validate Command Triggers (check for nulls, names, duplicates, dispatcher references, call Validate on children)
        var commandTriggerDict = new Dictionary<string, CommandTrigger>(StringComparer.OrdinalIgnoreCase);
        foreach (var trigger in currentTriggers)
        {
            // Check for null items
            if (trigger is null)
            {
                currentErrors.Add($"Command trigger in the collection is null.");
                continue; // Skip further validation for this item
            }
            // Check name validity
            if (string.IsNullOrWhiteSpace(trigger.Name))
            {
                currentErrors.Add($"Command trigger '{trigger}' has a null or empty name.");
            }
            // Check for duplicate names within the current collection
            else if (!commandTriggerDict.TryAdd(trigger.Name, trigger))
            {
                currentErrors.Add($"Duplicate command trigger name found: '{trigger.Name}'. Names must be unique (case-insensitive).");
            }

            // Validate the trigger itself (assuming CommandTrigger implements IValidate)
            if (trigger is IValidate validatableTrigger) // Check if it implements IValidate
            {
                if (!validatableTrigger.Validate(out var triggerErrors))
                {
                    currentErrors.AddRange(triggerErrors.Select(e => $"Trigger '{trigger.Name}': {e}"));
                }
            }
            // else { /* Optional: Add warning if trigger doesn't implement IValidate */ }


            // Check dispatcher reference validity (only if trigger itself is not null)
            if (trigger.Dispatcher == null)
            {
                currentErrors.Add($"Command trigger '{trigger.Name}' has a null Dispatcher reference.");
            }
            // Check if the referenced dispatcher exists in the *current* dispatcher dictionary (built above)
            else if (!dispatcherDict.TryGetValue(trigger.Dispatcher.Name, out DispatcherDefinition? dispatcherRef))
            {
                // This covers cases where the dispatcher was removed or renamed after initial construction
                currentErrors.Add($"Command trigger '{trigger.Name}' references dispatcher '{trigger.Dispatcher.Name}', which is not found in the current valid dispatcher list.");
            }
            // Check instance equality - treat as error if required for UI consistency
            else if (!ReferenceEquals(trigger.Dispatcher, dispatcherRef))
            {
                currentErrors.Add($"Command trigger '{trigger.Name}' dispatcher reference is not the same instance found in the current dispatcher list. This may cause UI inconsistencies.");
            }
        }

        errors = currentErrors;
        return currentErrors.Count == 0;
    }

    // ----- IDisposable Implementation -----

    private bool _isDisposed = false; // To detect redundant calls to Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                if (_dispatchers != null)
                {
                    _dispatchers.CollectionChanged -= OnDispatchersChanged;
                    UnsubscribeFromItemChanges(_dispatchers); // Unsubscribe from all items
                }
                if (_commandTriggers != null)
                {
                    _commandTriggers.CollectionChanged -= OnCommandTriggersChanged;
                    UnsubscribeFromItemChanges(_commandTriggers); // Unsubscribe from all items
                }
            }

            _commandTriggers?.Clear();
            _dispatchers?.Clear();

            _isDisposed = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns a string summary of the configuration.
    /// </summary>
    /// <returns>A string containing counts of dispatchers, triggers, and total actions.</returns>
    public override string ToString()
    {
        // Calculate total actions across all dispatchers
        int totalActionRegistrations = _dispatchers?.Sum(d => d?.Actions?.Count ?? 0) ?? 0;

        return $"AppConfig: {Dispatchers?.Count ?? 0} Dispatchers, {CommandTriggers?.Count ?? 0} Triggers, {totalActionRegistrations} Actions. Valid: {IsValid}";
    }
}


// --------------------------------------------------
// Updated Class: DispatcherDefinition
// --------------------------------------------------

/// <summary>
/// Represents the definition of a dispatcher instance.
/// Reacts to changes in its Actions collection to maintain validation state.
/// </summary>
public partial class DispatcherDefinition : ObservableObject, IValidate, IDisposable // Implement IValidate and IDisposable
{
    // Backing fields for observable properties
    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private IReadOnlyList<string> _validationErrors = new List<string>();

    /// <summary>
    /// Unique identifier name for this dispatcher instance.
    /// Used for linking actions and commands. Case-insensitive recommended.
    /// </summary>
    [ObservableProperty]
    private string _name;

    // Backing field
    private readonly ExtendedObservableCollection<DispatcherActionEntry> _actions;

    /// <summary>
    /// The actions associated to this dispatcher instance. Changes trigger re-validation.
    /// </summary>
    public ExtendedObservableCollection<DispatcherActionEntry> Actions => _actions;

    private bool _isDisposed = false; // To detect redundant calls to Dispose

    #region ----- Constructors. -----

    /// <summary>
    /// Private constructor. Initializes name and actions, subscribes to events, and validates initial state.
    /// </summary>
    /// <param name="name">The name of the dispatcher.</param>
    /// <param name="actionEntries">The collection of DispatcherActionEntry items.</param>
    private DispatcherDefinition(string name, IEnumerable<DispatcherActionEntry> actionEntries)
    {
        this._name = name; // Assumes name is pre-validated by Create method
        var entryList = actionEntries?.ToList() ?? Array.Empty<DispatcherActionEntry>().ToList();

        // Perform initial structural validation (nulls, duplicate indices) before creating collection
        if (!ValidateActionsStructure(entryList, out var structureErrors))
        {
            Errors.ThrowArgumentExceptionIfErrors(structureErrors, nameof(actionEntries)); // Parameter name changed
        }

        // Initialize collection and subscribe
        this._actions = new(entryList);
        this._actions.CollectionChanged += OnActionsChanged;
        SubscribeToActionChanges(this._actions); // Subscribe to existing items

        // Perform initial full validation (including children)
        TriggerRevalidation();
        // If the initial state must throw, check IsValid here:
        if (!IsValid)
        {
            Errors.ThrowArgumentExceptionIfErrors(ValidationErrors, "actions"); // Use generic name if needed
        }
    }

    /// <summary>
    /// Factory method to create and validate a DispatcherDefinition.
    /// </summary>
    /// <param name="name">The name of the dispatcher.</param>
    /// <param name="actions">The actions associated with the dispatcher, as tuples.</param>
    /// <returns>A validated DispatcherDefinition instance.</returns>
    /// <exception cref="ArgumentException">Thrown if name is invalid or initial actions validation fails.</exception>
    public static DispatcherDefinition Create(string name, IEnumerable<DispatcherActionEntry> actions)
    {
        // Validate name immediately
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Dispatcher name cannot be null or whitespace.", nameof(name));
        }

        // Further validation (structure, initial state) happens inside the private constructor
        return new DispatcherDefinition(name, actions);
    }

    /// <summary>
    /// Creates a new DispatcherDefinition with an empty name and no actions.
    /// This definition is initially invalid!
    /// </summary>
    public DispatcherDefinition()
    {
        _actions = new();
        _name = string.Empty; // Initialize to empty
        _isValid = false;
        _validationErrors = new List<string>(); // Initialize to empty

        TriggerRevalidation(); // Initial state is invalid
    }

    #endregion

    // ----- Event Handlers -----

    private void OnActionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Use the new class type
        HandleActionCollectionChanges(e, SubscribeToActionEntryChanges, UnsubscribeFromActionEntryChanges);
    }

    private void HandleActionCollectionChanges(NotifyCollectionChangedEventArgs e, Action<DispatcherActionEntry> subscribe, Action<DispatcherActionEntry> unsubscribe)
    {
        // Unsubscribe from old items
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<DispatcherActionEntry>())
            {
                unsubscribe(item);
            }
        }

        // Subscribe to new items
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<DispatcherActionEntry>())
            {
                subscribe(item);
            }
        }

        // Re-validate whenever the collection changes
        TriggerRevalidation();
    }

    // Renamed handler
    private void OnDispatcherActionEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-validate if the entry's IsValid state changes, or its Index changes (affects uniqueness)
        if (e.PropertyName == nameof(DispatcherActionEntry.IsValid) ||
            e.PropertyName == nameof(DispatcherActionEntry.Index))
        {
            TriggerRevalidation();
        }
    }


    // ----- Subscription Helpers -----

    // Renamed methods to reflect the new type
    private void SubscribeToActionChanges(IEnumerable<DispatcherActionEntry> items)
    {
        foreach (var item in items) SubscribeToActionEntryChanges(item);
    }

    private void UnsubscribeFromActionChanges(IEnumerable<DispatcherActionEntry> items)
    {
        foreach (var item in items) UnsubscribeFromActionEntryChanges(item);
    }

    private void SubscribeToActionEntryChanges(DispatcherActionEntry item)
    {
        if (item != null)
        {
            item.PropertyChanged += OnDispatcherActionEntryPropertyChanged;
        }
    }
    private void UnsubscribeFromActionEntryChanges(DispatcherActionEntry item)
    {
        if (item != null)
        {
            item.PropertyChanged -= OnDispatcherActionEntryPropertyChanged;
        }
    }


    // ----- Validation -----

    /// <summary>
    /// Triggers re-validation of the dispatcher and updates the IsValid
    /// and ValidationErrors properties.
    /// </summary>
    private void TriggerRevalidation()
    {
        if (_isDisposed) return;
        IsValid = Validate(out var errors);
        ValidationErrors = errors;
    }

    /// <summary>
    /// Validates the structure of the actions list (nulls, duplicate indices).
    /// Does not perform deep validation of ActionRegistration items.
    /// </summary>
    private bool ValidateActionsStructure(IEnumerable<DispatcherActionEntry>? actionEntriesToValidate, out List<string> errors)
    {
        var validationErrors = new List<string>();
        if (actionEntriesToValidate == null)
        {
            errors = validationErrors;
            return true; // Null is valid (empty)
        }

        var indexDict = new Dictionary<uint, bool>(); // Just need to track presence
        foreach (var entry in actionEntriesToValidate)
        {
            if (entry == null) // Check if the entry itself is null
            {
                validationErrors.Add($"Action entry in the input list is null.");
                continue;
            }
            if (entry.Action == null) // Check if the action within the entry is null
            {
                validationErrors.Add($"Action at index {entry.Index} in the input list is null.");
                // Don't continue here, still need to check index uniqueness
            }
            if (!indexDict.TryAdd(entry.Index, true))
            {
                validationErrors.Add($"Duplicate action index found in input list: {entry.Index}. Indices must be unique.");
            }
        }
        errors = validationErrors;
        return validationErrors.Count == 0;
    }


    /// <summary>
    /// Validates the current state of the DispatcherDefinition according to IValidate.
    /// Checks name validity and validates the contained actions.
    /// </summary>
    /// <param name="errors">An output list populated with validation error messages.</param>
    /// <returns>True if the dispatcher is currently valid, false otherwise.</returns>
    public bool Validate(out IReadOnlyList<string> errors)
    {
        List<string> currentErrors = new();
        // Check name
        if (string.IsNullOrWhiteSpace(Name)) { currentErrors.Add("Name cannot be empty."); }

        // Validate actions structure (duplicates, nulls) first
        if (!ValidateActionsStructure(this._actions, out var structureErrors))
        {
            currentErrors.AddRange(structureErrors);
        }

        // Then, recursively validate items

        foreach (var entry in this._actions) // Use current collection
        {
            // Validate the DispatcherActionEntry itself (which validates the ActionRegistration)
            if (entry is IValidate validatableEntry)
            {
                if (!validatableEntry.Validate(out var entryErrors))
                {
                    // Add errors from the entry
                    currentErrors.AddRange(entryErrors.Select(e => $"Action Index {entry.Index}: {e}"));
                }
            }
        }

        errors = currentErrors;
        return errors.Count == 0;
    }

    // Override ToString for better debugging/display
    public override string ToString() => $"Dispatcher: {Name} ({Actions.Count} actions)";

    // ----- IDisposable Implementation -----

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                if (_actions != null)
                {
                    _actions.CollectionChanged -= OnActionsChanged;
                    UnsubscribeFromActionChanges(_actions); // Unsubscribe from all items
                                                            // Dispose individual entries if they are disposable
                    foreach (var entry in _actions.OfType<IDisposable>())
                    {
                        entry.Dispose();
                    }
                }
            }
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}


/// <summary>
/// Represents an entry in a DispatcherDefinition, associating an index with an ActionRegistration.
/// Implements validation and property change notification.
/// </summary>
public partial class DispatcherActionEntry : ObservableObject, IValidate, IDisposable
{

    #region ----- Observable Properties. -----
    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private IReadOnlyList<string> _validationErrors = new List<string>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))] // Index change might affect parent validation
    [NotifyPropertyChangedFor(nameof(ValidationErrors))]
    private uint _index;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))] // Action change affects validity
    [NotifyPropertyChangedFor(nameof(ValidationErrors))]
    private ActionRegistration _action;

    #endregion

    #region ----- Constructors. -----
    /// <summary>
    /// Private constructor. Use Create factory method.
    /// </summary>
    private DispatcherActionEntry(uint index, ActionRegistration action)
    {
        // Basic assignment, validation happens in Create or externally
        _index = index;
        _action = action ?? throw new ArgumentNullException(nameof(action));

        // Subscribe to the action's property changes to propagate validity
        SubscribeToActionChanges(Action);

        // Perform initial validation
        TriggerRevalidation();
        if (!IsValid)
        {
            // Throw if the initial state (specifically the action) is invalid
            Errors.ThrowArgumentExceptionIfErrors(ValidationErrors);
        }
    }

    /// <summary>
    /// Creates and validates a new DispatcherActionEntry.
    /// </summary>
    /// <param name="index">The index for the action.</param>
    /// <param name="action">The ActionRegistration. Must not be null.</param>
    /// <returns>A validated DispatcherActionEntry.</returns>
    /// <exception cref="ArgumentNullException">Thrown if action is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the action is initially invalid.</exception>
    public static DispatcherActionEntry Create(uint index, ActionRegistration action)
    {
        // Constructor handles null check and initial validation/throwing
        return new DispatcherActionEntry(index, action);
    }

    #endregion

    #region ----- IValidate. -----

    /// <summary>
    /// Validates the DispatcherActionEntry. Primarily checks if the Action is valid.
    /// </summary>
    public bool Validate(out IReadOnlyList<string> errors)
    {
        List<string> currentErrors = new();
        if (Action == null)
        {
            currentErrors.Add("ActionRegistration cannot be null.");
        }
        else if (Action is IValidate validatableAction)
        {
            if (!validatableAction.Validate(out var actionErrors))
            {
                currentErrors.AddRange(actionErrors); // Add errors from the action itself
            }
        }

        errors = currentErrors;
        return currentErrors.Count == 0;
    }

    private void TriggerRevalidation()
    {
        if (_isDisposed) return;
        IsValid = Validate(out var errors);
        ValidationErrors = errors;
    }

    #endregion

    #region ----- Event Handlers. -----
    private void SubscribeToActionChanges(ActionRegistration? action)
    {
        action.Map((action) =>
        {
            action.PropertyChanged += OnActionPropertyChanged;
        });
    }

    private void UnsubscribeFromActionChanges(ActionRegistration? action)
    {
        action.Map((action) =>
        {
            action.PropertyChanged -= OnActionPropertyChanged;
        });
    }

    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If the action's validity changes, re-validate this entry
        if (e.PropertyName == nameof(ActionRegistration.IsValid))
        {
            TriggerRevalidation();
        }
    }

    // ----- Property Change Handler for Action Property -----
    // Called when the _action property itself is changed
    partial void OnActionChanging(ActionRegistration? oldValue, ActionRegistration newValue)
    {
        // ArgumentNullException in setter prevents newValue from being null
        UnsubscribeFromActionChanges(oldValue);
    }
    partial void OnActionChanged(ActionRegistration value)
    {
        // ArgumentNullException in setter prevents value from being null
        SubscribeToActionChanges(value);
        TriggerRevalidation(); // Revalidate when the action instance changes
    }
    #endregion

    public override string ToString() => $"Index={Index}, Action={Action}";

    #region ----- IDisposable. -----

    private bool _isDisposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Unsubscribe from the action
                UnsubscribeFromActionChanges(this.Action);
                // Dispose the action if it's disposable
                (this.Action as IDisposable)?.Dispose();
            }
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}


/// <summary>
/// Definition of an ActionRegistration's parameters.
/// </summary>
public partial class ActionParameter : ObservableObject, IValidate // Implement IValidate
{
    /// <summary>
    /// Gets the definition/metadata for this parameter (name, type, validation rules).
    /// </summary>
    public ActionParameterInfo ParameterInfo { get; } // Made readonly via getter-only

    /// <summary>
    /// Gets the key (name) of the parameter. Read-only.
    /// </summary>
    public string Key => ParameterInfo.Name;

    /// <summary>
    /// Gets the expected data type of the parameter value.
    /// </summary>
    public Type Type => ParameterInfo.Type;

    // Backing field for the observable property Value
    // Notify validation properties when value changes
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(ValidationErrors))]
    private object? _value;

    // Explicit implementation for IValidate properties
    public bool IsValid => Validate(out _);
    public IReadOnlyList<string> ValidationErrors
    {
        get
        {
            Validate(out var errors);
            return errors;
        }
    }

    /// <summary>
    /// Private constructor. Use Create factory method. Validates arguments.
    /// </summary>
    /// <param name="parameterInfo">The metadata defining the parameter.</param>
    /// <param name="value">The initial value for the parameter.</param>
    /// <exception cref="ArgumentNullException">Thrown if parameterInfo is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the initial value is invalid.</exception>
    private ActionParameter(ActionParameterInfo parameterInfo, object? value)
    {
        ParameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
        // Validate initial value using ParameterInfo and throw using Errors util
        if (!ParameterInfo.ValidateValue(value, out var errors))
        {
            Errors.ThrowArgumentExceptionIfErrors(errors, nameof(value));
        }
        _value = value;
    }

    /// <summary>
    /// Factory method for the class. Creates and validates the ActionParameter.
    /// </summary>
    /// <param name="parameterInfo">The metadata defining the parameter.</param>
    /// <param name="value">The value for the parameter.</param>
    /// <returns>A new ActionParameter instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if parameterInfo is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the value is invalid.</exception>
    public static ActionParameter Create(ActionParameterInfo parameterInfo, object? value)
    {
        // Validation happens inside the private constructor
        return new ActionParameter(parameterInfo, value);
    }

    public static ActionParameter CreateFromString(ActionParameterInfo parameterInfo, IReadOnlyList<string> value)
    {
        ArgumentNullException.ThrowIfNull(parameterInfo, nameof(parameterInfo));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        object? convertedValue = null;

        if (parameterInfo.Type == typeof(StringListParameter))
        {
            if (value.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException($"Parameter '{parameterInfo.Name}' contains an empty string.");
            }
            convertedValue = new StringListParameter(value);
        }
        else
        {
            if (value.Count > 1)
            {
                throw new ArgumentException($"Parameter '{parameterInfo.Name}' has more than one value.");
            }
            else if (value.Count == 0 || string.IsNullOrWhiteSpace(value[0]))
            {
                throw new ArgumentException($"Parameter '{parameterInfo.Name}' has a null or empty string value.");
            }
            string stringValue = value[0];
            try
            {
                TypeConverter converter = TypeDescriptor.GetConverter(parameterInfo.Type);
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    convertedValue = converter.ConvertFromInvariantString(stringValue);
                    if (convertedValue == null)
                    {
                        throw new ArgumentException($"TypeConverter returned null for parameter '{parameterInfo.Name}' with value '{stringValue}'.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Cannot find TypeConverter from string for parameter '{parameterInfo.Name}' (Type: {parameterInfo.Type.FullName}).");
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error processing parameter '{parameterInfo.Name}' with value '{stringValue}': {ex.Message}", ex);
            }
        }
        return convertedValue.Map(v => ActionParameter.Create(parameterInfo, v)) ??
               throw new ArgumentException($"Failed to create ActionParameter for '{parameterInfo.Name}' with value '{string.Join(", ", value)}'.");
    }

    // ----- Methods. -----

    /// <summary>
    /// Validates the current value of the parameter against its definition according to IValidate.
    /// </summary>
    /// <param name="errors">A list to populate with validation errors.</param>
    /// <returns>True if the current value is valid, false otherwise.</returns>
    public bool Validate(out IReadOnlyList<string> errors) // Implement IValidate
    {
        // Delegate validation to the ParameterInfo object
        var result = ParameterInfo.ValidateValue(Value, out List<string> currentErrors);
        errors = currentErrors; // Assign the list to the IReadOnlyList out parameter
        return result;
    }

    // ----- Event Handlers. -----

    // Override ToString for better debugging/display
    public override string ToString() => $"{Key}: {Value ?? "<null>"} ({Type.Name})";
}

/// <summary>
/// Represents a registration of a specific action type with its configured parameters.
/// Implements reactive validation based on parameter requirements and contained parameter validity.
/// </summary>
public partial class ActionRegistration : ObservableObject, IValidate, IDisposable // Implement IValidate and IDisposable
{
    #region ----- Observable Properties. -----
    // Backing fields for observable properties
    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private IReadOnlyList<string> _validationErrors = new List<string>();

    /// <summary>
    /// Gets or sets the type of the action. Changes trigger re-validation.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(ValidationErrors))]
    private ActionType _actionType;

    // Store parameters internally as an observable collection
    private readonly ExtendedObservableCollection<ActionParameter> _parameters;

    /// <summary>
    /// Gets the observable collection of configured parameters for this action.
    /// Modifying this collection directly might lead to duplicate parameter keys;
    /// use helper methods if available, or ensure uniqueness externally.
    /// Changes to the collection or its items trigger re-validation.
    /// </summary>
    public ExtendedObservableCollection<ActionParameter> Parameters => _parameters;
    #endregion




    /// <summary>
    /// Private constructor. Use Create methods for instantiation.
    /// Subscribes to parameter changes and performs initial validation.
    /// </summary>
    /// <param name="type">The type of the action.</param>
    /// <param name="parameters">The collection of configured parameters.</param>
    /// <exception cref="ArgumentNullException">Thrown if type or parameters is null.</exception>
    private ActionRegistration(ActionType type, IEnumerable<ActionParameter> parameters)
    {
        _actionType = type ?? throw new ArgumentNullException(nameof(type));
        // Ensure parameters is materialized if it's lazy, handle null
        var parameterList = parameters?.ToList() ?? throw new ArgumentNullException(nameof(parameters));
        _parameters = new ExtendedObservableCollection<ActionParameter>(parameterList);
        // Initial validation of required params happens in Create methods

        // Subscribe to collection changes and changes in contained parameters
        _parameters.CollectionChanged += OnParametersCollectionChanged;
        SubscribeToParameterChanges(_parameters);

        // Perform initial validation
        TriggerRevalidation();
        if (!IsValid)
        {
            // Throw if initial state is invalid (e.g., a required parameter's initial value is bad or duplicates exist)
            Errors.ThrowArgumentExceptionIfErrors(ValidationErrors);
        }
    }

    /// <summary>
    /// Creates and validates an ActionRegistration using pre-constructed ActionParameter objects.
    /// Checks if all required parameters defined by the ActionType are present.
    /// </summary>
    /// <param name="actionType">The type of the action.</param>
    /// <param name="parameters">An enumerable collection of ActionParameter objects.</param>
    /// <returns>A validated ActionRegistration object.</returns>
    /// <exception cref="ArgumentNullException">Thrown if actionType or parameters is null.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails (e.g., missing required parameters).</exception>
    public static ActionRegistration Create(ActionType actionType, IEnumerable<ActionParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(actionType, nameof(actionType));
        ArgumentNullException.ThrowIfNull(parameters, nameof(parameters));

        // Validation in the constructor.
        return new ActionRegistration(actionType, parameters);
    }

    /// <summary>
    /// Creates and validates an ActionRegistration from string-based parameters provided as key-value tuples.
    /// Attempts to convert string values to the types required by the ActionType.
    /// </summary>
    /// <param name="actionType">The type of the action.</param>
    /// <param name="parameters">An enumerable collection of (Key, Value) string tuples representing parameters.</param>
    /// <returns>A validated ActionRegistration object.</returns>
    /// <exception cref="ArgumentNullException">Thrown if actionType or parameters is null.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails (e.g., missing required parameters, type conversion errors, duplicate keys in input).</exception>
    /// <exception cref="FormatException">Can be wrapped in ArgumentException if conversion fails.</exception>
    /// <exception cref="InvalidOperationException">Can be wrapped in ArgumentException if TypeConverter is missing/invalid.</exception>
    public static ActionRegistration Create(ActionType actionType, IEnumerable<(string Key, List<string> Value)> parameters)
    {
        // Attempt to convert string-based parameters to ActionParameter objects (throws exceptions if invalid)
        var parameterObjects = createParametersFromStrings(actionType, parameters);

        // Validation passed, call the private constructor using the successfully derived parameters' values
        return new ActionRegistration(actionType, parameterObjects);
    }

    static IEnumerable<ActionParameter> createParametersFromStrings(ActionType actionType, IEnumerable<(string Key, List<string> Value)> parameters)
    {
        ArgumentNullException.ThrowIfNull(actionType, nameof(actionType));
        ArgumentNullException.ThrowIfNull(parameters, nameof(parameters));

        // Convert IEnumerable<tuple> to a Lookup for efficient access and handling of potential duplicate keys in input
        var paramLookup = parameters.ToLookup(p => p.Key, p => p.Value);

        // Check for duplicate keys in the input enumerable before proceeding
        var duplicateInputKeys = paramLookup.Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateInputKeys.Count > 0)
        {
            throw new ArgumentException($"Duplicate parameter keys found in input: {string.Join(", ", duplicateInputKeys)}", nameof(parameters));
        }

        Dictionary<string, ActionParameter> derivedParameters = new();
        List<string> validationErrors = new(); // Collect all errors

        var expectedParams = actionType.Parameters; // Get parameter definitions from ActionType

        foreach (var expectedParamInfo in expectedParams)
        {
            ActionParameter? parameterObject = _createParameterObject(expectedParamInfo, paramLookup, validationErrors);
            if (parameterObject is ActionParameter p)
            {
                // Use expectedParamInfo.Name for the dictionary key to ensure consistency
                derivedParameters[expectedParamInfo.Name] = parameterObject;
            }
            else if (!expectedParamInfo.IsOptional)
            {
                // If the parameter is required and not found, add an error.
                validationErrors.Add($"Missing required parameter '{expectedParamInfo.Name}'.");
            }
        }

        Errors.ThrowArgumentExceptionIfErrors(validationErrors, nameof(parameters));

        // Validation passed, call the private constructor using the successfully derived parameters' values
        return derivedParameters.Values;
    }

    private static ActionParameter? _createParameterObject(ActionParameterInfo expectedParamInfo, ILookup<string, List<string>> paramLookup, List<string> validationErrors)
    {
        bool gotValue = paramLookup.Contains(expectedParamInfo.Name);
        List<string>? paramValue = gotValue ? paramLookup[expectedParamInfo.Name].First() : null; // Get the first value if key exists
        ActionParameter? convertedValue = null;

        if (gotValue)
        {
            if (paramValue is List<string>)
            {
                try
                {
                    convertedValue = ActionParameter.CreateFromString(expectedParamInfo, paramValue);
                }
                catch (Exception ex)
                {
                    validationErrors.Add(ex.Message);
                }
            }
            else
            {
                validationErrors.Add($"Parameter '{expectedParamInfo.Name}' has an invalid null value.");
            }
        }
        return convertedValue;
    }

    // ----- Event Handlers -----
    private void OnParametersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Handle items being added or removed from the collection
        if (e.OldItems != null)
        {
            UnsubscribeFromParameterChanges(e.OldItems.OfType<ActionParameter>());
        }
        if (e.NewItems != null)
        {
            SubscribeToParameterChanges(e.NewItems.OfType<ActionParameter>());
        }
        TriggerRevalidation(); // Re-validate after any collection change
    }

    private void OnParameterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-validate if the parameter's IsValid state changes or its Key changes (for uniqueness check)
        if (e.PropertyName == nameof(ActionParameter.IsValid) || e.PropertyName == nameof(ActionParameter.Key))
        {
            TriggerRevalidation();
        }
    }

    // ----- Subscription Helpers -----
    private void SubscribeToParameterChanges(IEnumerable<ActionParameter> parameters)
    {
        foreach (var param in parameters)
        {
            if (param != null) param.PropertyChanged += OnParameterPropertyChanged;
        }
    }
    private void UnsubscribeFromParameterChanges(IEnumerable<ActionParameter> parameters)
    {
        foreach (var param in parameters)
        {
            if (param != null) param.PropertyChanged -= OnParameterPropertyChanged;
        }
    }

    // ----- Validation -----
    private void TriggerRevalidation()
    {
        if (_isDisposed) return;
        IsValid = Validate(out var errors);
        ValidationErrors = errors;
    }

    // Implement IValidate for ActionRegistration
    public bool Validate(out IReadOnlyList<string> errors)
    {
        List<string> currentErrors = new();
        var currentParams = _parameters.ToList(); // Snapshot

        // Check required parameters based on ActionType and current collection content
        var expectedParams = ActionType.Parameters;

        // Use currentParams for checking existence
        var currentParamLookup = currentParams.Where(p => p != null).ToLookup(p => p.Key);

        foreach (var expectedParamInfo in expectedParams)
        {
            if (!expectedParamInfo.IsOptional && !currentParamLookup.Contains(expectedParamInfo.Name))
            {
                currentErrors.Add($"Missing required parameter '{expectedParamInfo.Name}'.");
            }
        }

        // Check for duplicate keys within the current collection
        var duplicateKeys = currentParams
            .Where(p => p != null && !string.IsNullOrEmpty(p.Key)) // Consider only non-null params with keys
            .GroupBy(p => p.Key) // Group by key (case-insensitive)
            .Where(g => g.Count() > 1) // Find groups with more than one item
            .Select(g => g.Key); // Select the duplicate key
        foreach (var duplicateKey in duplicateKeys)
        {
            currentErrors.Add($"Duplicate parameter key found: '{duplicateKey}'. Keys must be unique.");
        }

        // Validate existing parameters
        foreach (var param in currentParams)
        {
            if (param == null)
            {
                currentErrors.Add("Parameter item in the collection is null.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(param.Key))
            {
                currentErrors.Add($"Parameter '{param}' has a null or empty key.");
            }

            // Assuming ActionParameter implements IValidate
            if (param is IValidate validatableParam)
            {
                if (!validatableParam.Validate(out var paramErrors))
                {
                    currentErrors.AddRange(paramErrors.Select(e => $"Parameter '{param.Key}': {e}"));
                }
            }
        }
        errors = new List<string>(currentErrors.Distinct()); // Remove potential duplicates
        return errors.Count == 0;
    }

    #region ----- String representation. -----
    // Override ToString for better debugging/display
    public override string ToString() => $"Action: {ActionType.Name} ({ParameterSummary})";

    public string Summary => $"{ActionType.Name}({ParameterSummary})";

    /// <summary>
    /// Gets a summary of the parameters for display purposes.
    /// </summary>
    public string ParameterSummary
    {
        get
        {
            if (_parameters == null || _parameters.Count == 0)
                return string.Empty;
            // Use ActionParameter.Key and ActionParameter.Value
            return string.Join(", ", _parameters.Select(p => $"{p?.Key ?? "<null_key>"}: {p?.Value ?? "<null>"}"));
        }
    }
    #endregion

    #region ----- IDisposable. -----
    private bool _isDisposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Unsubscribe from collection and parameter changes
                if (_parameters != null)
                {
                    _parameters.CollectionChanged -= OnParametersCollectionChanged;
                    UnsubscribeFromParameterChanges(_parameters); // Unsubscribe from all items
                                                                  // Dispose parameters if they are disposable (ActionParameter currently isn't)
                                                                  // foreach(var param in _parameters.OfType<IDisposable>()) { param.Dispose(); }
                }
            }
            _parameters?.Clear();
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}

/// <summary>
/// Represents a command trigger definition, linking a key sequence to a dispatcher.
/// Implements reactive validation.
/// </summary>
public partial class CommandTrigger : ObservableObject, IValidate, IDisposable // Implement IValidate and IDisposable
{
    // Backing fields for observable properties
    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private IReadOnlyList<string> _validationErrors = new List<string>();

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private DispatcherDefinition _dispatcher;

    [ObservableProperty]
    private KeySequence _sequence;

    private bool _isDisposed = false; // To detect redundant calls to Dispose

    /// <summary>
    /// Private constructor. Use the Create factory method.
    /// Initializes properties and performs initial validation.
    /// </summary>
    /// <param name="name">The name of the trigger.</param>
    /// <param name="dispatcher">The dispatcher to trigger.</param>
    /// <param name="sequence">The key sequence.</param>
    /// <exception cref="ArgumentException">Thrown if initial validation fails.</exception>
    private CommandTrigger(string name, DispatcherDefinition dispatcher, KeySequence sequence)
    {
        // Direct assignment; initial validation happens in Create before calling this.
        _name = name;
        _dispatcher = dispatcher;
        _sequence = sequence;

        // Perform initial validation and set observable properties
        TriggerRevalidation();
        if (!IsValid)
        {
            // Throw if the initial state is invalid
            Errors.ThrowArgumentExceptionIfErrors(ValidationErrors);
        }
    }

    /// <summary>
    /// Factory method to create and validate a CommandTrigger.
    /// </summary>
    /// <param name="name">The unique name for the trigger.</param>
    /// <param name="dispatcher">The dispatcher instance to be triggered.</param>
    /// <param name="sequence">The key sequence that activates this trigger (optional, defaults to empty).</param>
    /// <returns>A new, validated CommandTrigger instance.</returns>
    /// <exception cref="ArgumentException">Thrown if name is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown if dispatcher or sequence is null.</exception>
    public static CommandTrigger Create(string name, DispatcherDefinition dispatcher, KeySequence sequence)
    {
        // Perform initial null/whitespace checks before calling constructor
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Command trigger name cannot be null or whitespace.", nameof(name));
        ArgumentNullException.ThrowIfNull(dispatcher, nameof(dispatcher));
        ArgumentNullException.ThrowIfNull(sequence, nameof(sequence));

        // Constructor performs further validation (like checking sequence validity if needed)
        // and throws if the initial state is invalid.
        return new CommandTrigger(name, dispatcher, sequence);
    }

    /// <summary>
    /// Creates an empty, but invalid, CommandTrigger.
    /// </summary>
    public CommandTrigger()
    {
        _name = string.Empty;
        _dispatcher = null!;
        _sequence = null!;
    }

    // ----- Property Change Handlers -----

    partial void OnNameChanged(string value) => TriggerRevalidation();
    partial void OnDispatcherChanged(DispatcherDefinition value) => TriggerRevalidation();
    partial void OnSequenceChanged(KeySequence value) => TriggerRevalidation();


    // ----- Validation -----
    private void TriggerRevalidation()
    {
        if (_isDisposed) return;
        IsValid = Validate(out var errors);
        ValidationErrors = errors;
    }

    // Implement IValidate for CommandTrigger
    public bool Validate(out IReadOnlyList<string> errors)
    {
        List<string> currentErrors = new();
        if (string.IsNullOrWhiteSpace(Name))
        {
            currentErrors.Add("Name cannot be empty.");
        }
        if (Dispatcher == null)
        {
            currentErrors.Add("Dispatcher cannot be null.");
        }
        if (Sequence == null)
        {
            currentErrors.Add("Sequence cannot be null.");
        }
        if (Sequence != null && Sequence.Bindings.Count == 0)
        {
            currentErrors.Add("Sequence cannot be empty.");
        }
        errors = currentErrors;
        return currentErrors.Count == 0;
    }


    // Override ToString for better debugging/display
    public override string ToString() => $"Trigger: '{Name}' Sequence=[{Sequence}] -> Dispatcher='{Dispatcher?.Name ?? "<null>"}'";


    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                // Unsubscribe from any events if CommandTrigger subscribed to them
                // (e.g., if it subscribed to Dispatcher.PropertyChanged).
                // Currently, no subscriptions are made *by* CommandTrigger itself.
            }
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

