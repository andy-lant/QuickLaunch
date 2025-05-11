using System.Collections.Generic;

namespace QuickLaunch.Core.Utils
{
    /// <summary>
    /// Defines a contract for objects that support internal validation
    /// and expose their validation state.
    /// </summary>
    public interface IValidate
    {
        /// <summary>
        /// Gets a value indicating whether the object is currently in a valid state.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Gets the list of current validation errors. Returns an empty list if the object is valid.
        /// </summary>
        IReadOnlyList<string> ValidationErrors { get; }

        /// <summary>
        /// Validates the object's current state.
        /// </summary>
        /// <param name="errors">When this method returns, contains a read-only list of validation error messages if validation failed; otherwise, an empty list.</param>
        /// <returns>True if the object's state is valid; otherwise, false.</returns>

        bool Validate(out IReadOnlyList<string> errors);

        /// <summary>
        /// Validates the object's current state, discarding the specific errors.
        /// </summary>
        /// <returns>True if object is valid; otherwise, false.</returns>
        /// <remarks>
        /// Default implementation relies on the primary Validate method.
        /// </remarks>
        bool Validate()
        {
            return Validate(out _);
        }
    }
}
