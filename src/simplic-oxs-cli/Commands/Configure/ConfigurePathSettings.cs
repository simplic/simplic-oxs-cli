using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Configure
{
    public class ConfigurePathSettings : ConfigureSettings
    {
        [Description("Remove the CLI from PATH instead of adding it")]
        [CommandOption("-r|--remove")]
        public bool Remove { get; set; }

        [Description("Add to user PATH instead of system PATH")]
        [CommandOption("-u|--user")]
        public bool User { get; set; }
    }
}