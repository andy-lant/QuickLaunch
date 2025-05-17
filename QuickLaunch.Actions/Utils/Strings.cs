using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QuickLaunch.Core.Utils.QuotedString;

/// <summary>
/// Defintion for an escaped character.
/// </summary>
/// <param name="character">The character to be escaped.</param>
/// <param name="escapeValue">The escape for the character (after the backslash)</param>
/// <param name="allowUnescaped">Whether to allow the unescaped character when parsing an escaped string.</param>
public record Escape(char Character, char? escapeValue = null, bool AllowUnescaped = false)
{
    public char EscapeValue { get; init; } = (char)(escapeValue is not null ? escapeValue : Character);
}

/// <summary>
/// Represents a string value that is typically stored or represented
/// in a quoted and escaped format when serialized as a string.
/// This class holds the actual, unescaped value.
/// </summary>
[TypeConverter(typeof(MaybeQuotedStringTypeConverter))]
public class MaybeQuotedString : IEquatable<MaybeQuotedString>
{
    /// <summary>
    /// Gets the actual, unescaped string value. Can be null.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaybeQuotedString"/> class.
    /// </summary>
    /// <param name="value">The actual, unescaped string value.</param>
    public MaybeQuotedString(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Returns the raw string value. For the quoted/escaped representation,
    /// use the TypeConverter.
    /// </summary>
    /// <returns>The raw string value, or an empty string if null.</returns>
    public override string ToString()
    {
        return Value.ToString();
    }

    // --- Equality Members ---

    public override bool Equals(object? obj)
    {
        return Equals(obj as MaybeQuotedString);
    }

    public bool Equals(MaybeQuotedString? other)
    {
        if (other is null) return false;
        // Two QuotedString instances are equal if their underlying Value is equal.
        return StringComparer.Ordinal.Equals(this.Value, other.Value);
    }

    public bool Equals(string? other)
    {
        if (other is null) return false;
        // Compare the raw string value with another string.
        return StringComparer.Ordinal.Equals(this.Value, other);
    }

    public bool Equals(QuotedString? other)
    {
        if (other is null) return false;
        // Compare the raw string value with a QuotedString.
        return StringComparer.Ordinal.Equals(this.Value, other.Value);
    }

    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }

