#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Logging;
using QuickLaunch.Core.Utils; // For Win32Exception

// Use file-scoped namespace
namespace QuickLaunch.Core.Actions;

/// <summary>
/// Action to open a URL in the default web browser.
/// </summary>
internal class OpenUrlAction : IAction
{
    public static ActionType ActionType { get; } = new ActionType(
        "OpenUrl", "Open an URL using the default application associated with it.",
        typeof(OpenUrlAction),
        new ActionParameterInfo[] {
            new ActionParameterInfo("Url", typeof(string), false, "The URL to open.")
        }
    );

    public string Url { get; }

    /// <summary>
    /// Initializes a new instance of the OpenUrlAction class.
    /// </summary>
    /// <param name="url">The URL to open. Must be a valid absolute URL.</param>
    /// <exception cref="ArgumentException">Thrown if url is null, empty, or not a valid absolute URI.</exception>
    public OpenUrlAction(string url)
    {
        ArgumentExceptionHelper.ThrowIfNullOrEmpty(url, nameof(url));

        // Basic validation - ensure it's an absolute URI
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
            || uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException($"Invalid URL format or scheme: '{url}'. Must be an absolute HTTP or HTTPS URL.", nameof(url));
        }

        Url = url; // Store the original string
    }

    public void Execute()
    {
        Log.Logger?.LogDebug($"Executing OpenUrlAction for URL: {Url}");

        try
        {
            // Using ProcessStartInfo is safer and recommended
            ProcessStartInfo startInfo = new(Url)
            {
                UseShellExecute = true // Crucial for opening URL in default browser
            };
            Process.Start(startInfo);
            Log.Logger?.LogDebug($"Successfully initiated opening of URL: {Url}");
        }
        catch (Win32Exception winEx)
        {
            // Common error if no browser is configured or other OS issues
            Log.Logger?.LogDebug($"Win32Exception opening URL '{Url}': {winEx.Message} (Code: {winEx.NativeErrorCode})");
            throw new InvalidOperationException($"Could not open URL '{Url}'. No default browser configured or OS error. Win32 Error Code: {winEx.NativeErrorCode}", winEx);
        }
        catch (Exception ex)
        {
            Log.Logger?.LogDebug($"Exception opening URL '{Url}': {ex.Message}");
            // Catch other potential exceptions
            throw new InvalidOperationException($"An unexpected error occurred while trying to open the URL '{Url}'.", ex);
        }
    }
}
#nullable disable
