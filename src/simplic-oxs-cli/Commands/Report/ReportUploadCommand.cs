using oxs.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http.Headers;
using System.Text.Json;

namespace oxs.Commands.Report;

/// <summary>
/// Command for uploading a report definition to the reporting API. Creates a new report or updates an existing one.
/// </summary>
public class ReportUploadCommand : Command<ReportUploadSettings>
{
    /// <summary>
    /// Executes the report upload command with the specified settings.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The settings for uploading a report.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An exit code indicating success (0) or failure (1).</returns>
    public override int Execute(CommandContext context, ReportUploadSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously executes the report upload process, reading the report from a file and uploading it to the API.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The settings for uploading a report.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with an exit code indicating success (0) or failure (1).</returns>
    private async Task<int> ExecuteAsync(CommandContext context, ReportUploadSettings settings, CancellationToken cancellationToken)
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

        if (!File.Exists(settings.FileName))
        {
            AnsiConsole.MarkupLine($"[red]File not found:[/] {settings.FileName}");
            return 1;
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

        AnsiConsole.MarkupLine($"[blue]Uploading report:[/] [yellow]{settings.Name}[/] from [yellow]{settings.FileName}[/]");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            // Read the report file
            var reportContent = await File.ReadAllBytesAsync(settings.FileName, cancellationToken);
                
            // First, try to check if report exists by fetching all reports
            var checkQuery = new
            {
                query = @"
                    query {
                        report {
                            items {
                                id
                                name
                            }
                        }
                    }"
            };

            var checkJsonContent = JsonSerializer.Serialize(checkQuery);
            var checkContent = new StringContent(checkJsonContent, System.Text.Encoding.UTF8, "application/json");
            var checkResponse = await http.PostAsync($"{apiBase}/graphql", checkContent, cancellationToken);

            bool reportExists = false;
            string? reportId = null;

            if (checkResponse.IsSuccessStatusCode)
            {
                var checkResponseContent = await checkResponse.Content.ReadAsStringAsync(cancellationToken);
                var checkJsonDoc = JsonDocument.Parse(checkResponseContent);

                // Check for GraphQL errors (not fatal for check, report may not exist)
                if (!checkJsonDoc.RootElement.TryGetProperty("errors", out _))
                {
                    if (checkJsonDoc.RootElement.TryGetProperty("data", out var dataElement) &&
                        dataElement.TryGetProperty("report", out var reportElement) &&
                        reportElement.TryGetProperty("items", out var itemsElement))
                    {
                        // Find the report with matching name
                        foreach (var item in itemsElement.EnumerateArray())
                        {
                            if (item.TryGetProperty("name", out var nameElement) && 
                                nameElement.GetString() == settings.Name)
                            {
                                reportExists = true;
                                if (item.TryGetProperty("id", out var idElement))
                                {
                                    reportId = idElement.GetString();
                                }
                                break;
                            }
                        }
                    }
                }
            }

            // Prepare the request based on whether report exists
            if (reportExists && !string.IsNullOrEmpty(reportId))
            {
                // Update existing report using PATCH
                AnsiConsole.MarkupLine($"[yellow]Report exists. Updating...[/]");

                var patchRequest = new
                {
                    name = settings.Name,
                    bytes = reportContent
                };

                var patchJson = JsonSerializer.Serialize(patchRequest);
                var patchContent = new StringContent(patchJson, System.Text.Encoding.UTF8, "application/json");
                var patchResponse = await http.PatchAsync($"{apiBase}/Report/{reportId}", patchContent, cancellationToken);

                if (!patchResponse.IsSuccessStatusCode)
                {
                    var errorContent = await patchResponse.Content.ReadAsStringAsync(cancellationToken);
                    AnsiConsole.MarkupLine($"[red]Failed to update report: {patchResponse.StatusCode}[/]");
                    AnsiConsole.MarkupLine($"[red]Error: {errorContent}[/]");
                    return 1;
                }

                AnsiConsole.MarkupLine($"[green]✓ Report updated successfully[/]");
            }
            else
            {
                // Create new report using POST
                AnsiConsole.MarkupLine($"[yellow]Creating new report...[/]");

                var postRequest = new
                {
                    name = settings.Name,
                    bytes = reportContent
                };

                var postJson = JsonSerializer.Serialize(postRequest);
                var postContent = new StringContent(postJson, System.Text.Encoding.UTF8, "application/json");
                var postResponse = await http.PostAsync($"{apiBase}/Report", postContent, cancellationToken);

                if (!postResponse.IsSuccessStatusCode)
                {
                    var errorContent = await postResponse.Content.ReadAsStringAsync(cancellationToken);
                    AnsiConsole.MarkupLine($"[red]Failed to create report: {postResponse.StatusCode}[/]");
                    AnsiConsole.MarkupLine($"[red]Error: {errorContent}[/]");
                    return 1;
                }

                AnsiConsole.MarkupLine($"[green]✓ Report created successfully[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error uploading report: {ex.Message}[/]");
            return 1;
        }
    }
}