    public static bool operator ==(MaybeQuotedString? left, MaybeQuotedString? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(MaybeQuotedString? left, MaybeQuotedString? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Implicitly converts a string to a MaybeQuotedString.
    /// </summary>
    public static implicit operator MaybeQuotedString?(string? value) => value is null ? null : new(value);

    /// <summary>
    /// Implicitly converts a QuotedString to a MaybeQuotedString.
    /// </summary>
    public static implicit operator MaybeQuotedString?(QuotedString? value) => value is null ? null : new(value.Value);

    /// <summary>
    /// Implicitly converts a MaybeQuotedString to string.
    /// </summary>
    public static implicit operator string?(MaybeQuotedString? quotedString) => quotedString?.Value;

    /// <summary>
    /// Implicitly converts a MaybeQuotedString to QuotedString.
    /// </summary>
    public static implicit operator QuotedString?(MaybeQuotedString? quotedString)
        => quotedString is null ? null : new QuotedString(quotedString.Value);

}

/// <summary>
/// Represents a string value that is typically stored or represented
/// in a quoted and escaped format when serialized as a string.
/// This class holds the actual, unescaped value.
/// </summary>
[TypeConverter(typeof(QuotedStringTypeConverter))]
public class QuotedString : IEquatable<QuotedString>
{
    /// <summary>
    /// Gets the actual, unescaped string value. Can be null.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuotedString"/> class.
    /// </summary>
    /// <param name="value">The actual, unescaped string value.</param>
    public QuotedString(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Returns the raw string value. For the quoted/escaped representation,
    /// use the TypeConverter.
    /// </summary>
    /// <returns>The raw string value, or an empty string if null.</returns>
    public override string ToString()
    {
        return Value.ToString();
    }

    // --- Equality Members ---

    public override bool Equals(object? obj)
    {
        return Equals(obj as QuotedString);
    }

    public bool Equals(QuotedString? other)
    {
        if (other is null) return false;
        // Two QuotedString instances are equal if their underlying Value is equal.
        return StringComparer.Ordinal.Equals(this.Value, other.Value);
    }

    public bool Equals(MaybeQuotedString? other)
    {
        if (other is null) return false;
        // Compare the raw string value with a MaybeQuotedString.
        return StringComparer.Ordinal.Equals(this.Value, other.Value);
    }

    public bool Equals(string? other)
    {
        if (other is null) return false;
        // Compare the raw string value with another string.
        return StringComparer.Ordinal.Equals(this.Value, other);
    }

    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }

    public static bool operator ==(QuotedString? left, QuotedString? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(QuotedString? left, QuotedString? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Implicitly converts a string to a QuotedString.
    /// </summary>
    public static implicit operator QuotedString?(string? value) => value is null ? null : new(value);

    /// <summary>
    /// Implicitly converts a MaybeQuotedString to a QuotedString.
    /// </summary>
    public static implicit operator QuotedString?(MaybeQuotedString? value) => value is null ? null : new(value.Value!);

    /// <summary>
    /// Implicitly converts a QuotedString to a string.
    /// </summary>
    public static implicit operator string?(QuotedString? quotedString) => quotedString?.Value;

    /// <summary>
    /// Implicitly converts a QuotedString to a MaybeQuotedString.
    /// </summary>
    public static implicit operator MaybeQuotedString?(QuotedString? quotedString) => quotedString is null ? null : new(quotedString.Value);
}


/// <summary>
/// TypeConverter for converting between a standard string
/// and a <see cref="QuotedString"/> object (which holds the raw value).
/// Handles adding/removing surrounding quotes and processing escapes (\", \\).
/// </summary>
public class QuotedStringTypeConverter : TypeConverter
{
    #region ----- Properties. -----

    // Backing field.
    private readonly ImmutableDictionary<char, Escape> _escapedChars;

    /// <summary>
    /// List of characters that should/must be escaped.
    /// </summary>
    public IImmutableDictionary<char, Escape> EscapedChars => _escapedChars;

    // Backing field.
    private readonly ImmutableDictionary<char, Escape> _unescapedChars;

    /// <summary>
    /// List of characters part of an escape sequence.
    /// </summary>
    public IImmutableDictionary<char, Escape> UnescapedChars => _unescapedChars;

    #endregion

    #region ----- Constructors. -----
    public QuotedStringTypeConverter() : this(Array.Empty<Escape>())
    {
        // Default escaped chars.
    }

    public QuotedStringTypeConverter(IEnumerable<Escape> escapedChars)
    {
        // Default escaped chars + user given.
        List<Escape> escapes = new(new[] { new Escape('"'), new Escape('\\') });
        escapes.AddRange(escapedChars);
        _escapedChars = escapes.ToImmutableDictionary((c) => c.Character);
        _unescapedChars = escapes.ToImmutableDictionary((c) => c.EscapeValue);
    }

    #endregion

    #region ----- Public methods. -----

    /// <summary>
    /// Whether a given character should/must be escaped.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public bool ShouldEscape(char c)
    {
        return EscapedChars.Keys.Contains(c);
    }

    /// <summary>
    /// Whether a given character is part of escape sequence.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public bool IsEscape(char c)
    {
        return UnescapedChars.Keys.Contains(c);
    }

    #endregion

    #region ----- TypeConverter. -----

    // --- CanConvert ---

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        // Can create a QuotedString FROM a string.
        return sourceType == typeof(string) ||
            sourceType == typeof(MaybeQuotedString) ||
            base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        // Can convert a QuotedString TO a string.
        return destinationType == typeof(string) ||
            destinationType == typeof(MaybeQuotedString) ||
            base.CanConvertTo(context, destinationType);
    }

    // --- ConvertFrom (string -> QuotedString) ---

    /// <summary>
    /// Converts a string representation (potentially quoted and escaped)
    /// into a QuotedString object containing the raw value.
    /// </summary>
    /// <exception cref="System.FormatException">Thrown if the input string has invalid quoting or escaping.</exception>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string inputString)
        {
            // Handle null or empty string input -> QuotedString with null or empty value
            if (inputString == null) return null;
            if (inputString == string.Empty) return new QuotedString(string.Empty);

            if (!TryParsePrefix(inputString, out QuotedString? quotedString, out int consumedLength) ||
                quotedString == null || consumedLength != inputString.Length)
            {
                throw new FormatException($"String is not properly quoted at position {consumedLength}: '{inputString}'.");
            }

            return quotedString;
        }
        else if (value is MaybeQuotedString maybeQuotedString)
        {
            // If the input is a MaybeQuotedString, return its Value directly
            return new QuotedString(maybeQuotedString.Value);
        }
        else
        {
            return base.ConvertFrom(context, culture, value);
        }
    }

    // --- ConvertTo (QuotedString -> string) ---

    /// <summary>
    /// Converts a QuotedString object into its quoted and escaped string representation.
    /// </summary>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {

        if (destinationType == typeof(string))
        {
            if (value is QuotedString quotedString)
            {
                if (quotedString.Value == null)
                {
                    return null;
                }

                // Escape the raw value and enclose in quotes
                string escapedValue = EscapeString(quotedString.Value);
                return $"\"{escapedValue}\"";
            }
            else if (value == null)
            {
                return null;
            }
            else
            {
                throw new ArgumentException($"Invalid value type {value.GetType().Name}, expected {nameof(QuotedString)}.", nameof(value));
            }
        }
        else if (destinationType == typeof(MaybeQuotedString))
        {
            if (value is QuotedString quotedString)
            {
                if (quotedString.Value == null)
                {
                    return null;
                }
                else
                {
                    return new MaybeQuotedString(quotedString.Value);
                }
            }
            else if (value == null)
            {
                return null;
            }
            else
            {
                throw new ArgumentException($"Invalid value type {value.GetType().Name}, expected {nameof(QuotedString)}.", nameof(value));
            }
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    #endregion

    #region --- Helper Methods for Escaping/Unescaping ---

    /// <summary>
    /// Escapes backslashes and double quotes within a string
    /// for representation inside a quoted string literal.
    /// </summary>
    internal string EscapeString(string rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(rawValue.Length + 5); // Estimate capacity
        foreach (char c in rawValue)
        {
            if (ShouldEscape(c))
            {
                sb.Append('\\');
                sb.Append(EscapedChars[c].EscapeValue);
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Attempts to parse a QuotedString from the beginning of the specified text.
    /// A valid QuotedString representation must start and end with '"' and handle '\' escapes
    /// for '"' and '\' characters internally.
    /// </summary>
    /// <param name="text">The input string to parse.</param>
    /// <param name="startIndex">The starting index within the text to begin parsing.</param>
    /// <param name="parsedValue">When this method returns true, contains the parsed QuotedString object; otherwise, null.</param>
    /// <param name="consumedLength">When this method returns true, contains the number of characters consumed from the text (including quotes).
    /// When this method returns false, indicates the number of characters examined before encountering a definitive format error or the end of the string prematurely.</param>
    /// <returns>True if a valid QuotedString representation was successfully parsed; otherwise, false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if text is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if startIndex is out of range.</exception>
    public bool TryParsePrefix(string text, int startIndex, out QuotedString? parsedValue, out int consumedLength, int? endIndex = null)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));

        if (endIndex == null)
        {
            endIndex = text.Length;
        }
        else if (endIndex < 0 || endIndex > text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(endIndex));
        }

        if (startIndex < 0 || startIndex > endIndex) // Allow startIndex == endIndex (results in immediate failure)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be within the bounds of the text.");

        parsedValue = null;
        consumedLength = 0;

        // Check if there's enough length for at least quotes ""
        if (startIndex >= endIndex || text[startIndex] != '"')
        {
            // Not starting with a quote, definitely not a valid QuotedString prefix
            return false;
        }

        var sb = new StringBuilder();
        bool isEscaped = false;
        int currentIndex = startIndex + 1; // Start parsing after the opening quote

        while (currentIndex < endIndex)
        {
            char c = text[currentIndex];

            if (isEscaped)
            {
                // Previous char was '\'
                if (IsEscape(c))
                {
                    sb.Append(UnescapedChars[c].Character);
                }
                else
                {
                    sb.Append('\\');
                    sb.Append(c);
                }
                isEscaped = false;
            }
            else // Not escaped
            {
                switch (c)
                {
                    case '\\':
                        // Start escape sequence
                        isEscaped = true;
                        break;
                    case '"':
                        // Found the closing quote
                        consumedLength = currentIndex + 1 - startIndex; // Consumed up to and including the closing quote
                        parsedValue = new QuotedString(sb.ToString());
                        return true; // Success!
                    default:
                        if (ShouldEscape(c) && !EscapedChars[c].AllowUnescaped)
                        {
                            throw new FormatException($"Unexpected unescaped character '{c}' at position {currentIndex}: '{text}'.");
                        }
                        // Regular character
                        sb.Append(c);
                        break;
                }
            }
            currentIndex++;
        }

        // If loop finishes, we reached the end of the string without finding a closing quote
        // or ending on a valid escape.
        // consumedLength should reflect the entire processed portion.
        consumedLength = currentIndex - startIndex;

        // Also check for dangling escape at the very end
        // (Though the loop structure implies isEscaped would be true if the *last* char was '\')
        // The loop exit condition (currentIndex == text.Length) means the last processed index was text.Length - 1.
        // If text[text.Length - 1] was '\', isEscaped would be true here.
        if (isEscaped)
        {
            // String ended with an unescaped backslash - format error
            return false;
        }

        // Reached end of string without closing quote - format error
        return false;
    }

    /// <summary>
    /// Attempts to parse a QuotedString from the beginning of the specified text (starting at index 0).
    /// </summary>
    /// <param name="text">The input string to parse.</param>
    /// <param name="parsedValue">When this method returns true, contains the parsed QuotedString object; otherwise, null.</param>
    /// <param name="consumedLength">The number of characters consumed or examined.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool TryParsePrefix(string text, out QuotedString? parsedValue, out int consumedLength)
    {
        return TryParsePrefix(text, 0, out parsedValue, out consumedLength);
    }

    #endregion
}

/// <summary>
/// TypeConverter for converting between a standard string (potentially quoted and escaped)
/// and a <see cref="MaybeQuotedString"/> object (which holds the raw value).
/// Handles adding/removing surrounding quotes and processing escapes (\", \\).
/// </summary>
public class MaybeQuotedStringTypeConverter : TypeConverter
{
    #region ----- Properties. -----

    // Associated QuotedStringTypeConverter.
    private readonly QuotedStringTypeConverter _qsConverter;

    /// <summary>
    /// List of characters that should/must be escaped.
    /// </summary>
    public IReadOnlyDictionary<char, Escape> EscapedChars => _qsConverter.EscapedChars;

    public IReadOnlyDictionary<char, Escape> UnescapedChars => _qsConverter.UnescapedChars;

    #endregion

    #region ----- Constructors. -----
    public MaybeQuotedStringTypeConverter()
    {
        _qsConverter = new();
    }

    public MaybeQuotedStringTypeConverter(IEnumerable<Escape> escapedChars)
    {
        _qsConverter = new(escapedChars);
    }

    #endregion

    #region ----- Public methods. -----

    /// <summary>
    /// Whether a given character should/must be escaped.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public bool ShouldEscape(char c) => _qsConverter.ShouldEscape(c);

    /// <summary>
    /// Whether a given character is part of escape sequence.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public bool IsEscape(char c) => _qsConverter.IsEscape(c);

    #endregion

    #region ----- TypeConverter. -----


    // --- CanConvert ---

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        // Can create a QuotedString FROM a string.
        return sourceType == typeof(string) || sourceType == typeof(QuotedString) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        // Can convert a QuotedString TO a string.
        return destinationType == typeof(string) || destinationType == typeof(QuotedString) || base.CanConvertTo(context, destinationType);
    }

    // --- ConvertFrom (string || QuotedString -> MaybeQuotedString) ---

    /// <summary>
    /// Converts a string representation into a MaybeQuotedString.
    /// - If the string is quoted ("..."), it's unescaped.
    /// - If the string is not quoted, it must not contain quotes (") or whitespace.
    /// </summary>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string inputString)
        {
            if (inputString == null) return null; // Should not happen
            if (inputString == string.Empty) return new MaybeQuotedString(string.Empty);

            // Check if the string is properly quoted
            if (!TryParsePrefix(inputString, out MaybeQuotedString? mbquotedString, out int consumedLength) ||
                    mbquotedString is null || consumedLength != inputString.Length)
            {
                // Failed to parse as a QuotedString
                throw new FormatException($"String is not properly quoted at position {consumedLength}: '{inputString}'.");
            }
            else
            {
                return mbquotedString;
            }
        }
        else if (value is QuotedString quotedString)
        {
            // Allow conversion from QuotedString
            return new MaybeQuotedString(quotedString.Value);
        }

        return base.ConvertFrom(context, culture, value);
    }

