using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Project;

/// <summary>
/// Settings for cleaning project build artifacts and output directories.
/// </summary>
public class ProjectCleanSettings : ProjectSettings
{
    [Description("Build output directory to clean")]
    [CommandOption("-b|--build-dir <Path>")]
    public string? BuildDirectory { get; set; } = "./.build";
}