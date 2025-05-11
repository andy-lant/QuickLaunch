#nullable enable // Assuming nullable context is enabled for the project

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Logging;

// Use file-scoped namespace
namespace QuickLaunch.Core.KeyEvents;

// --- WpfKeyBinding struct (Unchanged) ---
/// <summary>
/// Represents a specific key and its associated modifiers (Ctrl, Shift, Alt, Win) for WPF.
/// Used as a key for mapping key combinations to commands.
/// Implements IEquatable for use as a dictionary key.
/// </summary>
public readonly struct WpfKeyBinding : IEquatable<WpfKeyBinding>
{
    #region ----- Properties. -----

    public Key Key { get; }
    public ModifierKeys Modifiers { get; }

    public bool HasModifier => Modifiers != ModifierKeys.None;

    private static readonly Key[] _modifierKeys = { Key.LeftShift, Key.RightShift, Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt, Key.LWin, Key.RWin };

    public bool IsModifierKey => _modifierKeys.Contains(Key);

    #endregion

    #region ----- Constructor. -----

    public WpfKeyBinding(Key key, ModifierKeys modifiers)
    {
        Key = key;
        Modifiers = modifiers;
    }

    #endregion

    #region ----- Public Methods. -----

    public bool IsDigit()
    {
        if (Modifiers != ModifierKeys.None) return false;

        var key = Key;
        if (key >= Key.D0 && key <= Key.D9)
        {
            return true;
        }
        else if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    public bool IsSimpleCharacter()
    {
        if (Modifiers != ModifierKeys.None) return false;

        // Use KeyConverter to get the string representation
        string? keyStr = _keyConverter.ConvertToInvariantString(Key);

        // Check if it's a single character and fits the criteria
        if (keyStr?.Length == 1)
        {
            char c = keyStr[0];
            // Visible ASCII range, excluding space, <, >
            // Note: Adjust range if needed, '!' (33) to '~' (126) is common
            return IsSimpleCharacter(c);
        }
        else
        {
            return false;
        }
    }

    internal static bool IsSimpleCharacter(char c)
    {
        return c >= '!' && c <= '~' && c != '<' && c != '>';
    }

    // Helper to get the simple character if IsSimpleCharacter is true
    public char GetSimpleCharacter()
    {
        if (!IsSimpleCharacter())
        {
            throw new InvalidOperationException("Binding is not a simple character.");
        }
        // We know from IsSimpleCharacter that this conversion works and is 1 char
        return (_keyConverter.ConvertToInvariantString(Key)!)[0];
    }
    #endregion


    public override string ToString()
    {
        string keyStr = _keyConverter.ConvertToInvariantString(Key) ?? Key.ToString();
        string modStr = Modifiers != ModifierKeys.None ? (_modifierKeysConverter.ConvertToInvariantString(Modifiers) ?? Modifiers.ToString()) + "+" : "";

        // Handle special cases where ToString() might not be ideal for parsing back
        // Although ConvertTo in the converter handles the canonical string representation
        // This ToString() is useful for debugging.
        // Example: OemPlus might be "OemPlus" by default, but we want "+" visually
        // KeyConverter usually handles common cases well.
        return $"{modStr}{keyStr}";
    }

    public override bool Equals(object? obj) => obj is WpfKeyBinding other && Equals(other);
    public bool Equals(WpfKeyBinding other) => Key == other.Key && Modifiers == other.Modifiers;
    public override int GetHashCode() => HashCode.Combine(Key, Modifiers);
    public static bool operator ==(WpfKeyBinding left, WpfKeyBinding right) => left.Equals(right);
    public static bool operator !=(WpfKeyBinding left, WpfKeyBinding right) => !(left == right);

    // Use KeyConverter and ModifierKeysConverter for a more robust ToString
    private static readonly KeyConverter _keyConverter = new KeyConverter();
    private static readonly ModifierKeysConverter _modifierKeysConverter = new ModifierKeysConverter();


}

[TypeConverter(typeof(KeySequenceConverter))]
public partial class KeySequence : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<WpfKeyBinding> _bindings = new();

    // Keep track if collection changed internally to avoid loops if needed elsewhere.
    private bool _isInternallyChanging = false;

    public KeySequence() : base()
    {
        Bindings.CollectionChanged += Bindings_CollectionChanged;
    }

    public KeySequence(IEnumerable<WpfKeyBinding> bindings) : this()
    {
        UpdateBindings(bindings);
    }

    private void Bindings_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_isInternallyChanging)
        {
            // Notify that the effective "value" of KeySequence has changed
            // if tracking changes matters for databinding or other logic.
            OnPropertyChanged(nameof(Bindings));
        }
    }

    public void UpdateBindings(IEnumerable<WpfKeyBinding> newBindings)
    {
        _isInternallyChanging = true;
        try
        {
            var currentBindings = Bindings.ToList();
            var newBindingsList = newBindings.ToList();

            if (!currentBindings.SequenceEqual(newBindingsList))
            {
                Bindings.Clear();
                foreach (var binding in newBindingsList)
                {
                    Bindings.Add(binding);
                }
                // Property change notification is implicitly handled by [ObservableProperty]
                // when the collection instance itself doesn't change, but its contents do.
                // However, explicitly notifying ensures observers relying on the collection *property* are updated.
                OnPropertyChanged(nameof(Bindings));
            }
        }
        finally
        {
            _isInternallyChanging = false;
        }
    }

    // Expose a readonly list or similar if direct modification of the collection is undesirable
    public IReadOnlyList<WpfKeyBinding> GetBindingsList() => Bindings;

    public override string ToString()
    {
        var repr = new KeySequenceConverter().ConvertTo(this, typeof(string));
        if (repr is string str)
        {
            return str;
        }
        else
        {
            throw new ArgumentException("Can't convert to string!", nameof(KeySequence));
        }
    }
}

