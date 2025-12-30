using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Project;

/// <summary>
/// Settings for building project artifacts and packaging them for deployment.
/// </summary>
public class ProjectBuildSettings : ProjectSettings
{
    [Description("Build output directory where zip files will be created")]
    [CommandOption("-b|--build-dir <Path>")]
    public string? BuildDirectory { get; set; } = "./.build";
}