using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Manifest;

/// <summary>
/// Settings for adding a deployment from a template.
/// </summary>
public class ManifestAddDeploymentSettings : CommandSettings
{
    [Description("Name of the template to use (e.g., logistics.shipment.status)")]
    [CommandOption("-t|--template <Template>")]
    public string? Template { get; set; }

    [Description("Name of the deployment")]
    [CommandOption("-n|--name <Name>")]
    public string? Name { get; set; }
}