public class KeySequenceConverter : TypeConverter
{
    private static readonly KeyConverter _keyConverter = new KeyConverter();
    private static readonly ModifierKeysConverter _modifierKeysConverter = new ModifierKeysConverter();

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    // Convert From String -> KeySequence
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            var bindings = new List<WpfKeyBinding>();
            int i = 0;
            while (i < str.Length)
            {
                // Skip whitespace acting as a separator
                if (char.IsWhiteSpace(str[i]))
                {
                    i++;
                    continue;
                }

                // Case 1: Bracketed expression <...>
                if (str[i] == '<')
                {
                    int closingBracket = str.IndexOf('>', i);
                    if (closingBracket == -1)
                    {
                        throw new FormatException($"Invalid key sequence string: Unterminated '<' starting at index {i}. Input: '{str}'");
                    }

                    string content = str.Substring(i + 1, closingBracket - i - 1);
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        throw new FormatException($"Invalid key sequence string: Empty brackets <> found. Input: '{str}'");
                    }

                    bindings.Add(ParseBracketedBinding(content, str));
                    i = closingBracket + 1;
                }
                // Case 2: Simple character sequence (ASCII, not space, <, >)
                else if (IsSimpleParseChar(str[i]))
                {
                    // Parse consecutive simple characters
                    while (i < str.Length && IsSimpleParseChar(str[i]))
                    {
                        // Convert char to Key
                        // KeyConverter handles basic chars like 'A', '1', etc.
                        var keyObj = _keyConverter.ConvertFromInvariantString(str[i].ToString());
                        if (keyObj is Key key)
                        {
                            bindings.Add(new WpfKeyBinding(key, ModifierKeys.None));
                        }
                        else
                        {
                            // Should not happen if IsSimpleParseChar is correct, but handle defensively
                            throw new FormatException($"Invalid key sequence string: Cannot convert character '{str[i]}' to Key. Input: '{str}'");
                        }
                        i++;
                    }
                }
                // Case 3: Invalid character
                else
                {
                    throw new FormatException($"Invalid key sequence string: Unexpected character '{str[i]}' at index {i}. Input: '{str}'");
                }
            }
            return new KeySequence(bindings);
        }



        return base.ConvertFrom(context, culture, value);
    }

    private static bool IsSimpleParseChar(char c) => WpfKeyBinding.IsSimpleCharacter(c);

    private static WpfKeyBinding ParseBracketedBinding(string content, string originalInputForError)
    {
        string keyString;
        ModifierKeys modifiers = ModifierKeys.None;

        int lastPlus = content.LastIndexOf('+');

        if (lastPlus != -1)
        {
            string modifierString = content.Substring(0, lastPlus);
            keyString = content.Substring(lastPlus + 1);

            // Use ModifierKeysConverter for robust parsing (handles "Ctrl", "Control", "CTRL", "Ctrl+Alt", etc.)
            try
            {
                var modObj = _modifierKeysConverter.ConvertFromInvariantString(modifierString);
                if (modObj is ModifierKeys parsedModifiers)
                {
                    modifiers = parsedModifiers;
                }
                else
                {
                    // This path might be hit if ConvertFromInvariantString returns null or non-ModifierKeys
                    throw new FormatException($"Invalid modifier keys string: '{modifierString}'");
                }
            }
            catch (Exception ex) // Catches exceptions from the converter itself
            {
                throw new FormatException($"Invalid modifier keys string: '{modifierString}'. Input: '{originalInputForError}'", ex);
            }
        }
        else
        {
            keyString = content;
        }

        if (string.IsNullOrWhiteSpace(keyString))
        {
            throw new FormatException($"Invalid key sequence string: Missing key within brackets <{content}>. Input: '{originalInputForError}'");
        }

        // Use KeyConverter for robust key parsing (handles "A", "Space", "F1", "OemPlus", etc.)
        try
        {
            // Handle potential single character case within brackets like <a> which KeyConverter might expect as "A"
            if (keyString.Length == 1 && char.IsLetterOrDigit(keyString[0]))
            {
                keyString = keyString.ToUpperInvariant();
            }
            // Special cases KeyConverter might not handle intuitively from string names
            else if (keyString.Equals("SPACE", StringComparison.OrdinalIgnoreCase)) keyString = "Space";
            else if (keyString.Equals("PLUS", StringComparison.OrdinalIgnoreCase)) keyString = "OemPlus"; // Common representation for '+' key
            else if (keyString.Equals("MINUS", StringComparison.OrdinalIgnoreCase)) keyString = "OemMinus";
            // Add other special mappings as needed (e.g., DEL -> Delete, WIN -> LWin/RWin - choose one or handle specifically)
            else if (keyString.Equals("WIN", StringComparison.OrdinalIgnoreCase)) keyString = "LWin"; // Default to Left Windows key
            else if (keyString.Equals("ALT", StringComparison.OrdinalIgnoreCase)) keyString = "Alt"; // Note: ModifierKeysConverter usually handles Alt, but maybe key 'Alt' is intended? Context matters. Assume it means the key itself if no modifier given.

            var keyObj = _keyConverter.ConvertFromInvariantString(keyString);
            if (keyObj is Key key)
            {
                return new WpfKeyBinding(key, modifiers);
            }
            else
            {
                throw new FormatException($"Invalid key string: '{keyString}'");
            }
        }
        catch (Exception ex) // Catches exceptions from the converter itself
        {
            throw new FormatException($"Invalid key string: '{keyString}'. Input: '{originalInputForError}'", ex);
        }
    }

    // Convert From KeySequence -> String
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        IEnumerable<WpfKeyBinding>? bindings = null;

        if (value is KeySequence keySequence)
        {
            bindings = keySequence.GetBindingsList(); // Use the readonly accessor
        }
        else if (value is IEnumerable<WpfKeyBinding> enumerable)
        {
            bindings = enumerable;
        }

        if (destinationType == typeof(string) && bindings is IEnumerable<WpfKeyBinding>)
        {
            var sb = new StringBuilder();
            // Flag to track if the *last processed* binding was simple.
            // Initialize assuming the "element before the first" was not simple,
            // so a space might be needed before the first complex element,
            // but not before the first simple element.
            bool lastProcessedWasSimple = false;

            foreach (var binding in bindings)
            {
                bool currentIsSimple = binding.IsSimpleCharacter();

                // --- Determine if a space is needed BEFORE appending the current binding ---
                if (sb.Length > 0) // Only add spaces if the string is not empty
                {
                    // Add space if:
                    // 1. Current is complex (always needs space separation from previous)
                    // 2. Current is simple BUT the previous was complex
                    if (!(currentIsSimple && lastProcessedWasSimple))
                    {
                        sb.Append(' ');
                    }
                }

                // --- Append the current binding's representation ---
                if (currentIsSimple)
                {
                    sb.Append(binding.GetSimpleCharacter());
                }
                else // Complex binding (needs brackets)
                {
                    sb.Append('<');

                    // Add modifiers if they exist
                    if (binding.Modifiers != ModifierKeys.None)
                    {
                        string? modStr = _modifierKeysConverter.ConvertToInvariantString(binding.Modifiers);
                        sb.Append(string.IsNullOrEmpty(modStr) ? binding.Modifiers.ToString() : modStr).Append('+');
                    }

                    // Add key - use converter for canonical representation + special names
                    string keyStr;
                    switch (binding.Key)
                    {
                        case Key.Space: keyStr = "SPACE"; break;
                        case Key.OemPlus: keyStr = "PLUS"; break;
                        case Key.OemMinus: keyStr = "MINUS"; break;
                        case Key.Delete: keyStr = "DEL"; break;
                        case Key.LeftCtrl: case Key.RightCtrl: keyStr = "CTRL"; break;
                        case Key.LeftAlt: case Key.RightAlt: keyStr = "ALT"; break;
                        case Key.LeftShift: case Key.RightShift: keyStr = "SHIFT"; break;
                        case Key.LWin: case Key.RWin: keyStr = "WIN"; break;
                        default:
                            keyStr = _keyConverter.ConvertToInvariantString(binding.Key) ?? binding.Key.ToString();
                            if (keyStr.Length == 1 && char.IsLetter(keyStr[0])) keyStr = keyStr.ToUpperInvariant();
                            break;
                    }
                    sb.Append(keyStr);

                    sb.Append('>');
                }

                // --- Update the flag for the next iteration ---
                lastProcessedWasSimple = currentIsSimple;
            }

            return sb.ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}


