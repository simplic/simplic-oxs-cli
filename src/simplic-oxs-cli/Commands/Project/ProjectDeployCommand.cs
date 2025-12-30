using oxs.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace oxs.Commands.Project
{
    public class ProjectDeployCommand : Command<ProjectDeploySettings>
    {
        public override int Execute(CommandContext context, ProjectDeploySettings settings, CancellationToken cancellationToken)
        {
            return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
        }

        private async Task<int> ExecuteAsync(CommandContext context, ProjectDeploySettings settings, CancellationToken cancellationToken)
        {
            // Validate artifact path is provided
            if (string.IsNullOrWhiteSpace(settings.Artifact))
            {
                AnsiConsole.MarkupLine("[red]Artifact path is required. Use --artifact or -a to specify the artifact file(s).[/]");
                return 1;
            }

            // Resolve artifact files (handle wildcards)
            var artifactFiles = ResolveArtifactFiles(settings.Artifact);

            if (artifactFiles.Length == 0)
            {
                AnsiConsole.MarkupLine($"[red]No artifact files found matching pattern: {settings.Artifact}[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[blue]Found {artifactFiles.Length} artifact file(s) to deploy:[/]");
            foreach (var file in artifactFiles)
            {
                AnsiConsole.MarkupLine($"  [yellow]{file}[/]");
            }

            AnsiConsole.MarkupLine($"[blue]Using configuration section:[/] [yellow]{settings.Section}[/]");

            var successCount = 0;
            var failCount = 0;

            foreach (var artifactFile in artifactFiles)
            {
                try
                {
                    AnsiConsole.MarkupLine($"[blue]Deploying:[/] [yellow]{Path.GetFileName(artifactFile)}[/]");

                    // Read the artifact file and encode it as base64 string
                    var fileBytes = await File.ReadAllBytesAsync(artifactFile, cancellationToken);
                    var base64Data = Convert.ToBase64String(fileBytes);

                    // Create the request payload
                    var payload = new
                    {
                        data = base64Data
                    };

                    var payloadJson = JsonSerializer.Serialize(payload);

                    // Create a new HttpService instance for each request to avoid HttpClient reuse issues
                    using var httpService = new HttpService();

                    // Make the deployment request
                    var result = await httpService.ExecuteRequestAsync(
                        "post",
                        "repository-api/v2/Package",
                        settings.Section,
                        payloadJson);

                    if (result.Success)
                    {
                        successCount++;
                        AnsiConsole.MarkupLine($"[green]✓[/] Successfully deployed: [yellow]{Path.GetFileName(artifactFile)}[/]");
                        
                        // Show response if available
                        if (!string.IsNullOrWhiteSpace(result.ResponseBody))
                        {
                            try
                            {
                                var responseDoc = JsonDocument.Parse(result.ResponseBody);
                                var formattedResponse = JsonSerializer.Serialize(responseDoc, new JsonSerializerOptions { WriteIndented = true });
                                AnsiConsole.MarkupLine($"[dim]Response: {formattedResponse}[/]");
                            }
                            catch
                            {
                                AnsiConsole.MarkupLine($"[dim]Response: {result.ResponseBody}[/]");
                            }
                        }
                    }
                    else
                    {
                        failCount++;
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to deploy: [yellow]{Path.GetFileName(artifactFile)}[/]");
                        AnsiConsole.MarkupLine($"[red]Error: {result.ErrorMessage}[/]");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to deploy: [yellow]{Path.GetFileName(artifactFile)}[/]");
                    AnsiConsole.MarkupLine($"[red]Exception: {ex.Message}[/]");
                }
            }

            // Summary
            AnsiConsole.MarkupLine($"[blue]Deployment completed![/]");
            AnsiConsole.MarkupLine($"[green]Successfully deployed:[/] {successCount} files");
            
            if (failCount > 0)
            {
                AnsiConsole.MarkupLine($"[red]Failed to deploy:[/] {failCount} files");
                return 1;
            }

            return 0;
        }

        private string[] ResolveArtifactFiles(string artifactPattern)
        {
            try
            {
                // Handle wildcard patterns
                if (artifactPattern.Contains('*') || artifactPattern.Contains('?'))
                {
                    var directoryPath = Path.GetDirectoryName(artifactPattern);
                    var fileName = Path.GetFileName(artifactPattern);

                    // If no directory specified, use current directory
                    if (string.IsNullOrEmpty(directoryPath))
                    {
                        directoryPath = Directory.GetCurrentDirectory();
                    }
                    else
                    {
                        directoryPath = Path.GetFullPath(directoryPath);
                    }

                    if (!Directory.Exists(directoryPath))
                    {
                        return Array.Empty<string>();
                    }

                    return Directory.GetFiles(directoryPath, fileName, SearchOption.TopDirectoryOnly)
                        .OrderBy(f => f)
                        .ToArray();
                }
                else
                {
                    // Single file path
                    var fullPath = Path.GetFullPath(artifactPattern);
                    return File.Exists(fullPath) ? new[] { fullPath } : Array.Empty<string>();
                }
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}