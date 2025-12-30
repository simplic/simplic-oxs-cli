using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Project;

/// <summary>
/// Settings for initializing a new OXS project with specified configuration and structure.
/// </summary>
public class ProjectInitSettings : ProjectSettings
{
    [Description("Directory where the project should be initialized")]
    [CommandOption("-p|--project-dir <Path>")]
    public string? ProjectDirectory { get; set; }

    [Description("Name of the project")]
    [CommandOption("-n|--name <Name>")]
    public string? Name { get; set; }

    [Description("Target platform for the project. Options: ox")]
    [CommandOption("-t|--target <Target>")]
    public string? Target { get; set; }

    [Description("Description of the project")]
    [CommandOption("-d|--description <Description>")]
    public string? Description { get; set; }

    [Description("Project used for build and deploy")]
    [CommandOption("-s|--section <section>")]
    public string? Section { get; set; } = "default";
}