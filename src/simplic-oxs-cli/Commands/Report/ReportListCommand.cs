using oxs.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http.Headers;
using System.Text.Json;

namespace oxs.Commands.Report;

/// <summary>
/// Command for listing all available reports from the reporting API.
/// </summary>
public class ReportListCommand : Command<ReportListSettings>
{
    /// <summary>
    /// Executes the report list command with the specified settings.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The settings for listing reports.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An exit code indicating success (0) or failure (1).</returns>
    public override int Execute(CommandContext context, ReportListSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously executes the report list process, fetching reports from the API.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The settings for listing reports.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with an exit code indicating success (0) or failure (1).</returns>
    private async Task<int> ExecuteAsync(CommandContext context, ReportListSettings settings, CancellationToken cancellationToken)
    {
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

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
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
                AnsiConsole.MarkupLine($"[red]Failed to fetch reports: {response.StatusCode}[/]");
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

            var reports = itemsElement.EnumerateArray().ToList();

            if (settings.JsonOutput)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.WriteLine(JsonSerializer.Serialize(reports, options));
                return 0;
            }

            if (reports.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No reports found.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[green]Found {reports.Count} report(s):[/]");
            AnsiConsole.WriteLine();

            var table = new Table();
            table.AddColumn("Name");
            table.AddColumn("ID");
            table.AddColumn("Created");
            table.AddColumn("Updated");

            foreach (var report in reports)
            {
                var name = report.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? "N/A" : "N/A";
                var id = report.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "N/A" : "N/A";
                var created = report.TryGetProperty("createDateTime", out var createElement) ? createElement.GetString() ?? "N/A" : "N/A";
                var updated = report.TryGetProperty("updateDateTime", out var updateElement) ? updateElement.GetString() ?? "N/A" : "N/A";

                table.AddRow(
                    $"[yellow]{name.EscapeMarkup()}[/]",
                    $"[dim]{id.EscapeMarkup()}[/]",
                    $"[dim]{created.EscapeMarkup()}[/]",
                    $"[dim]{updated.EscapeMarkup()}[/]"
                );
            }

            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error fetching reports: {ex.Message}[/]");
            return 1;
        }
    }
}
