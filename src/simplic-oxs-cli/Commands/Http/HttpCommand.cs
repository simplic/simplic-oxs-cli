using oxs.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace oxs.Commands.Http;

/// <summary>
/// Command for making HTTP requests to APIs with configurable methods, headers, and body content.
/// </summary>
public class HttpCommand : Command<HttpCommandOptions>
{
    /// <summary>
    /// Executes the HTTP command with the specified options.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The HTTP command options and settings.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An exit code indicating success (0) or failure (1).</returns>
    public override int Execute(CommandContext context, HttpCommandOptions settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously executes the HTTP request with validation, request execution, and response formatting.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The HTTP command options and settings.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with an exit code indicating success (0) or failure (1).</returns>
    private async Task<int> ExecuteAsync(CommandContext context, HttpCommandOptions settings, CancellationToken cancellationToken)
    {
        // Validate method
        var validMethods = new[] { "get", "post", "put", "patch", "delete" };
        if (!validMethods.Contains(settings.Method.ToLower()))
        {
            if (!settings.FormatOnly)
                AnsiConsole.MarkupLine($"[red]Invalid HTTP method: {settings.Method}. Valid methods are: {string.Join(", ", validMethods)}[/]");
            return 1;
        }

        // Validate endpoint
        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            if (!settings.FormatOnly)
                AnsiConsole.MarkupLine("[red]Endpoint is required. Use --endpoint or -e to specify the API endpoint.[/]");
            return 1;
        }

        // Show request details only if not in format-only mode
        if (!settings.FormatOnly)
        {
            AnsiConsole.MarkupLine($"[blue]Making {settings.Method.ToUpper()} request to:[/] [yellow]{settings.Endpoint}[/]");
            AnsiConsole.MarkupLine($"[blue]Using configuration section:[/] [yellow]{settings.Section}[/]");

            if (!string.IsNullOrWhiteSpace(settings.Body))
            {
                var bodyDisplay = settings.Body.StartsWith("$") ? $"File: {settings.Body.Substring(1)}" : "Inline content";
                AnsiConsole.MarkupLine($"[blue]Request body:[/] [yellow]{bodyDisplay}[/]");
            }
        }

        using var httpService = new HttpService();
        
        var result = await httpService.ExecuteRequestAsync(
            settings.Method, 
            settings.Endpoint, 
            settings.Section,
            settings.Body,
            settings.Headers);

        if (!result.Success)
        {
            if (!settings.FormatOnly)
                AnsiConsole.MarkupLine($"[red]Request failed: {result.ErrorMessage}[/]");
            return 1;
        }

        // If format-only mode, only output the response body
        if (settings.FormatOnly)
        {
            var responseOutput = FormatOutput(result.ResponseBody, result.ContentType, settings.Format);
            Console.WriteLine(responseOutput);
            return 0;
        }

        // Display response with full information
        AnsiConsole.MarkupLine($"[green]Response Status:[/] [yellow]{(int)result.StatusCode!} {result.StatusCode}[/]");
        
        if (result.ResponseHeaders?.Count > 0)
        {
            AnsiConsole.MarkupLine("[blue]Response Headers:[/]");
            foreach (var header in result.ResponseHeaders)
            {
                AnsiConsole.MarkupLine($"  [dim]{header.Key}:[/] {header.Value}");
            }
        }

        AnsiConsole.MarkupLine("[blue]Response Body:[/]");
        
        // Format output based on requested format
        var formattedOutput = FormatOutput(result.ResponseBody, result.ContentType, settings.Format);
        Console.WriteLine(formattedOutput);

        return 0;
    }

    /// <summary>
    /// Formats the response body according to the specified format (json, xml, or text).
    /// </summary>
    /// <param name="responseBody">The raw response body content.</param>
    /// <param name="contentType">The content type header from the response.</param>
    /// <param name="format">The desired output format.</param>
    /// <returns>The formatted response body.</returns>
    private string FormatOutput(string? responseBody, string? contentType, string format)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return string.Empty;

        try
        {
            switch (format.ToLower())
            {
                case "json":
                    if (IsJsonContent(contentType) || IsValidJson(responseBody))
                    {
                        var jsonDocument = JsonDocument.Parse(responseBody);
                        return JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions 
                        { 
                            WriteIndented = true 
                        });
                    }
                    break;
                
                case "xml":
                    // For XML, we'll just return as-is with potential formatting
                    if (IsXmlContent(contentType))
                    {
                        return responseBody;
                    }
                    break;
                
                case "text":
                default:
                    return responseBody;
            }
        }
        catch
        {
            // If formatting fails, return original content
        }

        return responseBody;
    }

    /// <summary>
    /// Determines whether the content type indicates JSON content.
    /// </summary>
    /// <param name="contentType">The content type header value.</param>
    /// <returns>True if the content type indicates JSON; otherwise, false.</returns>
    private bool IsJsonContent(string? contentType)
    {
        return contentType != null && 
               (contentType.Contains("application/json") || 
                contentType.Contains("text/json"));
    }

    /// <summary>
    /// Determines whether the content type indicates XML content.
    /// </summary>
    /// <param name="contentType">The content type header value.</param>
    /// <returns>True if the content type indicates XML; otherwise, false.</returns>
    private bool IsXmlContent(string? contentType)
    {
        return contentType != null && 
               (contentType.Contains("application/xml") || 
                contentType.Contains("text/xml"));
    }

    /// <summary>
    /// Validates whether the provided content is valid JSON by attempting to parse it.
    /// </summary>
    /// <param name="content">The content to validate.</param>
    /// <returns>True if the content is valid JSON; otherwise, false.</returns>
    private bool IsValidJson(string content)
    {
        try
        {
            JsonDocument.Parse(content);
            return true;
        }
        catch
        {
            return false;
        }
    }
}