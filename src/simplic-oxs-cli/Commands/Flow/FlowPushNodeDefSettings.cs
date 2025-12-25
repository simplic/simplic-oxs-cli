using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Flow
{
    public class FlowSettings : CommandSettings
    {
    }

    public class FlowPushNodeDefSettings : FlowSettings
    {
        [Description("Path to the .NET assembly (.dll)")]
        [CommandOption("-p|--path <Path>")]
        public string? DllPath { get; set; }

        [Description("Configuration section to use. Default: default")]
        [CommandOption("-s|--section <Section>")]
        [DefaultValue("default")]
        public string Section { get; set; } = "default";
    }
}
