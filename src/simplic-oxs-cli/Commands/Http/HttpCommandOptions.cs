using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Http;

/// <summary>
/// Settings for executing HTTP requests against APIs with various options and configurations.
/// </summary>
public class HttpCommandOptions : CommandSettings
{
    /// <summary>
    /// Gets or sets the HTTP method for the request (get, post, put, patch, delete).
    /// </summary>
    [CommandArgument(0, "<METHOD>")]
    [Description("HTTP method (get, post, put, patch, delete)")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API endpoint URL to call.
    /// </summary>
    [CommandOption("-e|--endpoint <ENDPOINT>")]
    [Description("The API endpoint to call")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration section to use for authentication and API settings.
    /// </summary>
    [CommandOption("-s|--section <SECTION>")]
    [Description("Configuration section to use")]
    [DefaultValue("default")]
    public string Section { get; set; } = "default";

    /// <summary>
    /// Gets or sets the request body content. Use $filename to load from file or provide inline content directly.
    /// </summary>
    [CommandOption("-b|--body <BODY>")]
    [Description("Request body (use $filename for file or inline content for direct content)")]
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets additional HTTP headers in the format 'Key:Value;Key2:Value2'.
    /// </summary>
    [CommandOption("-H|--headers <HEADERS>")]
    [Description("Additional headers in format 'Key:Value;Key2:Value2'")]
    public string? Headers { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to return only the response content without headers or status information.
    /// </summary>
    [CommandOption("-f|--format-only")]
    [Description("Return only response content, no headers or status information")]
    public bool FormatOnly { get; set; }

    /// <summary>
    /// Gets or sets the output format for the response (json, xml, text). Default is 'json'.
    /// </summary>
    [CommandOption("-o|--output-format <FORMAT>")]
    [Description("Output format (json, xml, text)")]
    [DefaultValue("json")]
    public string Format { get; set; } = "json";
}