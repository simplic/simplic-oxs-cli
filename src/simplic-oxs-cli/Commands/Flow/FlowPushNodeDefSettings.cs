using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Flow;

/// <summary>
/// Base settings for flow-related commands.
/// </summary>
public class FlowSettings : CommandSettings
{
}

/// <summary>
/// Settings for pushing node definitions to the flow service.
/// </summary>
public class FlowPushNodeDefSettings : FlowSettings
{
    /// <summary>
    /// Gets or sets the path to the .NET assembly (.dll) containing flow node definitions.
    /// </summary>
    [Description("Path to the .NET assembly (.dll)")]
    [CommandOption("-p|--path <Path>")]
    public string? DllPath { get; set; }

    /// <summary>
    /// Gets or sets the configuration section to use for API connection. Default is 'default'.
    /// </summary>
    [Description("Configuration section to use. Default: default")]
    [CommandOption("-s|--section <Section>")]
    [DefaultValue("default")]
    public string Section { get; set; } = "default";
}
