using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Project
{
    public class ProjectCleanSettings : ProjectSettings
    {
        [Description("Build output directory to clean")]
        [CommandOption("-b|--build-dir <Path>")]
        public string? BuildDirectory { get; set; } = "./build";
    }
}