using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Project;

/// <summary>
/// Settings for deploying project artifacts to the OXS platform.
/// </summary>
public class ProjectDeploySettings : ProjectSettings
{
    [Description("Path to the artifact file(s) to deploy (supports wildcards like ./.build/*.zip)")]
    [CommandOption("-a|--artifact <Path>")]
    public string? Artifact { get; set; }

    [Description("Configuration section to use for deployment")]
    [CommandOption("-s|--section <Section>")]
    public string Section { get; set; } = "default";
}