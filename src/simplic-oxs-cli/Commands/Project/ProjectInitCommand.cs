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
                    clean = "oxs project clean --build-dir ./.build",
                    build = "oxs project build --build-dir ./.build",
                    deploy = $"oxs project deploy --artifact ./.build/*.zip --section {settings.Section}",
                    install = $"oxs package install --artifact ./.build --section {settings.Section}"
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

                // Create packages directory
                var packagesPath = Path.Combine(projectPath, "packages");
                if (!Directory.Exists(packagesPath))
                {
                    try
                    {
                        Directory.CreateDirectory(packagesPath);
                        AnsiConsole.MarkupLine($"[green]✓[/] packages directory created: [yellow]{packagesPath}[/]");
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: Failed to create packages directory: {ex.Message}[/]");
                    }
                }

                // Create .gitignore
                var gitignorePath = Path.Combine(projectPath, ".gitignore");
                var gitignoreContent = @"# Ignore build artifacts
./.build/**
*.zip
";
                try
                {
                    await File.WriteAllTextAsync(gitignorePath, gitignoreContent, cancellationToken);
                    AnsiConsole.MarkupLine($"[green]✓[/] .gitignore created: [yellow]{gitignorePath}[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to create .gitignore: {ex.Message}[/]");
                }

                // Create README.md
                var readmePath = Path.Combine(projectPath, "README.md");
                var readmeContent = $@"# {settings.Name}

{settings.Description}

## Project Structure

This is an OX project with the following structure:

### Files and Directories

- **`ox.json`** - Project configuration file containing:
  - Project metadata (name, version, description)
  - Build scripts (clean, build, deploy)
  - Target platform configuration

- **`packages/`** - Directory containing your package definitions
  - Each subdirectory represents a package
  - Each package should contain a `manifest.json` file
  - Additional package files (scripts, configurations, etc.)

- **`.build/`** - Build output directory (created during build)
  - Contains generated ZIP packages
  - Automatically created by the build process

### Build Scripts

The project includes predefined scripts in `ox.json`:

```bash
# Clean build artifacts
oxs project run clean

# Build all packages
oxs project run build

# Deploy built packages
oxs project run deploy

# Run all scripts in sequence
oxs project run
```

### Getting Started

1. Add your packages in the `packages/` directory
2. Each package needs a `manifest.json` with package metadata
3. Run `oxs project run` to clean, build, and deploy

### Package Structure

Each package in the `packages/` directory should follow this structure:

```
packages/
├── your-package-name/
│   ├── manifest.json      # Package metadata
│   ├── script.py          # Your package files
│   └── config.json        # Additional configurations
```

### Manifest Example

```json
{{
  ""id"": ""your-package-name"",
  ""version"": ""1.0.0"",
  ""description"": ""Package description"",
  ""type"": ""lambda"",
  ""runtime"": ""python3.9""
}}
```
";

                try
                {
                    await File.WriteAllTextAsync(readmePath, readmeContent, cancellationToken);
                    AnsiConsole.MarkupLine($"[green]✓[/] README.md created: [yellow]{readmePath}[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to create README.md: {ex.Message}[/]");
                }

                AnsiConsole.MarkupLine($"[green]✓[/] Project '[yellow]{settings.Name}[/]' initialized with target '[yellow]{settings.Target}[/]'");
                AnsiConsole.MarkupLine("[blue]Next steps:[/]");
                AnsiConsole.MarkupLine("  1. Add packages to the [yellow]packages/[/] directory");
                AnsiConsole.MarkupLine("  2. Run [yellow]oxs project run[/] to build and deploy");

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