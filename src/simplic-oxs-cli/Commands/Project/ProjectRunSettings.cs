using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Project
{
    public class ProjectRunSettings : ProjectSettings
    {
        [Description("Script name to run from ox.json scripts section")]
        [CommandArgument(0, "[script]")]
        public string? Script { get; set; }

        [Description("List all available scripts without executing")]
        [CommandOption("-l|--list")]
        public bool List { get; set; }
    }
}