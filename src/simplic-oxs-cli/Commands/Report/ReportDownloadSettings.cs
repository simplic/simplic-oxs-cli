using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Report;

/// <summary>
/// Settings for the report download command to download a report definition from the reporting API.
/// </summary>
public class ReportDownloadSettings : ReportSettings
{
    /// <summary>
    /// Gets or sets the name of the report to download.
    /// </summary>
    [CommandOption("-n|--name <NAME>")]
    [Description("The name of the report to download")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the file name to save the downloaded report to.
    /// </summary>
    [CommandOption("-f|--file <FILE>")]
    [Description("The file name to save the report to")]
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the configuration section to use for determining the API environment (prod/staging).
    /// </summary>
    [CommandOption("-s|--section <SECTION>")]
    [Description("Configuration section to use (determines prod/staging environment)")]
    [DefaultValue("default")]
    public string Section { get; set; } = "default";
}
