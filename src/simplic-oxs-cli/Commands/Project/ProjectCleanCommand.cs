using Spectre.Console;
using Spectre.Console.Cli;

namespace oxs.Commands.Project;

public class ProjectCleanCommand : Command<ProjectCleanSettings>
{
    public override int Execute(CommandContext context, ProjectCleanSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(CommandContext context, ProjectCleanSettings settings, CancellationToken cancellationToken)
    {
        var buildDirectory = Path.GetFullPath(settings.BuildDirectory ?? "./.build");

        AnsiConsole.MarkupLine($"[blue]Cleaning build directory:[/] [yellow]{buildDirectory}[/]");

        try
        {
            if (Directory.Exists(buildDirectory))
            {
                // Get all files and directories in the build directory
                var files = Directory.GetFiles(buildDirectory, "*", SearchOption.AllDirectories);
                var directories = Directory.GetDirectories(buildDirectory, "*", SearchOption.AllDirectories);

                AnsiConsole.MarkupLine($"[blue]Found {files.Length} file(s) and {directories.Length} directory(ies) to remove[/]");

                // Delete all files
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: Could not delete file {file}: {ex.Message}[/]");
                    }
                }

                // Delete all directories (in reverse order to delete children first)
                foreach (var directory in directories.OrderByDescending(d => d.Length))
                {
                    try
                    {
                        Directory.Delete(directory);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: Could not delete directory {directory}: {ex.Message}[/]");
                    }
                }

                // Try to delete the build directory itself if it's empty
                try
                {
                    if (!Directory.GetFileSystemEntries(buildDirectory).Any())
                    {
                        Directory.Delete(buildDirectory);
                        AnsiConsole.MarkupLine("[green]✓[/] Build directory removed completely");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[green]✓[/] Build directory cleaned (some files/directories could not be removed)");
                    }
                }
                catch
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Build directory cleaned");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Build directory does not exist - nothing to clean[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error cleaning build directory: {ex.Message}[/]");
            return 1;
        }
    }
}