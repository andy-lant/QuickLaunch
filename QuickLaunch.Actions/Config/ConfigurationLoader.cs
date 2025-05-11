#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Actions;
using QuickLaunch.Core.KeyEvents;
using QuickLaunch.Core.Logging;
using Tomlyn; // TOML parsing library

// Use file-scoped namespace
namespace QuickLaunch.Core.Config;

/// <summary>
/// Handles loading and parsing the application configuration from a TOML file.
/// Accessible only within the current assembly.
/// </summary>
public class ConfigurationLoader
{
    #region ----- Constants. -----

    private const string ConfigFileName = "config.toml";

    private const string AppDataFolderName = "QuickLaunch";

    #endregion

    #region ----- Fields. -----

    private string ConfigFilePath { get; }

    #endregion

    #region ----- Constructors. -----

    /// <summary>
    /// ConfigurationLoader using default configuration file path.
    /// </summary>
    public ConfigurationLoader()
    {
        ConfigFilePath = GetConfigFilePath();
    }

    /// <summary>
    /// ConfigurationLoader with custom configuration file path.
    /// </summary>
    /// <param name="configFilePath"></param>
    public ConfigurationLoader(string configFilePath)
    {
        ArgumentNullException.ThrowIfNull(configFilePath, nameof(configFilePath));
        ConfigFilePath = configFilePath;
    }

    #endregion

    #region ----- Public Methods. -----

    /// <summary>
    /// Gets the full path to the configuration file.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    /// <returns>The full path string.</returns>
    public static string GetConfigFilePath()
    {
        string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolderPath = Path.Combine(localAppDataPath, AppDataFolderName);

        // Ensure the directory exists
        if (!Directory.Exists(appFolderPath))
        {
            try
            {
                Directory.CreateDirectory(appFolderPath);
                Log.Logger?.LogDebug($"Created configuration directory: {appFolderPath}");
            }
            catch (Exception ex)
            {
                Log.Logger?.LogDebug($"ERROR creating configuration directory '{appFolderPath}': {ex.Message}");
                // Propagate or handle error appropriately - maybe fallback to default config?
                throw new IOException($"Failed to create configuration directory: {appFolderPath}", ex);
            }
        }

        return Path.Combine(appFolderPath, ConfigFileName);
    }

    /// <summary>
    /// Loads the application configuration from the TOML file.
    /// If the file doesn't exist, it creates a default one and returns an empty config.
    /// </summary>
    /// <returns>The loaded AppConfig object.</returns>
    public AppConfig LoadConfig()
    {
        string configPath = ConfigFilePath;
        Log.Logger?.LogInformation($"Loading configuration from: {configPath}");

        if (!File.Exists(configPath))
        {
            Log.Logger?.LogWarning($"Configuration file not found. Creating default empty file at: {configPath}");
            File.Copy("config.sample.toml", configPath);
            return new AppConfig(); // Return empty config as default
        }

        try
        {
            string tomlContent = File.ReadAllText(configPath);
            var options = new TomlModelOptions
            {
                ConvertToModel = (value, changeType) =>
                {
                    if (value is string single && typeof(List<string>).IsAssignableFrom(changeType))
                    {
                        // Handle single string to List<string> conversion
                        return new List<string> { single };
                    }
                    return null;
                }
            };
            TomlConfigModel tomlConfig = Toml.ToModel<TomlConfigModel>(tomlContent, options: options);

            AppConfig config = tomlConfig.ToAppConfig(); // Convert to AppConfig

            Log.Logger?.LogInformation($"Successfully loaded config.");
            return config;
        }
        catch (IOException ioEx)
        {
            Log.Logger?.LogError(ioEx, $"ERROR reading configuration file '{configPath}'. ");
            throw;
        }
        catch (Exception ex) // Catches Tomlyn parsing errors and others
        {
            Log.Logger?.LogError(ex, $"ERROR parsing configuration file '{configPath}'.");
            throw new FormatException($"Failed to parse configuration file: {configPath}. Please check its format.", ex);
        }
    }

