using oxs.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace oxs.Commands.Report;

/// <summary>
/// Command for downloading a report definition from the reporting API.
/// </summary>
public class ReportDownloadCommand : Command<ReportDownloadSettings>
{
    /// <summary>
    /// Executes the report download command with the specified settings.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The settings for downloading a report.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An exit code indicating success (0) or failure (1).</returns>
    public override int Execute(CommandContext context, ReportDownloadSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously executes the report download process, fetching the report definition from the API and saving it to a file.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The settings for downloading a report.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with an exit code indicating success (0) or failure (1).</returns>
    private async Task<int> ExecuteAsync(CommandContext context, ReportDownloadSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            settings.Name = AnsiConsole.Ask<string>("Report [green]name[/]?");
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[red]Report name is required. Use -n|--name <name>[/]");
                return 1;
            }
        }

        if (string.IsNullOrWhiteSpace(settings.FileName))
        {
            settings.FileName = AnsiConsole.Ask<string>("File [green]name[/]?", $"{settings.Name}.json");
        }

        var configManager = new ConfigurationManager();
        var config = await configManager.LoadConfigurationAsync(settings.Section);

        if (config == null)
        {
            AnsiConsole.MarkupLine($"[red]Configuration not found for section '{settings.Section}'. Run 'oxs configure env'.[/]");
            return 1;
        }

        var apiBase = config.Api == "prod" ? "https://oxs.simplic.io/reporting-api/v1" :
                      config.Api == "staging" ? "https://dev-oxs.simplic.io/reporting-api/v1" : null;

        if (apiBase == null)
        {
            AnsiConsole.MarkupLine($"[red]Unknown API environment: {config.Api}[/]");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(config.Token))
        {
            AnsiConsole.MarkupLine("[red]Missing bearer token in configuration. Run 'oxs configure env'.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[blue]Downloading report:[/] [yellow]{settings.Name}[/]");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            // First, get all reports and find the one with matching name (without content field)
            var graphqlQuery = new
            {
                query = @"
                    query {
                        report {
                            items {
                                id
                                name
                                createDateTime
                                createUserId
                                updateDateTime
                                updateUserId
                            }
                        }
                    }"
            };

            var jsonContent = JsonSerializer.Serialize(graphqlQuery);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"{apiBase}/graphql", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                AnsiConsole.MarkupLine($"[red]Failed to fetch reports list: {response.StatusCode}[/]");
                AnsiConsole.MarkupLine($"[red]Error: {errorContent}[/]");
                return 1;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseContent);

            // Check for GraphQL errors
            if (jsonDoc.RootElement.TryGetProperty("errors", out var errorsElement))
            {
                AnsiConsole.MarkupLine("[red]GraphQL query returned errors:[/]");
                foreach (var error in errorsElement.EnumerateArray())
                {
                    if (error.TryGetProperty("message", out var messageElement))
                    {
                        AnsiConsole.MarkupLine($"[red]  - {messageElement.GetString().EscapeMarkup()}[/]");
                    }
                }
                return 1;
            }

            if (!jsonDoc.RootElement.TryGetProperty("data", out var dataElement) ||
                !dataElement.TryGetProperty("report", out var reportElement) ||
                !reportElement.TryGetProperty("items", out var itemsElement))
            {
                AnsiConsole.MarkupLine("[red]Unexpected response format from API[/]");
                AnsiConsole.MarkupLine("[yellow]Response received:[/]");
                var formattedResponse = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(formattedResponse);
                return 1;
            }

            // Find the report with matching name
            string? reportId = null;
            foreach (var item in itemsElement.EnumerateArray())
            {
                if (item.TryGetProperty("name", out var nameElement) &&
                    nameElement.GetString() == settings.Name)
                {
                    if (item.TryGetProperty("id", out var idElement))
                    {
                        reportId = idElement.GetString();
                    }
                    break;
                }
            }

            if (reportId == null)
            {
                AnsiConsole.MarkupLine($"[red]Report '{settings.Name}' not found.[/]");
                return 1;
            }

            // Now fetch the full report with content using REST API
            AnsiConsole.MarkupLine($"[blue]Fetching report content...[/]");
            var reportResponse = await http.GetAsync($"{apiBase}/Report/{reportId}", cancellationToken);

            if (!reportResponse.IsSuccessStatusCode)
            {
                var errorContent = await reportResponse.Content.ReadAsStringAsync(cancellationToken);
                AnsiConsole.MarkupLine($"[red]Failed to download report content: {reportResponse.StatusCode}[/]");
                AnsiConsole.MarkupLine($"[red]Error: {errorContent}[/]");
                return 1;
            }

            var reportContent = await reportResponse.Content.ReadAsStringAsync(cancellationToken);
            var reportDoc = JsonDocument.Parse(reportContent);
            
            // Check if there's a 'bytes' field containing Base64-encoded content
            if (reportDoc.RootElement.TryGetProperty("bytes", out var bytesElement))
            {
                var base64String = bytesElement.GetString();

                if (!string.IsNullOrWhiteSpace(base64String))
                {
                    try
                    {
                        // Decode the Base64 string to bytes
                        var decodedBytes = Convert.FromBase64String(base64String);

                        // If it's not JSON, save the decoded content as-is
                        await File.WriteAllBytesAsync(settings.FileName, decodedBytes, cancellationToken);
                    }
                    catch (FormatException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Failed to decode Base64 content: {ex.Message}[/]");
                        return 1;
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Warning: Report bytes field is empty[/]");
                    return 1;
                }
            }
            else
            {
                // Fallback: save the entire API response if no 'bytes' field exists
                AnsiConsole.MarkupLine("[yellow]Warning: No 'bytes' field found in response, saving full API response[/]");
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var prettyJson = JsonSerializer.Serialize(reportDoc, jsonOptions);
                await File.WriteAllTextAsync(settings.FileName, prettyJson, cancellationToken);
            }

            AnsiConsole.MarkupLine($"[green]✓ Report downloaded successfully to:[/] [yellow]{Path.GetFullPath(settings.FileName)}[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error downloading report: {ex.Message}[/]");
            return 1;
        }
    }
}
