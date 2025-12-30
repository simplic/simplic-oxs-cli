using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace oxs.Commands.Project
{
    public class ProjectInitCommand : Command<ProjectInitSettings>
    {
        public override int Execute(CommandContext context, ProjectInitSettings settings, CancellationToken cancellationToken)
        {
            return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
        }

        private async Task<int> ExecuteAsync(CommandContext context, ProjectInitSettings settings, CancellationToken cancellationToken)
        {
            // Get project directory (required)
            if (string.IsNullOrWhiteSpace(settings.ProjectDirectory))
            {
                settings.ProjectDirectory = AnsiConsole.Ask<string>("Project [green]directory[/] (where ox.json will be created)?");
                if (string.IsNullOrWhiteSpace(settings.ProjectDirectory))
                {
                    AnsiConsole.MarkupLine($"[red]Project directory is required[/]");
                    return 1;
                }
            }

            // Resolve full path
            var projectPath = Path.GetFullPath(settings.ProjectDirectory);
            
            // Check if directory exists, create if not
            if (!Directory.Exists(projectPath))
            {
                try
                {
                    Directory.CreateDirectory(projectPath);
                    AnsiConsole.MarkupLine($"[green]Created directory:[/] {projectPath}");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Failed to create directory {projectPath}: {ex.Message}[/]");
                    return 1;
                }
            }

            // Check if ox.json already exists
            var oxJsonPath = Path.Combine(projectPath, "ox.json");
            if (File.Exists(oxJsonPath))
            {
                var overwrite = AnsiConsole.Confirm($"ox.json already exists in {projectPath}. Overwrite?");
                if (!overwrite)
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
                    return 0;
                }
            }

            // Get project name
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                settings.Name = AnsiConsole.Ask<string>("Project [green]name[/]?");
                if (string.IsNullOrWhiteSpace(settings.Name))
                {
                    AnsiConsole.MarkupLine($"[red]Project name is required[/]");
                    return 1;
                }
            }

            // Get target (selectable list)
            var availableTargets = new[] { "ox" };
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

            // Get description
            if (string.IsNullOrWhiteSpace(settings.Description))
            {
                settings.Description = AnsiConsole.Ask<string>("Project [green]description[/] (optional)?", "");
            }

            // Create ox.json content
            var projectConfig = new
            {
                name = settings.Name,
                target = settings.Target,
                description = settings.Description ?? "",
                version = "1.0.0",
                scripts = new
                {
                    clean = "oxs project clean --build-dir ./build",
                    build = "oxs project build --build-dir ./build",
                    deploy = $"oxs project deploy --artifact ./build/*.zip --section {settings.Section}"
                }
            };

            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var jsonContent = JsonSerializer.Serialize(projectConfig, jsonOptions);
                await File.WriteAllTextAsync(oxJsonPath, jsonContent, cancellationToken);

                AnsiConsole.MarkupLine($"[green]✓[/] ox.json created successfully at: [yellow]{oxJsonPath}[/]");
                AnsiConsole.MarkupLine($"[green]✓[/] Project '[yellow]{settings.Name}[/]' initialized with target '[yellow]{settings.Target}[/]'");

                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to create ox.json: {ex.Message}[/]");
                return 1;
            }
        }
    }
}