    /// <summary>
    /// Saves the provided application configuration to the TOML file.
    /// Overwrites the existing file.
    /// </summary>
    /// <param name="config">The AppConfig object to save.</param>
    /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the config is invalid or conversion fails.</exception>
    /// <exception cref="IOException">Thrown if writing to the file fails.</exception>
    /// <exception cref="Tomlyn.Syntax.TomlException">Thrown if Tomlyn fails to serialize the model.</exception>
    public void SaveConfig(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        if (!config.IsValid)
        {
            Log.Logger?.LogDebug($"ERROR: Invalid configuration data. Cannot save.");
            throw new InvalidOperationException("Configuration data is invalid. Cannot save.");
        }

        string configPath = ConfigFilePath;
        Log.Logger?.LogDebug($"Saving configuration to: {configPath}");

        // 1. Convert AppConfig to TomlConfigModel
        TomlConfigModel tomlModel;
        try
        {
            tomlModel = ConvertToTomlModel(config);
        }
        catch (Exception ex)
        {
            Log.Logger?.LogDebug($"ERROR converting AppConfig to TomlConfigModel: {ex.Message}");
            throw new InvalidOperationException("Failed to convert configuration data for saving.", ex);
        }

        // 2. Serialize TomlConfigModel to TOML string
        string tomlContent;
        try
        {
            // Add comments or structure if desired, though Tomlyn primarily handles data
            tomlContent = Toml.FromModel(tomlModel);
            // Prepend header comments manually if needed
            tomlContent = $@"# QuickLaunch Configuration File (Generated on {DateTime.Now})

{tomlContent}";
        }
        catch (Exception ex) // Catch potential Tomlyn serialization errors
        {
            Log.Logger?.LogDebug($"ERROR serializing configuration model to TOML: {ex.Message}");
            throw; // Re-throw Tomlyn exception or wrap it
        }


        // 3. Write TOML string to file
        try
        {
            File.WriteAllText(configPath, tomlContent);
            Log.Logger?.LogDebug($"Successfully saved configuration to: {configPath}");
        }
        catch (IOException ioEx)
        {
            Log.Logger?.LogDebug($"ERROR writing configuration file '{configPath}': {ioEx.Message}");
            throw; // Re-throw IO exception
        }
        catch (Exception ex) // Catch other potential errors like UnauthorizedAccessException
        {
            Log.Logger?.LogDebug($"ERROR writing configuration file '{configPath}': {ex.Message}");
            // Wrap in IOException or a more specific exception if needed
            throw new IOException($"An unexpected error occurred while writing the configuration file: {ex.Message}", ex);
        }
    }

    #endregion

    #region ----- Helper Methods. -----

    /// <summary>
    /// Converts an AppConfig object into the TomlConfigModel used for serialization.
    /// </summary>
    /// <param name="config">The AppConfig instance.</param>
    /// <returns>A TomlConfigModel representation.</returns>
    /// <exception cref="InvalidOperationException">If conversion encounters issues (e.g., inconsistent action types within a dispatcher).</exception>
    private TomlConfigModel ConvertToTomlModel(AppConfig config)
    {
        var tomlModel = new TomlConfigModel();

        // Convert Dispatchers and their Actions
        foreach (var dispatcherDef in config.Dispatchers)
        {
            _ = dispatcherDef ?? throw new NullReferenceException($"Dispatcher is null.");

            // Add TomlDispatcherDefinition
            tomlModel.Dispatcher.Add(new TomlDispatcherDefinition
            {
                Name = dispatcherDef.Name,
            });

            // Add TomlActionRegistration for each action in this dispatcher
            foreach (var actionEntry in dispatcherDef.Actions)
            {
                var tomlAction = new TomlActionRegistration
                {
                    Dispatcher = dispatcherDef.Name, // Link action to dispatcher name
                    Index = actionEntry.Index,
                    Type = actionEntry.Action.ActionType.Name,
                    Params = new Dictionary<string, List<string>>()
                };

                // Convert ActionParameters back to Dictionary<string, string>
                foreach (var param in actionEntry.Action.Parameters)
                {
                    // Convert value back to string. Use InvariantCulture for consistency.
                    // Handle potential null values appropriately.
                    string stringValue = string.Empty;
                    if (param.Value != null)
                    {
                        // Use TypeConverter if available for more robust conversion, otherwise ToString()
                        if (param.Type == typeof(StringListParameter))
                        {
                            tomlAction.Params[param.Key] = ((StringListParameter)param.Value).List;
                        }
                        else
                        {
                            TypeConverter? converter = TypeDescriptor.GetConverter(param.Type);
                            if (converter != null && converter.CanConvertTo(typeof(string)))
                            {
                                // Use ConvertToInvariantString for consistency
                                stringValue = converter.ConvertToInvariantString(param.Value) ?? string.Empty;
                            }
                            else
                            {
                                Log.Logger?.LogDebug($"Warning: No converter found for parameter type {param.Type.FullName}. Using ToString() instead.");
                                // Fallback to ToString with invariant culture
                                stringValue = Convert.ToString(param.Value, CultureInfo.InvariantCulture) ?? string.Empty;
                            }
                            tomlAction.Params[param.Key] = new List<string> { stringValue }; // Wrap in list for consistency
                        }
                    }
                }
                tomlModel.Action.Add(tomlAction);
            }
        }

        // Convert CommandTriggers
        foreach (var trigger in config.CommandTriggers)
        {
            string sequenceString;
            // Convert KeySequence back to string representation
            var converter = new KeySequenceConverter();
            sequenceString = converter.ConvertToInvariantString(trigger.Sequence) ?? throw new InvalidDataException($"Invalid KeySequence '{trigger.Sequence}' for trigger '{trigger.Name}'.");

            tomlModel.Command.Add(new TomlCommandTrigger
            {
                Name = trigger.Name, // Include the trigger name
                Sequence = sequenceString,
                Dispatcher = trigger.Dispatcher.Name // Link to dispatcher by name
            });
        }

        return tomlModel;
    }