/// <summary>
/// Event arguments for sequence typed in.
/// </summary>
public class SequenceEventArgs : EventArgs
{
    #region ----- Fields. -----

    private static readonly KeySequenceConverter converter = new();

    #endregion

    #region ----- Properties. -----

    /// <summary>
    /// Key sequence.
    /// </summary>
    public IReadOnlyList<WpfKeyBinding> Sequence { get; }

    public string SequenceDescription { get; }

    public bool IsAborted { get; }

    public bool IsCompleted { get; }

    #endregion

    #region ----- Constructors. -----

    /// <summary>
    /// Constructor.
    /// </summary>
    public SequenceEventArgs(IReadOnlyList<WpfKeyBinding> sequence, bool isCompleted, bool isAborted)
    {
        Sequence = sequence;
        IsAborted = isAborted;
        IsCompleted = isCompleted;
        SequenceDescription = converter.ConvertToInvariantString(sequence) ??
            throw new ArgumentException($"Could not convert sequence to string.", nameof(sequence));
    }

    #endregion
}


// --- SequenceCompleteEventArgs class (New) ---
/// <summary>
/// Provides data for the SequenceComplete event.
/// </summary>
public class SequenceCompleteEventArgs : EventArgs
{
    /// <summary>
    /// The numeric argument entered before or during the sequence, if any.
    /// </summary>
    public uint? NumericArg { get; }

