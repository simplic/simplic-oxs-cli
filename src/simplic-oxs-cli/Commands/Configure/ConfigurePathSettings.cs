using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Configure;

/// <summary>
/// Settings for configuring the PATH environment variable for the OXS CLI.
/// </summary>
public class ConfigurePathSettings : ConfigureSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to remove the CLI from PATH instead of adding it.
    /// </summary>
    [Description("Remove the CLI from PATH instead of adding it")]
    [CommandOption("-r|--remove")]
    public bool Remove { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to modify the user PATH instead of the system PATH.
    /// </summary>
    [Description("Add to user PATH instead of system PATH")]
    [CommandOption("-u|--user")]
    public bool User { get; set; }
}