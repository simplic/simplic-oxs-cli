using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Manifest;

/// <summary>
/// Settings for initializing a new manifest with specified configuration.
/// </summary>
public class ManifestInitSettings : CommandSettings
{
    [Description("Unique identifier for the manifest (e.g., customer.shipment-ext)")]
    [CommandOption("-i|--id <Id>")]
    public string? Id { get; set; }

    [Description("Title of the manifest")]
    [CommandOption("-t|--title <Title>")]
    public string? Title { get; set; }

    [Description("Author name")]
    [CommandOption("-a|--author <Author>")]
    public string? Author { get; set; }

    [Description("Target platform for the manifest. Options: oxs, ox-web")]
    [CommandOption("--target <Target>")]
    public string? Target { get; set; }
}