    // --- ConvertTo (MaybeQuotedString -> string || QuotedString ---

    /// <summary>
    /// Converts a MaybeQuotedString object into its string representation.
    /// - If the raw value contains quotes (") or whitespace, it returns the quoted and escaped version.
    /// - Otherwise, it returns the raw value.
    /// </summary>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            if (value is MaybeQuotedString maybeQuotedString)
            {
                string? rawValue = maybeQuotedString.Value;

                if (rawValue == null) return null; // Or "" depending on convention
                if (rawValue == string.Empty) return "\"\"";

                // Determine if quoting is needed
                bool needsQuoting = ShouldBeQuoted(rawValue);

                if (needsQuoting)
                {
                    // Use the same escape logic as QuotedStringTypeConverter
                    string? escapedValue = (string?)_qsConverter.ConvertToInvariantString(new QuotedString(rawValue));
                    return escapedValue;
                }
                else
                {
                    // Return the raw value as it doesn't need quoting
                    return rawValue;
                }
            }
            else if (value == null)
            {
                return null; // Or ""
            }
            else
            {
                throw new ArgumentException($"Invalid value type {value.GetType().Name}, expected {nameof(MaybeQuotedString)}.", nameof(value));
            }
        }
        else if (destinationType == typeof(QuotedString))
        {
            if (value is MaybeQuotedString maybeQuotedString)
            {
                // Convert MaybeQuotedString to QuotedString directly
                return new QuotedString(maybeQuotedString.Value);
            }
            else if (value == null)
            {
                return null;
            }
            else
            {
                throw new ArgumentException($"Invalid value type {value.GetType().Name}, expected {nameof(MaybeQuotedString)}.", nameof(value));
            }
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    #endregion

    #region ----- Other Public Methods. -----

    public bool ShouldBeQuoted(string rawString)
    {
        return rawString.Any(c => EscapedChars.Keys.Contains(c) || char.IsWhiteSpace(c));
    }

    /// <summary>
    /// Attempts to parse a MaybeQuotedString from the beginning of the specified text.
    /// It can parse either:
    /// 1. A quoted string (starts and ends with '"', handles '\' escapes).
    /// 2. An unquoted string (consumes characters until whitespace or EOS, cannot contain '"').
    /// </summary>
    /// <param name="text">The input string to parse.</param>
    /// <param name="startIndex">The starting index within the text to begin parsing.</param>
    /// <param name="parsedValue">When this method returns true, contains the parsed MaybeQuotedString object; otherwise, null.</param>
    /// <param name="consumedLength">When this method returns true, contains the number of characters consumed from the text.
    /// For unquoted strings, this is the length before any terminating whitespace.
    /// When this method returns false, indicates the number of characters examined before encountering a definitive format error or the end of the string prematurely (in the quoted case).</param>
    /// <returns>True if a valid Quoted or Unquoted string representation was successfully parsed from the prefix; otherwise, false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if text is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if startIndex is out of range.</exception>
    public bool TryParsePrefix(string text, int startIndex, out MaybeQuotedString? parsedValue, out int consumedLength, int? endIndex = null)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));

        if (endIndex == null)
        {
            endIndex = text.Length;
        }
        else if (endIndex < 0 || endIndex > text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(endIndex));
        }

        if (startIndex < 0 || startIndex > endIndex) // Allow startIndex == endIndex (results in immediate failure for content)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be within the bounds of the text.");

        parsedValue = null;
        consumedLength = 0;

        // Check if we are at the end of the string
        if (startIndex >= endIndex)
        {
            return false;
        }

        char firstChar = text[startIndex];

        if (firstChar == '"')
        {
            // --- Case 1: Attempt to parse QUOTED string ---
            // Delegate to the existing implementation for quoted strings
            if (_qsConverter.TryParsePrefix(text, startIndex, out QuotedString? tempQuotedString, out consumedLength, endIndex: endIndex)
                && tempQuotedString is QuotedString quotedString)
            {
                // Successfully parsed as a QuotedString. Convert to MaybeQuotedString.
                // tempQuotedString is guaranteed non-null here by TryParsePrefix contract.
                parsedValue = new MaybeQuotedString(quotedString.Value);
                // consumedLength is correctly set by the called method.
                return true;
            }
            else
            {
                // Failed parsing as a QuotedString (invalid format, unterminated, etc.)
                // consumedLength indicating failure point is already set by the called method.
                parsedValue = null;
                return false;
            }
        }
        else
        {
            // --- Case 2: Attempt to parse UNQUOTED string ---
            var sb = new StringBuilder();
            int currentIndex = startIndex;

            while (currentIndex < endIndex)
            {
                char c = text[currentIndex];

                if (char.IsWhiteSpace(c))
                {
                    // Found terminator whitespace
                    consumedLength = currentIndex - startIndex; // Length *before* whitespace
                    parsedValue = new MaybeQuotedString(sb.ToString());
                    return true; // Success!
                }

                if (ShouldEscape(c) && !EscapedChars[c].AllowUnescaped)
                {
                    // Unquoted string cannot contain this character.
                    consumedLength = currentIndex - startIndex; // Length *before* unescaped character
                    return false; // Format error
                }

                // Regular character for unquoted string
                sb.Append(c);
                currentIndex++;
            }

            // Reached end of string, the whole remainder is the unquoted string
            consumedLength = currentIndex - startIndex; // == text.Length - startIndex
            parsedValue = new MaybeQuotedString(sb.ToString());
            return true; // Success!
        }
    }

    /// <summary>
    /// Attempts to parse a MaybeQuotedString from the beginning of the specified text (starting at index 0).
    /// </summary>
    /// <param name="text">The input string to parse.</param>
    /// <param name="parsedValue">When this method returns true, contains the parsed MaybeQuotedString object; otherwise, null.</param>
    /// <param name="consumedLength">The number of characters consumed or examined.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool TryParsePrefix(string text, out MaybeQuotedString? parsedValue, out int consumedLength)
    {
        return TryParsePrefix(text, 0, out parsedValue, out consumedLength);
    }

    #endregion
}


