using Spectre.Console;
using Spectre.Console.Cli;
using System.IO.Compression;
using System.Text.Json;

namespace oxs.Commands.Project;

public class ProjectBuildCommand : Command<ProjectBuildSettings>
{
    public override int Execute(CommandContext context, ProjectBuildSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(CommandContext context, ProjectBuildSettings settings, CancellationToken cancellationToken)
    {
        // Check if ox.json exists in current directory
        var currentDirectory = Directory.GetCurrentDirectory();
        var oxJsonPath = Path.Combine(currentDirectory, "ox.json");
        
        if (!File.Exists(oxJsonPath))
        {
            AnsiConsole.MarkupLine($"[red]ox.json not found in current directory: {currentDirectory}[/]");
            AnsiConsole.MarkupLine("[yellow]Please run this command in a directory containing an ox.json file[/]");
            return 1;
        }

        // Check if packages directory exists
        var packagesDirectory = Path.Combine(currentDirectory, "packages");
        if (!Directory.Exists(packagesDirectory))
        {
            AnsiConsole.MarkupLine($"[red]packages directory not found: {packagesDirectory}[/]");
            return 1;
        }

        // Create build directory if it doesn't exist
        var buildDirectory = Path.GetFullPath(settings.BuildDirectory ?? "./.build");
        if (!Directory.Exists(buildDirectory))
        {
            try
            {
                Directory.CreateDirectory(buildDirectory);
                AnsiConsole.MarkupLine($"[green]Created build directory:[/] {buildDirectory}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to create build directory {buildDirectory}: {ex.Message}[/]");
                return 1;
            }
        }

        // Process all subdirectories in packages
        var packageDirs = Directory.GetDirectories(packagesDirectory);
        var processedCount = 0;

        AnsiConsole.MarkupLine($"[blue]Scanning packages directory:[/] {packagesDirectory}");

        foreach (var packageDir in packageDirs)
        {
            var manifestPath = Path.Combine(packageDir, "manifest.json");
            
            if (!File.Exists(manifestPath))
            {
                AnsiConsole.MarkupLine($"[yellow]Skipping {Path.GetFileName(packageDir)} - no manifest.json found[/]");
                continue;
            }

            try
            {
                var result = await ProcessPackageAsync(packageDir, manifestPath, buildDirectory, cancellationToken);
                if (result)
                {
                    processedCount++;
                    AnsiConsole.MarkupLine($"[green]✓[/] Processed package: [yellow]{Path.GetFileName(packageDir)}[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to process package {Path.GetFileName(packageDir)}: {ex.Message}[/]");
            }
        }

        AnsiConsole.MarkupLine($"[green]Build completed![/] Processed {processedCount} packages");
        AnsiConsole.MarkupLine($"[green]Output directory:[/] {buildDirectory}");

        return 0;
    }

    private async Task<bool> ProcessPackageAsync(string packageDir, string manifestPath, string buildDirectory, CancellationToken cancellationToken)
    {
        // Read and parse manifest.json
        var manifestContent = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        
        using var manifestDoc = JsonDocument.Parse(manifestContent);
        var manifestRoot = manifestDoc.RootElement;

        // Extract id and version
        if (!manifestRoot.TryGetProperty("id", out var idElement) || 
            !manifestRoot.TryGetProperty("version", out var versionElement))
        {
            AnsiConsole.MarkupLine($"[red]Invalid manifest.json in {Path.GetFileName(packageDir)} - missing id or version[/]");
            return false;
        }

        var id = idElement.GetString();
        var version = versionElement.GetString();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(version))
        {
            AnsiConsole.MarkupLine($"[red]Invalid manifest.json in {Path.GetFileName(packageDir)} - empty id or version[/]");
            return false;
        }

        // Create a new manifest object by processing $ references
        var processedManifest = await ProcessManifestReferencesAsync(manifestRoot, packageDir, cancellationToken);

        // Create zip file name
        var zipFileName = $"{id}-{version}.zip";
        var zipFilePath = Path.Combine(buildDirectory, zipFileName);

        // Create zip file
        await CreateZipPackageAsync(packageDir, zipFilePath, processedManifest, cancellationToken);

        return true;
    }

    private async Task<JsonElement> ProcessManifestReferencesAsync(JsonElement manifestRoot, string packageDir, CancellationToken cancellationToken)
    {
        // Convert JsonElement to Dictionary for easier manipulation
        var manifestDict = new Dictionary<string, object>();

        foreach (var property in manifestRoot.EnumerateObject())
        {
            var value = property.Value;
            
            if (value.ValueKind == JsonValueKind.String)
            {
                var stringValue = value.GetString();
                
                // Check if it's a file reference (starts with $)
                if (stringValue?.StartsWith("$") == true)
                {
                    var fileName = stringValue.Substring(1);
                    var filePath = Path.Combine(packageDir, fileName);
                    
                    if (File.Exists(filePath))
                    {
                        var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
                        manifestDict[property.Name] = fileContent.Trim();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: Referenced file {fileName} not found in {Path.GetFileName(packageDir)}[/]");
                        manifestDict[property.Name] = stringValue;
                    }
                }
                else
                {
                    manifestDict[property.Name] = stringValue;
                }
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                var arrayValues = new List<object>();
                foreach (var arrayElement in value.EnumerateArray())
                {
                    if (arrayElement.ValueKind == JsonValueKind.String)
                    {
                        arrayValues.Add(arrayElement.GetString()!);
                    }
                    else
                    {
                        arrayValues.Add(arrayElement);
                    }
                }
                manifestDict[property.Name] = arrayValues;
            }
            else if (value.ValueKind == JsonValueKind.Object)
            {
                var nestedDict = new Dictionary<string, object>();
                foreach (var nestedProperty in value.EnumerateObject())
                {
                    if (nestedProperty.Value.ValueKind == JsonValueKind.String)
                    {
                        nestedDict[nestedProperty.Name] = nestedProperty.Value.GetString()!;
                    }
                    else
                    {
                        nestedDict[nestedProperty.Name] = nestedProperty.Value;
                    }
                }
                manifestDict[property.Name] = nestedDict;
            }
            else
            {
                // Handle other types (numbers, booleans, etc.)
                manifestDict[property.Name] = value;
            }
        }

        // Convert back to JsonElement
        var jsonString = JsonSerializer.Serialize(manifestDict, new JsonSerializerOptions { WriteIndented = true });
        using var doc = JsonDocument.Parse(jsonString);
        return doc.RootElement.Clone();
    }

    private async Task CreateZipPackageAsync(string packageDir, string zipFilePath, JsonElement processedManifest, CancellationToken cancellationToken)
    {
        // Delete existing zip file if it exists
        if (File.Exists(zipFilePath))
        {
            File.Delete(zipFilePath);
        }

        using var fileStream = new FileStream(zipFilePath, FileMode.Create);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        // Add processed manifest.json
        var manifestEntry = archive.CreateEntry("manifest.json");
        using (var entryStream = manifestEntry.Open())
        using (var writer = new StreamWriter(entryStream))
        {
            var manifestJson = JsonSerializer.Serialize(processedManifest, new JsonSerializerOptions { WriteIndented = true });
            await writer.WriteAsync(manifestJson);
        }

        // Add all other files from the package directory (except the original manifest.json)
        var files = Directory.GetFiles(packageDir, "*", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(packageDir, file);
            
            // Skip the original manifest.json since we're using the processed one
            if (relativePath.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            var entry = archive.CreateEntry(relativePath);
            using var entryStream = entry.Open();
            using var fileStream2 = File.OpenRead(file);
            await fileStream2.CopyToAsync(entryStream, cancellationToken);
        }
    }
}