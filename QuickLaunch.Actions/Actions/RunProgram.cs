#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Logging;
using QuickLaunch.Core.Utils;

// Use file-scoped namespace
namespace QuickLaunch.Core.Actions;

// Now implements IAction
internal class RunProgram : IAction
{
    public string Executable { get; }

    private StringListParameter _arguments;

    public IReadOnlyList<string> Arguments => _arguments.List.AsReadOnly();

    public static ActionType ActionType { get; } = new ActionType(
        "RunProgram", "Run program.",
        typeof(RunProgram), new ActionParameterInfo[]
        {
            new("Executable", typeof(string), IsOptional: false, "The executable to run."),
            new("Arguments", typeof(StringListParameter), IsOptional: true, "The arguments to pass to the executable.")
        }
    );

    public RunProgram(string executable, StringListParameter? arguments = null)
    {
        Executable = executable;
        _arguments = arguments ?? new StringListParameter();
    }

    public void Execute()
    {
        Log.Logger?.LogDebug($"Executing RunProgram: {Executable}");

        try
        {
            ProcessStartInfo startInfo = new(Executable)
            {
                UseShellExecute = true // to use system path
            };
            startInfo.ArgumentList.AddRange(Arguments);

            Process.Start(startInfo);
        }
        catch (Win32Exception winEx)
        {
            // This exception often occurs if there's no application associated
            // with the file type or other OS-level issues.
            throw new InvalidOperationException($"Could not start executable '{Executable}'. Executable not found or OS error. Win32 Error Code: {winEx.NativeErrorCode}", winEx);
        }
        catch (Exception ex)
        {
            // Catch other potential exceptions during process start
            throw new InvalidOperationException($"An unexpected error occurred while trying to run executable '{Executable}'.", ex);
        }
    }
}
#nullable disable
