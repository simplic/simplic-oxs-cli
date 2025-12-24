using oxs.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace oxs.Commands.Http
{
    public class HttpCommand : Command<HttpCommandOptions>
    {
        public override int Execute(CommandContext context, HttpCommandOptions settings, CancellationToken cancellationToken)
        {
            return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
        }

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

        private bool IsJsonContent(string? contentType)
        {
            return contentType != null && 
                   (contentType.Contains("application/json") || 
                    contentType.Contains("text/json"));
        }

        private bool IsXmlContent(string? contentType)
        {
            return contentType != null && 
                   (contentType.Contains("application/xml") || 
                    contentType.Contains("text/xml"));
        }

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
}