/// <summary>
/// TypeConverter for converting between a list of standard strings 
/// </summary>
public class MaybeQuotedStringListTypeConverter : TypeConverter
{
    #region ----- Properties. -----

    private readonly MaybeQuotedStringTypeConverter _mqsConverter = new(
       new[] { new Escape('['), new(']'), new(',') }
    );

    public MaybeQuotedStringTypeConverter MQSConverter => _mqsConverter;

    #endregion

    #region ----- Constructors. -----


    #endregion

    #region ----- TypeConverter. -----

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string inputString)
        {
            if (inputString == null) return null; // Should not happen
            if (inputString == string.Empty) return new MaybeQuotedString(string.Empty);

            // Check if the string is properly quoted
            if (!TryParsePrefix(inputString, out List<string>? mbquotedString, out int consumedLength) ||
                    mbquotedString is null || consumedLength != inputString.Length)
            {
                // Failed to parse
                throw new FormatException($"String is not properly quoted at position {consumedLength}: '{inputString}'.");
            }
            else
            {
                return mbquotedString;
            }
        }
        else if (value is IEnumerable<string> list)
        {
            return list.ToList();
        }
        else if (value is IEnumerable<QuotedString> qlist)
        {
            return qlist.Select(qs => qs.Value).ToList();
        }
        else if (value is IEnumerable<MaybeQuotedString> mqList)
        {
            return mqList.Select(mqs => mqs.Value).ToList();
        }
        else
        {
            return base.ConvertFrom(context, culture, value);
        }
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            IEnumerable<string>? source = null;
            if (value is IEnumerable<string> x)
            {
                source = x;
            }
            else if (value is IEnumerable<QuotedString> y)
            {
                source = y.Select(qs => qs.Value);
            }
            else if (value is IEnumerable<MaybeQuotedString> z)
            {
                source = z.Select(mqs => mqs.Value);
            }

            if (source is IEnumerable<string> list)
            {
                StringBuilder b = new StringBuilder("[");
                bool isFirst = true;
                foreach (string s in list)
                {
                    if (s is null) return null;
                    MaybeQuotedString sQuoted = new(s);
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        b.Append(", ");
                    }
                    b.Append(_mqsConverter.ConvertToInvariantString(sQuoted));
                }
                b.Append(']');
                return b.ToString();
            }
            else if (value == null)
            {
                return null; // Or ""
            }
            else
            {
                throw new ArgumentException($"Invalid value type {value.GetType().Name}, expected {nameof(MaybeQuotedString)}.", nameof(value));
            }
        }
        else if (destinationType == typeof(QuotedString))
        {
            if (value is MaybeQuotedString maybeQuotedString)
            {
                // Convert MaybeQuotedString to QuotedString directly
                return new QuotedString(maybeQuotedString.Value);
            }
            else if (value == null)
            {
                return null;
            }
            else
            {
                throw new ArgumentException($"Invalid value type {value.GetType().Name}, expected {nameof(MaybeQuotedString)}.", nameof(value));
            }
        }
        else
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    #endregion

    #region ----- Other Public Methods. -----

    public bool TryParsePrefix(string text, int startIndex, out List<string>? parsedValue, out int consumedLength)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (startIndex < 0 || startIndex > text.Length) // Allow startIndex == text.Length (results in immediate failure for content)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be within the bounds of the text.");

        parsedValue = null;
        consumedLength = 0;

        // Check if there's enough length for at least starting [
        if (startIndex >= text.Length || text[startIndex] != '[')
        {
            // Not starting with a [, definitely not a valid list of string representation
            return false;
        }

        List<string> result = new();
        int currentIndex = startIndex + 1; // Start parsing after the opening quote
        bool expectComma = false;

        while (currentIndex < text.Length)
        {
            char c = text[currentIndex];

            if (char.IsWhiteSpace(c))
            {
                // Do nothing.
            }
            else if (c == ']')
            {
                // Concluded parsing
                consumedLength = currentIndex + 1 - startIndex;
                parsedValue = result;
                return true;
            }
            else if (expectComma)
            {
                if (c != ',')
                {
                    consumedLength = currentIndex - startIndex;
                    return false;
                }
                expectComma = false;
            }
            else
            {
                int consumed = 0;
                string parsed = string.Empty;
                if (!_mqsConverter.TryParsePrefix(text, currentIndex, out var mqs, out int consumedSubstring) ||
                    mqs is null)
                {
                    // Check for corner case unquoted string immediately followed by comma or list end ']'
                    if (text[currentIndex] != '"' &&
                        (currentIndex + consumedSubstring < text.Length) &&
                        new[] { ',', ']' }.Contains(text[currentIndex + consumedSubstring]))
                    {
                        if (!_mqsConverter.TryParsePrefix(text, currentIndex, out var mqs2, out int consumed2, currentIndex + consumedSubstring)
                            || mqs2 is null)
                        {
                            consumedLength = currentIndex + consumedSubstring - startIndex;
                            return false;
                        }
                        consumed = consumed2;
                        parsed = mqs2.Value;
                    }
                    else
                    {
                        consumedLength = currentIndex + consumedSubstring - startIndex;
                        return false;
                    }
                }
                else
                {
                    consumed = consumedSubstring;
                    parsed = mqs.Value;
                }
                result.Add(parsed);
                currentIndex += consumed - 1; // -1 due to currentIndex++ below.
                expectComma = true;
            }
            currentIndex++;
        }
        consumedLength = currentIndex - startIndex;
        return false;
    }

    public bool TryParsePrefix(string text, out List<string>? parsedValue, out int consumedLength)
    {
        return TryParsePrefix(text, 0, out parsedValue, out consumedLength);
    }

    #endregion
}