    /// <summary>
    /// Optional data associated with the sequence during registration.
    /// </summary>
    public string? Tag { get; }


    public SequenceCompleteEventArgs(uint? numericArg, string? tag)
    {
        NumericArg = numericArg;
        Tag = tag;
    }
}


/// <summary>
/// Parses sequences of WPF KeyEventArgs (like Vim) including numeric arguments,
/// and raises an event when a registered sequence completes. Uses a Trie structure.
/// Includes checks to prevent prefix clashes during registration.
/// The Escape key cancels any ongoing sequence/numeric input; if no input is active, it fires the EscapePressed event.
/// Escape cannot be part of registered sequences.
/// </summary>
public class WpfSequenceKeyCommandParser
{
    #region ----- Internal Structures. -----
    /// <summary>
    /// Represents a node in the command sequence Trie.
    /// </summary>
    private class CommandTrieNode
    {
        private readonly CommandTrieNode root;
        public CommandTrieNode(bool _ /* to prevent empty constructor */)
        {
            root = this;
        }

        public CommandTrieNode(CommandTrieNode root)
        {
            this.root = root;
        }

        public Dictionary<WpfKeyBinding, CommandTrieNode> Children { get; } = new();

        /// <summary>
        /// Optional data associated with this sequence endpoint.
        /// </summary>
        public string? Tag { get; set; }