    #endregion
}

internal class TomlConfigModel
{
    public List<TomlDispatcherDefinition> Dispatcher { get; set; } = new();
    public List<TomlActionRegistration> Action { get; set; } = new();
    public List<TomlCommandTrigger> Command { get; set; } = new();

    internal AppConfig ToAppConfig()
    {
        // Parse dispatchers.
        List<DispatcherDefinition> dispatchers = new();
        foreach (var dispatcher in Dispatcher)
        {
            var actions = new List<DispatcherActionEntry>();
            foreach (var action in Action)
            {
                if (action.Dispatcher.Equals(dispatcher.Name))
                {
                    actions.Add(DispatcherActionEntry.Create(action.Index, action.ToActionRegistration()));
                }
            }

            DispatcherDefinition definition = DispatcherDefinition.Create(dispatcher.Name, actions.AsReadOnly());
            dispatchers.Add(definition);
        }

        var commands = new List<CommandTrigger>();
        foreach (var command in Command)
        {
            var sequence = (KeySequence?)new KeySequenceConverter().ConvertFrom(command.Sequence) ??
                throw new FormatException($"Command '{command.Name}' has an invalid sequence format.");
            var dispatcher = dispatchers.FirstOrDefault(d => d?.Name.Equals(command.Dispatcher) ?? false, null);
            if (dispatcher == null)
            {
                throw new KeyNotFoundException($"Command '{command.Name}' references unknown dispatcher '{command.Dispatcher}'.");
            }
            var trigger = CommandTrigger.Create(command.Name, dispatcher, sequence);

            if (!dispatchers.Exists(d => d.Name.Equals(command.Dispatcher)))
            {
                throw new KeyNotFoundException($"Command '{command.Name}' references unknown dispatcher '{command.Dispatcher}'.");
            }
            else
            {
                commands.Add(trigger);
            }

        }

        var appConfig = AppConfig.Create(dispatchers.AsReadOnly(), commands.AsReadOnly());
        return appConfig;
    }


}

internal class TomlDispatcherDefinition
{
    public string Name { get; set; } = string.Empty;
}

internal class TomlActionRegistration
{
    public TomlActionRegistration()
    {
    }
    public string Dispatcher { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public uint Index { get; set; } = 0;
    public Dictionary<string, List<string>> Params { get; set; } = new();

    public ActionRegistration ToActionRegistration()
    {
        var actionType = ActionFactory.LookupActionType(Type) ?? throw new NullReferenceException($"Invalid or unregistered action type {Type}.");

        // Convert to ActionRegistration
        var action = ActionRegistration.Create(actionType, Params.Select(kvp => (kvp.Key, kvp.Value)));
        return action;
    }
}

internal class TomlCommandTrigger
{
    public TomlCommandTrigger()
    {
    }
    public string Sequence { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string Dispatcher { get; set; } = string.Empty;
}

#nullable disable
