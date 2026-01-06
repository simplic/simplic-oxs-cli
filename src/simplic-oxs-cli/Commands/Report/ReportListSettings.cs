using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Report;

/// <summary>
/// Settings for the report list command to list all available reports from the reporting API.
/// </summary>
public class ReportListSettings : ReportSettings
{
    /// <summary>
    /// Gets or sets the configuration section to use for determining the API environment (prod/staging).
    /// </summary>
    [CommandOption("-s|--section <SECTION>")]
    [Description("Configuration section to use (determines prod/staging environment)")]
    [DefaultValue("default")]
    public string Section { get; set; } = "default";

    /// <summary>
    /// Gets or sets a value indicating whether to format the output as JSON.
    /// </summary>
    [CommandOption("-j|--json")]
    [Description("Output as JSON")]
    public bool JsonOutput { get; set; }
}
