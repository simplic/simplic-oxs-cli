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
            // Validate that either artifact directory or package-id/version are provided
            bool hasArtifact = !string.IsNullOrWhiteSpace(settings.ArtifactDirectory);
            bool hasPackageInfo = !string.IsNullOrWhiteSpace(settings.PackageId) && !string.IsNullOrWhiteSpace(settings.Version);

            if (!hasArtifact && !hasPackageInfo)
            {
                AnsiConsole.MarkupLine("[red]Either --artifact directory or both --package-id and --version must be provided.[/]");
                return 1;
            }

            if (hasArtifact && hasPackageInfo)
            {
                AnsiConsole.MarkupLine("[red]Cannot specify both --artifact and --package-id/--version at the same time.[/]");
                return 1;
            }

            if (hasArtifact)
            {
                return await InstallFromArtifactDirectoryAsync(settings, cancellationToken);
            }
            else
            {
                return await InstallSinglePackageAsync(settings, cancellationToken);
            }
        }

        private async Task<int> InstallFromArtifactDirectoryAsync(PackageInstallSettings settings, CancellationToken cancellationToken)
        {
            var artifactDir = Path.GetFullPath(settings.ArtifactDirectory!);

            if (!Directory.Exists(artifactDir))
            {
                AnsiConsole.MarkupLine($"[red]Artifact directory does not exist: {artifactDir}[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[blue]Scanning artifact directory:[/] [yellow]{artifactDir}[/]");
            AnsiConsole.MarkupLine($"[blue]Using configuration section:[/] [yellow]{settings.Section}[/]");

            // Find all .zip files matching the pattern
            var zipFiles = Directory.GetFiles(artifactDir, "*.zip", SearchOption.TopDirectoryOnly);
            var packages = new List<(string PackageId, string Version, string FilePath)>();

            foreach (var zipFile in zipFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(zipFile);
                var packageInfo = ParsePackageFileName(fileName);
                
                if (packageInfo.HasValue)
                {
                    packages.Add((packageInfo.Value.PackageId, packageInfo.Value.Version, zipFile));
                }
                // Silently skip files with invalid format
            }

            if (packages.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No valid package files found in the specified directory.[/]");
                AnsiConsole.MarkupLine("[dim]Expected format: <package-id>-<version>.zip (e.g., my-package-1.0.0.zip)[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]Found {packages.Count} package(s) to install:[/]");
            foreach (var package in packages)
            {
                AnsiConsole.MarkupLine($"  [yellow]{package.PackageId}[/] version [yellow]{package.Version}[/]");
            }

            // Install each package
            int successCount = 0;
            int failureCount = 0;

            foreach (var package in packages)
            {
                AnsiConsole.MarkupLine($"\n[blue]Installing package:[/] [yellow]{package.PackageId}[/] version [yellow]{package.Version}[/]");
                
                var result = await InstallPackageAsync(package.PackageId, package.Version, settings.Section, cancellationToken);
                
                if (result)
                {
                    successCount++;
                    AnsiConsole.MarkupLine($"[green]✓[/] Successfully installed: [yellow]{package.PackageId}[/] version [yellow]{package.Version}[/]");
                }
                else
                {
                    failureCount++;
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to install: [yellow]{package.PackageId}[/] version [yellow]{package.Version}[/]");
                }
            }

            // Summary
            AnsiConsole.MarkupLine($"\n[blue]Installation Summary:[/]");
            AnsiConsole.MarkupLine($"  [green]Successful:[/] {successCount}");
            AnsiConsole.MarkupLine($"  [red]Failed:[/] {failureCount}");
            AnsiConsole.MarkupLine($"  [blue]Total:[/] {packages.Count}");

            return failureCount == 0 ? 0 : 1;
        }

        private (string PackageId, string Version)? ParsePackageFileName(string fileName)
        {
            // Expected format: package-id-version
            // Find the last dot to separate version from package-id
            var lastDotIndex = fileName.LastIndexOf('-');
            
            if (lastDotIndex <= 0 || lastDotIndex >= fileName.Length - 1)
            {
                return null;
            }

            var packageId = fileName.Substring(0, lastDotIndex);
            var version = fileName.Substring(lastDotIndex + 1);

            // Basic validation
            if (string.IsNullOrWhiteSpace(packageId) || string.IsNullOrWhiteSpace(version))
            {
                return null;
            }
            
            return (packageId, version);
        }

        private async Task<int> InstallSinglePackageAsync(PackageInstallSettings settings, CancellationToken cancellationToken)
        {
            AnsiConsole.MarkupLine($"[blue]Installing package:[/] [yellow]{settings.PackageId}[/]");
            AnsiConsole.MarkupLine($"[blue]Version:[/] [yellow]{settings.Version}[/]");
            AnsiConsole.MarkupLine($"[blue]Using configuration section:[/] [yellow]{settings.Section}[/]");

            var result = await InstallPackageAsync(settings.PackageId!, settings.Version!, settings.Section, cancellationToken);
            return result ? 0 : 1;
        }

        private async Task<bool> InstallPackageAsync(string packageId, string version, string section, CancellationToken cancellationToken)
        {
            try
            {
                using var httpService = new HttpService();

                // Build the endpoint URL with query parameters
                var endpoint = $"repository-api/v2/InstalledPackage?packageId={Uri.EscapeDataString(packageId)}&version={Uri.EscapeDataString(version)}";

                // Make the installation request (POST with no body)
                var result = await httpService.ExecuteRequestAsync(
                    "post",
                    endpoint,
                    section,
                    null); // No body required for this endpoint

                if (result.Success)
                {
                    // Show response if available
                    if (!string.IsNullOrWhiteSpace(result.ResponseBody))
                    {
                        try
                        {
                            var responseDoc = System.Text.Json.JsonDocument.Parse(result.ResponseBody);
                            var formattedResponse = System.Text.Json.JsonSerializer.Serialize(responseDoc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                            AnsiConsole.MarkupLine($"[dim]Response:[/]");
                            Console.WriteLine(formattedResponse);

                            // Check if installation was actually successful by examining the response
                            return IsInstallationSuccessful(responseDoc, packageId);
                        }
                        catch
                        {
                            AnsiConsole.MarkupLine($"[dim]Response: {result.ResponseBody}[/]");
                            // If we can't parse JSON, check for success message in plain text
                            return result.ResponseBody.Contains($"The package {packageId} installation was successful", StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    // If no response body, assume success based on HTTP status
                    return true;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Error: {result.ErrorMessage}[/]");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Exception: {ex.Message}[/]");
                return false;
            }
        }

        private bool IsInstallationSuccessful(System.Text.Json.JsonDocument responseDoc, string packageId)
        {
            try
            {
                var root = responseDoc.RootElement;
                
                // Check if installationLogPackage exists and has state = "success"
                if (root.TryGetProperty("installationLogPackage", out var packageLog))
                {
                    if (packageLog.TryGetProperty("state", out var state))
                    {
                        if (state.GetString()?.Equals("success", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            // Also check the log messages for the success confirmation
                            if (packageLog.TryGetProperty("log", out var logArray) && logArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var logEntry in logArray.EnumerateArray())
                                {
                                    var logMessage = logEntry.GetString();
                                    if (!string.IsNullOrEmpty(logMessage) && 
                                        logMessage.Contains($"The package {packageId} installation was successful", StringComparison.OrdinalIgnoreCase))
                                    {
                                        return true;
                                    }
                                }
                            }
                            
                            // If state is success but we didn't find the specific log message, still consider it successful
                            // but this might indicate a different response format
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch
            {
                // If we can't parse the structure, assume failure
                return false;
            }
        }
    }
}