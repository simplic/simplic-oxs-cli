using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Project;

/// <summary>
/// Settings for running project scripts defined in the ox.json configuration file.
/// </summary>
public class ProjectRunSettings : ProjectSettings
{
    /// <summary>
    /// Gets or sets the name of the script to run from the ox.json scripts section.
    /// </summary>
    [Description("Script name to run from ox.json scripts section")]
    [CommandArgument(0, "[script]")]
    public string? Script { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to list all available scripts without executing any of them.
    /// </summary>
    [Description("List all available scripts without executing")]
    [CommandOption("-l|--list")]
    public bool List { get; set; }
}