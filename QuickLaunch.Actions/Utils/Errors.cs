using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickLaunch.Core.Utils
{
    /// <summary>
    /// Utilities for error handling
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Constructs and throws an exception of the specified type from a list of errors, if
        /// that list is not empty. 
        /// The exception message is created by joining the provided error strings.
        /// </summary>
        /// <param name="exceptionType">
        /// The <see cref="System.Type"/> of the exception to create and throw.
        /// This type must inherit from <see cref="System.Exception"/> and possess a public constructor
        /// accepting <c>(string message)</c> or <c>(string message, Exception innerException)</c>.
        /// </param>
        /// <param name="errors">
        /// A list of error messages to be joined together to form the primary exception message.
        /// If the list is empty, the function does nothing.
        /// </param>
        /// <param name="innerException">
        /// An optional inner exception (<see cref="System.Exception"/>) to be passed to the exception's constructor.
        /// If provided, the method will attempt to use a constructor accepting both a message and an inner exception.
        /// </param>
        /// <param name="errorsJoin">
        /// The separator string used to join the elements in the <paramref name="errors"/> list. Defaults to a single space (" ").
        /// </param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="exceptionType"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <paramref name="exceptionType"/> does not inherit from <see cref="System.Exception"/>,
        /// or if (and only if <paramref name="errors"/> is not null/empty) the specified <paramref name="exceptionType"/>
        /// lacks a suitable public constructor (either <c>(string)</c> or <c>(string, Exception)</c>
        /// based on whether <paramref name="innerException"/> is provided).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if (and only if <paramref name="errors"/> is not null/empty) creating the exception instance fails
        /// for reasons other than a missing constructor (e.g., the constructor itself throws an exception),
        /// or if the instantiation result is invalid.
        /// </exception>
        /// <remarks>
        /// The primary purpose of this method is to throw an exception of the type specified by <paramref name="exceptionType"/>
        /// when validation errors are present (<paramref name="errors"/> is not null or empty).
        /// The exceptions documented above (<see cref="ArgumentNullException"/>, <see cref="ArgumentException"/>, <see cref="InvalidOperationException"/>)
        /// relate to potential failures in validating the parameters or instantiating the requested exception type via reflection.
        /// If all checks pass and <paramref name="errors"/> has content, the requested exception is thrown.
        /// </remarks>
        public static void ThrowIfErrors(Type exceptionType, IReadOnlyList<string> errors, Exception? innerException = null, string errorsJoin = " ")
        {
            ThrowIfErrors(exceptionType, errors, Array.Empty<object?>(), innerException, errorsJoin);
        }

        /// <summary>
        /// Constructs and throws an exception of the type specified by <paramref name="exceptionType"/>
        /// if the <paramref name="errors"/> list is not null or empty, allowing for additional constructor arguments.
        /// The exception message is created by joining the provided error strings.
        /// </summary>
        /// <param name="exceptionType">
        /// The <see cref="System.Type"/> of the exception to create and throw.
        /// This type must inherit from <see cref="System.Exception"/>.
        /// </param>
        /// <param name="errors">
        /// A list of error messages to be joined together to form the primary exception message.
        /// If the list is null or empty, the function does nothing.
        /// </param>
        /// <param name="additionalConstructorArgs">
        /// An array of additional arguments to pass to the constructor of the <paramref name="exceptionType"/>.
        /// These arguments will be placed *after* the generated message and *before* the optional <paramref name="innerException"/>.
        /// </param>
        /// <param name="innerException">
        /// An optional inner exception (<see cref="System.Exception"/>) to be passed as the *last* argument to the exception's constructor.
        /// </param>
        /// <param name="errorsJoin">
        /// The separator string used to join the elements in the <paramref name="errors"/> list. Defaults to a single space (" ").
        /// </param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="exceptionType"/> or <paramref name="additionalConstructorArgs"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <paramref name="exceptionType"/> does not inherit from <see cref="System.Exception"/>,
        /// or if (and only if <paramref name="errors"/> is not null/empty) the specified <paramref name="exceptionType"/>
        /// lacks a public constructor matching the signature derived from the assembled arguments
        /// (message, additional args, optional inner exception).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if (and only if <paramref name="errors"/> is not null/empty) creating the exception instance fails
        /// for reasons other than a missing constructor (e.g., the constructor itself throws an exception),
        /// or if the instantiation result is invalid.
        /// </exception>
        /// <remarks>
        /// This overload uses <see cref="Activator.CreateInstance(Type, object[])"/> to invoke a constructor.
        /// It assumes a constructor signature pattern where the generated message string comes first,
        /// followed by the elements of <paramref name="additionalConstructorArgs"/>, and finally the
        /// <paramref name="innerException"/> if it is provided.
        /// Example: If called with `additionalConstructorArgs = { 123, "abc" }` and a non-null `innerException`,
        /// it attempts to find a constructor like `(string message, int arg1, string arg2, Exception inner)`.
        /// Ensure your custom exception type has a public constructor matching the types and order of the arguments you provide.
        /// </remarks>
        public static void ThrowIfErrors(
            Type exceptionType,
            IReadOnlyList<string> errors,
            object?[] additionalConstructorArgs,
            Exception? innerException = null,
            string errorsJoin = " ")
        {
            // 1. Validate critical input parameters first ("Fail Fast").
            ArgumentNullException.ThrowIfNull(exceptionType, nameof(exceptionType));
            ArgumentNullException.ThrowIfNull(additionalConstructorArgs, nameof(additionalConstructorArgs));
            if (!typeof(Exception).IsAssignableFrom(exceptionType))
            {
                throw new ArgumentException(
                    $"The type '{exceptionType.FullName}' must inherit from {typeof(Exception).FullName}.",
                    nameof(exceptionType)
                );
            }

            // 2. Check if there are any errors to process. If not, nothing more to do.
            if (errors is null || errors.Count == 0)
            {
                return;
            }

            // Filter empty error messages.
            List<string> validErrors = errors.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

            // 3. Prepare the message
            string message = string.Join(errorsJoin, validErrors);

            // 4. Assemble the complete list of arguments based on the convention
            // Order: [message, ...additionalConstructorArgs, innerException (if present)]
            // Using List for dynamic assembly, then converting to array
            List<object?> constructorArgsList = new();
            constructorArgsList.Add(message); // Message first
            constructorArgsList.AddRange(additionalConstructorArgs); // Then additional args
            if (innerException != null)
            {
                constructorArgsList.Add(innerException); // InnerException last, if provided
            }
            object?[] allArgs = constructorArgsList.ToArray();


            // 5. Create and throw a new instance of that type using Reflection
            object? exceptionInstance;
            try
            {
                exceptionInstance = Activator.CreateInstance(exceptionType, allArgs);
            }
            catch (MissingMethodException ex) // Catch if a matching constructor isn't found
            {
                // Generate the signature string based on the actual types passed
                string attemptedSignature = string.Join(", ", allArgs.Select(arg => arg?.GetType().FullName ?? "null"));
                throw new ArgumentException($"The exception type '{exceptionType.FullName}' does not have a public constructor matching the attempted argument types ({attemptedSignature}). Argument order convention: [message, ...additional args, innerException?].", nameof(exceptionType), ex);
            }
            catch (Exception ex) // Catch other potential issues during instantiation
            {
                throw new InvalidOperationException($"Failed to create an instance of exception type '{exceptionType.FullName}'. See inner exception for details.", ex);
            }

            // 6. Throw the created exception instance
            if (exceptionInstance is Exception exToThrow)
            {
                throw exToThrow;
            }
            else
            {
                // This case should be less likely if Activator didn't throw, but good to have.
                throw new InvalidOperationException($"Activator.CreateInstance returned null or a non-Exception object for type '{exceptionType.FullName}'.");
            }
        }

        /// <summary>
        /// Constructs and throws a <see cref="System.ArgumentException"/> directly from a list of errors,
        /// if that list is not empty, optionally including a parameter name and inner exception.
        /// The exception message is created by joining the provided error strings.
        /// </summary>
        /// <param name="errors">
        /// A list of error messages to be joined together to form the <see cref="ArgumentException"/> message.
        /// If the list is empty or null, the function does nothing.
        /// </param>
        /// <param name="paramName">
        /// The name of the parameter that caused the exception, passed directly to the relevant <see cref="ArgumentException"/> constructor.
        /// </param>
        /// <param name="innerException">
        /// An optional inner exception (<see cref="System.Exception"/>) passed directly to the relevant <see cref="ArgumentException"/> constructor.
        /// </param>
        /// <param name="errorsJoin">
        /// The separator string used to join the elements in the <paramref name="errors"/> list. Defaults to a single space (" ").
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="errors"/> list is not empty. The specific constructor used depends on the provided <paramref name="paramName"/> and <paramref name="innerException"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Can potentially be thrown if <paramref name="errors"/> is null (though prevented by initial check).
        /// </exception>
        public static void ThrowArgumentExceptionIfErrors(
            IReadOnlyList<string> errors,
            string? paramName = null,
            Exception? innerException = null,
            string errorsJoin = " ")
        {
            if (errors == null || errors.Count == 0) return;

            string message = string.Join(errorsJoin, errors);

            // --- Use explicit switch expression with tuple pattern matching (C# 9.0+) ---
            ArgumentException exceptionToThrow = (paramName, innerException) switch
            {
                // Case: Both are null
                (null, null) => new ArgumentException(message),

                // Case: Only innerException is not null
                (null, not null) => new ArgumentException(message, innerException),

                // Case: Only paramName is not null
                (not null, null) => new ArgumentException(message, paramName),

                // Case: Both are not null (Explicitly defined)
                (not null, not null) => new ArgumentException(message, paramName, innerException)

                // Note: No default '_' case is needed because all 4 possible combinations
                // of (null/not null, null/not null) are explicitly covered.
            };

            // Throw the constructed exception
            throw exceptionToThrow;
        }
    }
}
