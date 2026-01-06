using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Report;

/// <summary>
/// Settings for the report upload command to upload a report definition to the reporting API.
/// </summary>
public class ReportUploadSettings : ReportSettings
{
    /// <summary>
    /// Gets or sets the name of the report to upload.
    /// </summary>
    [CommandOption("-n|--name <NAME>")]
    [Description("The name of the report to upload")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the file name containing the report definition to upload.
    /// </summary>
    [CommandOption("-f|--file <FILE>")]
    [Description("The file name containing the report definition")]
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the configuration section to use for determining the API environment (prod/staging).
    /// </summary>
    [CommandOption("-s|--section <SECTION>")]
    [Description("Configuration section to use (determines prod/staging environment)")]
    [DefaultValue("default")]
    public string Section { get; set; } = "default";
}
