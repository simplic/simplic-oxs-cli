using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Manifest;

/// <summary>
/// Settings for listing available deployment templates.
/// </summary>
public class ManifestListTemplatesSettings : CommandSettings
{
    [Description("Show detailed information about each template")]
    [CommandOption("--detailed")]
    public bool ShowDetailed { get; set; }
}