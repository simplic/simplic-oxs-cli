using oxs.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace oxs.Commands.Package
{
    public class PackageInstallCommand : Command<PackageInstallSettings>
    {
        public override int Execute(CommandContext context, PackageInstallSettings settings, CancellationToken cancellationToken)
        {
            return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
        }

        private async Task<int> ExecuteAsync(CommandContext context, PackageInstallSettings settings, CancellationToken cancellationToken)
        {
            // Validate required parameters
            if (string.IsNullOrWhiteSpace(settings.PackageId))
            {
                AnsiConsole.MarkupLine("[red]Package ID is required. Use --package-id to specify the package ID.[/]");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(settings.Version))
            {
                AnsiConsole.MarkupLine("[red]Version is required. Use --version to specify the package version.[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[blue]Installing package:[/] [yellow]{settings.PackageId}[/]");
            AnsiConsole.MarkupLine($"[blue]Version:[/] [yellow]{settings.Version}[/]");
            AnsiConsole.MarkupLine($"[blue]Using configuration section:[/] [yellow]{settings.Section}[/]");

            try
            {
                using var httpService = new HttpService();

                // Build the endpoint URL with query parameters
                var endpoint = $"repository-api/v2/InstalledPackage?packageId={Uri.EscapeDataString(settings.PackageId)}&version={Uri.EscapeDataString(settings.Version)}";

                // Make the installation request (POST with no body)
                var result = await httpService.ExecuteRequestAsync(
                    "post",
                    endpoint,
                    settings.Section,
                    null); // No body required for this endpoint

                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[green]?[/] Successfully installed package: [yellow]{settings.PackageId}[/] version [yellow]{settings.Version}[/]");
                    
                    // Show response if available
                    if (!string.IsNullOrWhiteSpace(result.ResponseBody))
                    {
                        try
                        {
                            var responseDoc = System.Text.Json.JsonDocument.Parse(result.ResponseBody);
                            var formattedResponse = System.Text.Json.JsonSerializer.Serialize(responseDoc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                            AnsiConsole.MarkupLine($"[dim]Response:[/]");
                            Console.WriteLine(formattedResponse);
                        }
                        catch
                        {
                            AnsiConsole.MarkupLine($"[dim]Response: {result.ResponseBody}[/]");
                        }
                    }

                    return 0;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]?[/] Failed to install package: [yellow]{settings.PackageId}[/]");
                    AnsiConsole.MarkupLine($"[red]Error: {result.ErrorMessage}[/]");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]?[/] Failed to install package: [yellow]{settings.PackageId}[/]");
                AnsiConsole.MarkupLine($"[red]Exception: {ex.Message}[/]");
                return 1;
            }
        }
    }
}