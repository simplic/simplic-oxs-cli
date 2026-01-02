using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Service;

/// <summary>
/// Settings for the service get-definition command to download service definitions from APIs.
/// </summary>
public class ServiceGetDefinitionSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the endpoint name of the service (e.g., document, storage-management, provider-rossum).
    /// </summary>
    [CommandOption("-e|--endpoint <ENDPOINT>")]
    [Description("The endpoint name of the service (e.g., document, storage-management, provider-rossum)")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the service (e.g., v1, v2).
    /// </summary>
    [CommandOption("-v|--version <VERSION>")]
    [Description("The version of the service (e.g., v1, v2)")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration section to use for determining the API environment (prod/staging).
    /// </summary>
    [CommandOption("-s|--section <SECTION>")]
    [Description("Configuration section to use (determines prod/staging environment)")]
    [DefaultValue("default")]
    public string Section { get; set; } = "default";

    /// <summary>
    /// Gets or sets the output directory where the service definition files will be saved.
    /// </summary>
    [CommandOption("-o|--output <OUTPUT>")]
    [Description("Output directory for service definition files (default: current directory)")]
    [DefaultValue(".")]
    public string OutputDirectory { get; set; } = ".";
}
