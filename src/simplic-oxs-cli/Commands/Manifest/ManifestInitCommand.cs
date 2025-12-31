using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace oxs.Commands.Manifest;

public class ManifestInitCommand : Command<ManifestInitSettings>
{
    public override int Execute(CommandContext context, ManifestInitSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(CommandContext context, ManifestInitSettings settings, CancellationToken cancellationToken)
    {
        // Get id (required)
        if (string.IsNullOrWhiteSpace(settings.Id))
        {
            settings.Id = AnsiConsole.Ask<string>("Manifest [green]id[/] (e.g., customer.shipment-ext)?");
            if (string.IsNullOrWhiteSpace(settings.Id))
            {
                AnsiConsole.MarkupLine($"[red]Manifest id is required[/]");
                return 1;
            }
        }

        // Get title
        if (string.IsNullOrWhiteSpace(settings.Title))
        {
            settings.Title = AnsiConsole.Ask<string>("Manifest [green]title[/]?");
            if (string.IsNullOrWhiteSpace(settings.Title))
            {
                AnsiConsole.MarkupLine($"[red]Manifest title is required[/]");
                return 1;
            }
        }

        // Get author
        if (string.IsNullOrWhiteSpace(settings.Author))
        {
            settings.Author = AnsiConsole.Ask<string>("Author [green]name[/]?", "SIMPLIC GmbH");
        }

        // Get target (selectable list)
        var availableTargets = new[] { "oxs", "ox-web" };
        if (string.IsNullOrWhiteSpace(settings.Target))
        {
            settings.Target = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Please select a [green]target[/]?")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to select the target)[/]")
                    .AddChoices(availableTargets));
        }
        else if (!availableTargets.Contains(settings.Target))
        {
            AnsiConsole.MarkupLine($"[red]Invalid target. Available options: {string.Join(", ", availableTargets)}: {settings.Target}[/]");
            return 1;
        }

        // Create directory with the same name as the id
        var manifestDirectory = Path.GetFullPath(settings.Id);
        
        // Check if directory exists, create if not
        if (!Directory.Exists(manifestDirectory))
        {
            try
            {
                Directory.CreateDirectory(manifestDirectory);
                AnsiConsole.MarkupLine($"[green]Created directory:[/] {manifestDirectory}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to create directory {manifestDirectory}: {ex.Message}[/]");
                return 1;
            }
        }

        // Check if manifest.json already exists
        var manifestPath = Path.Combine(manifestDirectory, "manifest.json");
        if (File.Exists(manifestPath))
        {
            var overwrite = AnsiConsole.Confirm($"manifest.json already exists in {manifestDirectory}. Overwrite?");
            if (!overwrite)
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
                return 0;
            }
        }

        // Create manifest content
        var manifest = new
        {
            id = settings.Id,
            title = settings.Title,
            description = "$description.md",
            authors = new[] { settings.Author },
            owner = settings.Author,
            projectUrl = "https://simplic.biz",
            version = "1.0.0.0",
            versionTag = "beta",
            tags = new[] { "ext" },
            releaseNotes = "$releaseNotes.md",
            copyright = $"{settings.Author} - {DateTime.Now.Year}",
            target = settings.Target
        };

        try
        {
            // Create manifest.json
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var jsonContent = JsonSerializer.Serialize(manifest, jsonOptions);
            await File.WriteAllTextAsync(manifestPath, jsonContent, cancellationToken);

            AnsiConsole.MarkupLine($"[green]?[/] manifest.json created successfully at: [yellow]{manifestPath}[/]");

            // Create description.md
            var descriptionPath = Path.Combine(manifestDirectory, "description.md");
            var descriptionContent = $@"# {settings.Title}

This package extends the functionality of the system.

## Features

- Feature 1
- Feature 2
- Feature 3

## Installation

Install this package through the OXS package manager.

## Usage

Detailed usage instructions go here.
";
            
            try
            {
                await File.WriteAllTextAsync(descriptionPath, descriptionContent, cancellationToken);
                AnsiConsole.MarkupLine($"[green]?[/] description.md created: [yellow]{descriptionPath}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to create description.md: {ex.Message}[/]");
            }

            // Create releaseNotes.md
            var releaseNotesPath = Path.Combine(manifestDirectory, "releaseNotes.md");
            var releaseNotesContent = $@"# Release Notes

## Version 1.0.0.0 - {DateTime.Now:yyyy-MM-dd}

### Added
- Initial release
- Basic functionality implemented

### Changed
- N/A

### Fixed
- N/A

### Removed
- N/A
";
            
            try
            {
                await File.WriteAllTextAsync(releaseNotesPath, releaseNotesContent, cancellationToken);
                AnsiConsole.MarkupLine($"[green]?[/] releaseNotes.md created: [yellow]{releaseNotesPath}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to create releaseNotes.md: {ex.Message}[/]");
            }

            AnsiConsole.MarkupLine($"[green]?[/] Manifest '[yellow]{settings.Id}[/]' initialized with target '[yellow]{settings.Target}[/]'");
            AnsiConsole.MarkupLine("[blue]Next steps:[/]");
            AnsiConsole.MarkupLine("  1. Edit [yellow]description.md[/] with your package details");
            AnsiConsole.MarkupLine("  2. Update [yellow]releaseNotes.md[/] with version information");
            AnsiConsole.MarkupLine("  3. Modify [yellow]manifest.json[/] as needed");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to create manifest.json: {ex.Message}[/]");
            return 1;
        }
    }
}