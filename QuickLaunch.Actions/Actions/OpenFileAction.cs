#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Logging;
using QuickLaunch.Core.Utils;

// Use file-scoped namespace
namespace QuickLaunch.Core.Actions;


// Now implements IAction
internal class OpenFileAction : IAction
{
    public string Path { get; }

    public static ActionType ActionType { get; } = new ActionType(
        "OpenFile", "Open a file using the default application associated with the file type.",
        typeof(OpenFileAction),
        new ActionParameterInfo[] {
            new ActionParameterInfo("Path", typeof(string), false, "The file path to open.")
        }
    );

    public OpenFileAction(string path)
    {
        // Use modern ThrowIfNullOrEmpty
        ArgumentExceptionHelper.ThrowIfNullOrEmpty(path, nameof(path));
        Path = path;
    }

    public void Execute()
    {
        Log.Logger?.LogDebug($"Executing OpenFileAction for path: {Path}");

        // Check if the file exists (Do this at execution time)
        if (!File.Exists(Path))
        {
            string fullPath = System.IO.Path.GetFullPath(Path);
            throw new FileNotFoundException($"Error: The file was not found at the specified path: '{fullPath}'", fullPath);
        }

        try
        {
            ProcessStartInfo startInfo = new(Path)
            {
                UseShellExecute = true // Use the OS shell to execute the file (find default app)
            };

            Log.Logger?.LogDebug($"Attempting to open file: {Path}");
            Process.Start(startInfo);
            Log.Logger?.LogDebug($"Successfully initiated opening of file: {Path}");
        }
        catch (Win32Exception winEx)
        {
            // This exception often occurs if there's no application associated
            // with the file type or other OS-level issues.
            throw new InvalidOperationException($"Could not open file '{Path}'. No application associated or OS error. Win32 Error Code: {winEx.NativeErrorCode}", winEx);
        }
        catch (Exception ex)
        {
            // Catch other potential exceptions during process start
            throw new InvalidOperationException($"An unexpected error occurred while trying to open the file '{Path}'.", ex);
        }

    }
}


#nullable disable