        /// <summary>
        /// Flag indicating if this node marks the end of a registered sequence.
        /// </summary>
        public bool IsRegisteredSequenceEnd { get; set; }

        /// <summary>
        /// Synonym to IsRegisteredSequenceEnd.
        /// </summary>
        public bool IsTerminal => IsRegisteredSequenceEnd;

        /// <summary>
        /// For debugging purposes: is root or not.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{typeof(CommandTrieNode).Name}{(this == root ? "<ROOT>" : "")}";
        }
    }
    #endregion

    #region ----- Fields. -----

    /// <summary>
    /// Root node of trie.
    /// </summary>
    private readonly CommandTrieNode _commandTrieRoot = new(true);

    /// <summary>
    /// Pointer to current node in trie.
    /// </summary>
    private CommandTrieNode _currentSequenceNode;

    /// <summary>
    /// Currently parsed numeric argument.
    /// </summary>
    private uint? _currentNumericArg = null;

    /// <summary>
    /// Track entered sequence, including "mistakes" for event SequenceProgress.
    /// </summary>
    private readonly List<WpfKeyBinding> _rawSequence = new();

    /// <summary>
    /// Indicates sequence parsing has started.
    /// </summary>
    public bool IsSequenceStarted => _currentSequenceNode != _commandTrieRoot || _currentNumericArg.HasValue;

    #endregion

    #region ----- Events. -----

    /// <summary>
    /// Event fired when the Escape key is pressed and no sequence or numeric input was active.
    /// </summary>
    public event EventHandler? EscapePressed;

    /// <summary>
    /// Event fired when a registered key sequence is successfully completed.
    /// </summary>
    public event EventHandler<SequenceCompleteEventArgs>? SequenceComplete;

    /// <summary>
    /// Event fired when key sequence is updated (new key, abort, ...).
    /// </summary>
    public event EventHandler<SequenceEventArgs>? SequenceProgress;

    #endregion

    #region ----- Constructor. -----

    /// <summary>
    /// Initializes a new instance of the WpfSequenceKeyCommandParser class.
    /// </summary>
    public WpfSequenceKeyCommandParser()
    {
        _currentSequenceNode = _commandTrieRoot;
    }

    #endregion

    #region ----- Register/Unregister Sequences. -----

    /// <summary>
    /// Registers a sequence of key bindings. When this sequence is detected,
    /// the SequenceComplete event will be raised.
    /// Checks for prefix clashes and disallows Key.Escape.
    /// </summary>
    /// <param name="sequence">An enumerable of WpfKeyBinding representing the key sequence.</param>
    /// <param name="tag">Optional data to associate with this sequence, passed in the event args.</param>
    /// <exception cref="ArgumentNullException">Thrown if sequence is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the sequence is empty or contains Key.Escape.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the new sequence clashes with an existing one.</exception>
    public void RegisterSequence(IEnumerable<WpfKeyBinding> sequence, string? tag = null) // Changed command param to tag
    {
        ArgumentNullException.ThrowIfNull(sequence);
        // Tag can be null

        var sequenceList = sequence as List<WpfKeyBinding> ?? sequence.ToList();
        if (!sequenceList.Any()) throw new ArgumentException("Command sequence cannot be empty.", nameof(sequence));

        if (sequenceList.Any(binding => binding.Key == Key.Escape))
        {
            throw new ArgumentException("Key.Escape cannot be part of a registered command sequence.", nameof(sequence));
        }

        CommandTrieNode currentNode = _commandTrieRoot;
        string sequenceStr = "";
        List<WpfKeyBinding> traversedBindings = new List<WpfKeyBinding>();

        foreach (var binding in sequenceList)
        {
            sequenceStr += binding + " ";
            traversedBindings.Add(binding);

            // Clash Check 1: Existing shorter sequence is prefix of new one
            if (currentNode.IsRegisteredSequenceEnd) // Use new flag
            {
                string existingPrefixStr = string.Join(" ", traversedBindings.Take(traversedBindings.Count - 1));
                throw new InvalidOperationException($"Cannot register sequence '{sequenceStr.Trim()}'. An existing sequence is registered for the prefix '{existingPrefixStr}'.");
            }

            if (!currentNode.Children.TryGetValue(binding, out CommandTrieNode? nextNode))
            {
                nextNode = new CommandTrieNode(_commandTrieRoot);
                currentNode.Children.Add(binding, nextNode);
            }
            currentNode = nextNode;
        }

        // Clash Check 2: New sequence is prefix of existing longer one
        if (currentNode.Children.Any())
        {
            throw new InvalidOperationException($"Cannot register sequence '{sequenceStr.Trim()}'. It is a prefix of one or more existing longer sequences.");
        }

        // Mark the end node and assign the tag
        if (currentNode.IsRegisteredSequenceEnd)
        {
            Log.Logger?.LogDebug($"WARNING: Overwriting registration tag for identical sequence: {sequenceStr.Trim()}");
        }
        currentNode.IsRegisteredSequenceEnd = true; // Mark as end
        currentNode.Tag = tag; // Assign tag
        Log.Logger?.LogDebug($"Registered sequence: {sequenceStr.Trim()} {(tag != null ? $"with Tag: {tag}" : "")}");
    }

    /// <summary>
    /// Helper to register sequences using Key and ModifierKeys arrays.
    /// Checks for prefix clashes and disallows Key.Escape.
    /// </summary>
    /// <param name="keys">Array of Keys in the sequence.</param>
    /// <param name="modifiers">Array of ModifierKeys corresponding to each key.</param>
    /// <param name="tag">Optional data to associate with this sequence.</param>
    public void RegisterSequence(Key[] keys, ModifierKeys[] modifiers, string? tag = null) // Changed command param to tag
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(modifiers);

        if (keys.Length != modifiers.Length) throw new ArgumentException("Keys and Modifiers arrays must have the same length.");
        if (keys.Length == 0) throw new ArgumentException("Command sequence cannot be empty.");

        if (keys.Any(key => key == Key.Escape))
        {
            throw new ArgumentException("Key.Escape cannot be part of a registered command sequence.", nameof(keys));
        }

        var sequence = keys.Zip(modifiers, (key, mod) => new WpfKeyBinding(key, mod));
        RegisterSequence(sequence, tag); // Call base overload
    }

    /// <summary>
    /// Registers a sequence using a string representation (e.g., "gg", "hello").
    /// Each character is treated as a key press with no modifiers.
    /// Checks for prefix clashes and disallows Escape.
    /// </summary>
    /// <param name="sequenceString">The sequence string to parse.</param>
    /// <param name="tag">Optional data to associate with this sequence.</param>
    public void RegisterSequence(string sequenceString, string? tag = null) // Changed command param to tag
    {
        ArgumentNullException.ThrowIfNull(sequenceString);

        if (string.IsNullOrWhiteSpace(sequenceString)) throw new ArgumentException("Sequence string cannot be empty or whitespace.", nameof(sequenceString));

        var parsedSequence = new List<WpfKeyBinding>();

        foreach (char c in sequenceString)
        {
            string keyName = c.ToString();
            if (Enum.TryParse(keyName, true, out Key currentKey))
            {
                if (currentKey == Key.Escape)
                {
                    throw new ArgumentException("Key.Escape cannot be part of a registered command sequence string.", nameof(sequenceString));
                }
                parsedSequence.Add(new WpfKeyBinding(currentKey, ModifierKeys.None));
            }
            else
            {
                throw new FormatException($"Invalid character '{c}' in sequence string '{sequenceString}'. Cannot map directly to a Key enum value.");
            }
        }

        if (!parsedSequence.Any())
        {
            throw new FormatException($"Sequence string '{sequenceString}' did not result in any valid key bindings.");
        }

        RegisterSequence(parsedSequence, tag); // Call base overload
    }


    /// <summary>
    /// Removes a registered sequence.
    /// </summary>
    /// <param name="sequence">The sequence to remove.</param>
    /// <returns>True if the sequence was found and unregistered; otherwise, false.</returns>
    public bool UnregisterSequence(IEnumerable<WpfKeyBinding> sequence) // Renamed method
    {
        if (sequence == null || !sequence.Any()) return false;

        CommandTrieNode currentNode = _commandTrieRoot;

        // Find the node corresponding to the sequence end
        foreach (var binding in sequence)
        {
            if (!currentNode.Children.TryGetValue(binding, out CommandTrieNode? nextNode))
            {
                return false; // Sequence doesn't exist
            }
            currentNode = nextNode;
        }

        // Check if this node was marked as a sequence end
        if (currentNode.IsRegisteredSequenceEnd)
        {
            currentNode.IsRegisteredSequenceEnd = false; // Unmark
            currentNode.Tag = null; // Clear tag
            Log.Logger?.LogDebug($"Unregistered sequence ending at: {string.Join(" ", sequence)}");
            return true;
        }

        return false; // Sequence exists but wasn't registered as an endpoint
    }

    #endregion

    #region ----- Parsing Sequences. -----

    /// <summary>
    /// Processes incoming WPF KeyEventArgs. Handles Escape, digits, and sequence keys.
    /// Raises the SequenceComplete event when a registered sequence finishes.
    /// </summary>
    /// <param name="e">The KeyEventArgs from the KeyDown event.</param>
    /// <returns>True if the key event was handled; otherwise, false.</returns>
    public bool ProcessKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Form KeyBinding including Key and active modifiers.
        WpfKeyBinding newBinding;
        {
            Key actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
            ModifierKeys currentModifiers = e.KeyboardDevice.Modifiers;
            newBinding = new(actualKey, currentModifiers);
        }


        bool isHandled = true;
        // --- Handle Escape Key ---
        if (newBinding.Key == Key.Escape)
        {
            isHandled = HandleEscape(newBinding);
        }

        // --- Handle Digit Input ---
        else if (newBinding.Modifiers == ModifierKeys.None && TryGetDigitValue(newBinding.Key, out int digitValue))
        {
            isHandled = HandleNumeric(newBinding, digitValue);
        }

        // --- Process Non-Digit Keys ---
        else
        {
            // Handle pressing of modifier keys themselves => reset the modifiers on the binding.
            if (newBinding.IsModifierKey)
            {
                newBinding = new WpfKeyBinding(newBinding.Key, ModifierKeys.None);
                if (!_commandTrieRoot.Children.ContainsKey(newBinding))
                {
                    // Ignore key.
                    return false;
                }
            }

            if (!TryNext(newBinding))
            {
                bool wasInSequence = _currentSequenceNode != _commandTrieRoot;

                if (!wasInSequence)
                {
                    _rawSequence.Add(newBinding);
                    ResetSequenceState(); // Reset sequence state (clears number arg too)
                }
                else
                {
                    // --- Sequence Broken, New Sequence Starts ---
                    ResetSequenceState();
                    if (!TryNext(newBinding))
                    {
                        _rawSequence.Add(newBinding);
                        ResetSequenceState();
                    }
                }
            }
        }
        return isHandled;
    }

    private bool TryNext(WpfKeyBinding keyBinding)
    {
        if (_currentSequenceNode.Children.TryGetValue(keyBinding, out CommandTrieNode? nextNode))
        {
            // --- Sequence Continues ---
            _currentSequenceNode = nextNode;
            _rawSequence.Add(keyBinding);

            // Check if this new node completes a registered sequence
            if (_currentSequenceNode.IsRegisteredSequenceEnd)
            {
                OnSequenceProgress(new SequenceEventArgs(_rawSequence, true, false));
                OnSequenceComplete(new SequenceCompleteEventArgs(
                    _currentNumericArg,
                    _currentSequenceNode.Tag));
                ResetSequenceState(supressEvent: true);
            }
            else
            {
                OnSequenceProgress(new SequenceEventArgs(_rawSequence, false, false));
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool HandleEscape(WpfKeyBinding _)
    {
        if (IsSequenceStarted)
        {
            Log.Logger?.LogDebug("Escape key pressed. Cancelling current sequence/numeric input.");
            ResetSequenceState();
        }
        else
        {
            Log.Logger?.LogDebug("Escape key pressed with no active input. Firing EscapePressed event.");
            OnEscapePressed();
        }
        return true;
    }

    private bool HandleNumeric(WpfKeyBinding newBinding, int digitValue)
    {
        Key k = Key.D0 + digitValue;
        newBinding = new WpfKeyBinding(k, newBinding.Modifiers);
        if ((_currentNumericArg ?? 0) > int.MaxValue / 10 ||
            (_currentNumericArg ?? 0) == int.MaxValue / 10 && digitValue > int.MaxValue % 10)
        {
            Log.Logger?.LogDebug($"Numeric argument overflow prevented. Resetting.");
            _rawSequence.Add(newBinding);
            ResetSequenceState();
        }
        else
        {
            if (_currentSequenceNode != _commandTrieRoot)
            {
                Log.Logger?.LogDebug($"Sequence broken by numeric input. Resetting sequence part.");
                ResetSequenceState();
            }

            _currentNumericArg = (uint)((_currentNumericArg ?? 0) * 10 + digitValue);
            Log.Logger?.LogDebug($"Numeric argument updated: {_currentNumericArg}");
            _rawSequence.Add(newBinding);
            OnSequenceProgress(new SequenceEventArgs(_rawSequence, false, false));
        }
        return true;
    }

    public void ResetSequence()
    {
        ResetSequenceState();
    }


    /// <summary>
    /// Resets the sequence state, including the numeric argument.
    /// </summary>
    private void ResetSequenceState(bool supressEvent = false)
    {
        if (_rawSequence.Count > 0 && !supressEvent)
        {
            OnSequenceProgress(new SequenceEventArgs(_rawSequence, false, true));
        }
        _rawSequence.Clear();
        _currentSequenceNode = _commandTrieRoot;
        _currentNumericArg = null;
    }

    /// <summary>
    /// Checks if a key is a digit key (0-9) on the main keyboard or numpad.
    /// </summary>
    private bool TryGetDigitValue(Key key, out int value)
    {
        if (key >= Key.D0 && key <= Key.D9) { value = key - Key.D0; return true; }
        if (key >= Key.NumPad0 && key <= Key.NumPad9) { value = key - Key.NumPad0; return true; }
        value = -1; return false;
    }

    /// <summary>
    /// Clears all registered sequences and resets state.
    /// </summary>
    public void ClearBindings()
    {
        ResetSequenceState();
        // Need a way to clear the Trie flags/tags recursively or rebuild the root
        ClearTrieNode(_commandTrieRoot); // Add helper or rebuild
        _commandTrieRoot.Children.Clear(); // Simpler: just clear children if full reset ok
        Log.Logger?.LogDebug($"Cleared all sequence bindings.");
    }

    /// <summary>
    /// Helper to recursively clear registration flags and tags from the Trie.
    /// (Alternative to just clearing root children in ClearBindings)
    /// </summary>
    private void ClearTrieNode(CommandTrieNode node)
    {
        node.IsRegisteredSequenceEnd = false;
        node.Tag = null;
        foreach (var childNode in node.Children.Values)
        {
            ClearTrieNode(childNode);
        }
        node.Children.Clear();
    }

    #endregion

    #region ----- Event Handling. -----

    /// <summary>
    /// Raises the EscapePressed event.
    /// </summary>
    protected virtual void OnEscapePressed()
    {
        try
        {
            EscapePressed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            // Log errors from event subscribers
            Log.Logger?.LogError(ex, "ERROR in EscapePressed event subscriber.");
        }
    }

    /// <summary>
    /// Raises the SequenceComplete event.
    /// </summary>
    protected virtual void OnSequenceComplete(SequenceCompleteEventArgs e)
    {
        // Use a temporary variable to be thread-safe
        EventHandler<SequenceCompleteEventArgs>? handler = SequenceComplete;
        try
        {
            handler?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            // Log errors from event subscribers
            Log.Logger?.LogError(ex, "ERROR in SequenceComplete event subscriber.");
        }
    }

    /// <summary>
    /// Raises the SequenceEvent event.
    /// </summary>
    protected virtual void OnSequenceProgress(SequenceEventArgs e)
    {
        try
        {
            SequenceProgress?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            // Log errors from event subscribers
            Log.Logger?.LogDebug(ex, "ERROR in SequenceProgress event subscriber.");
        }
    }

    #endregion
}
