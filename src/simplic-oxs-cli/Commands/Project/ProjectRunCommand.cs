using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Text.Json;

namespace oxs.Commands.Project;

/// <summary>
/// Command for executing scripts defined in the ox.json file of an OXS project.
/// </summary>
public class ProjectRunCommand : Command<ProjectRunSettings>
{
    /// <summary>
    /// Executes the project run command with the specified settings.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The project run settings including script name and options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An exit code indicating success (0) or failure (1).</returns>
    public override int Execute(CommandContext context, ProjectRunSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously executes the project run process, parsing ox.json and executing the specified or all scripts.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The project run settings.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with an exit code indicating success (0) or failure (1).</returns>
    private async Task<int> ExecuteAsync(CommandContext context, ProjectRunSettings settings, CancellationToken cancellationToken)
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

        try
        {
            // Read and parse ox.json
            var oxJsonContent = await File.ReadAllTextAsync(oxJsonPath, cancellationToken);
            using var oxJsonDoc = JsonDocument.Parse(oxJsonContent);
            var oxJsonRoot = oxJsonDoc.RootElement;

            // Extract scripts section
            if (!oxJsonRoot.TryGetProperty("scripts", out var scriptsElement))
            {
                AnsiConsole.MarkupLine("[red]No 'scripts' section found in ox.json[/]");
                return 1;
            }

            var scripts = new Dictionary<string, string>();
            
            foreach (var script in scriptsElement.EnumerateObject())
            {
                if (script.Value.ValueKind == JsonValueKind.String)
                {
                    scripts[script.Name] = script.Value.GetString() ?? "";
                }
            }

            if (scripts.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No scripts found in ox.json[/]");
                return 0;
            }

            // If --list flag is used, list all available scripts
            if (settings.List)
            {
                AnsiConsole.MarkupLine("[blue]Available scripts in ox.json:[/]");
                foreach (var script in scripts)
                {
                    AnsiConsole.MarkupLine($"  [yellow]{script.Key}[/]: [dim]{script.Value.EscapeMarkup()}[/]");
                }
                return 0;
            }

            // If no script specified, run all scripts in sequence
            if (string.IsNullOrWhiteSpace(settings.Script))
            {
                AnsiConsole.MarkupLine($"[blue]Running all scripts in sequence ({scripts.Count} scripts found):[/]");
                
                var overallSuccess = true;
                var completedScripts = 0;
                
                foreach (var script in scripts)
                {
                    AnsiConsole.MarkupLine($"[blue]Running script '[yellow]{script.Key}[/]':[/]");
                    AnsiConsole.WriteLine($"  Command: {script.Value}");
                    AnsiConsole.WriteLine();

                    var scriptExitCode = await ExecuteCommandAsync(script.Value, currentDirectory, cancellationToken);

                    AnsiConsole.WriteLine();
                    if (scriptExitCode == 0)
                    {
                        completedScripts++;
                        AnsiConsole.MarkupLine($"[green]?[/] Script '[yellow]{script.Key}[/]' completed successfully");
                    }
                    else
                    {
                        overallSuccess = false;
                        AnsiConsole.MarkupLine($"[red]?[/] Script '[yellow]{script.Key}[/]' failed with exit code {scriptExitCode}");
                        AnsiConsole.MarkupLine("[yellow]Stopping execution of remaining scripts due to failure[/]");
                        break;
                    }
                    
                    AnsiConsole.WriteLine(); // Add spacing between scripts
                }
                
                // Summary
                AnsiConsole.MarkupLine($"[blue]Script execution completed![/]");
                AnsiConsole.MarkupLine($"[green]Successfully completed:[/] {completedScripts} of {scripts.Count} scripts");
                
                return overallSuccess ? 0 : 1;
            }

            // Find the requested script
            if (!scripts.TryGetValue(settings.Script, out var commandToRun))
            {
                AnsiConsole.MarkupLine($"[red]Script '{settings.Script}' not found in ox.json[/]");
                AnsiConsole.MarkupLine("[blue]Available scripts:[/]");
                foreach (var script in scripts)
                {
                    AnsiConsole.MarkupLine($"  [yellow]{script.Key}[/]: [dim]{script.Value.EscapeMarkup()}[/]");
                }
                return 1;
            }

            // Execute the script
            AnsiConsole.MarkupLine($"[blue]Running script '[yellow]{settings.Script}[/]':[/]");
            AnsiConsole.WriteLine($"  Command: {commandToRun}");
            AnsiConsole.WriteLine();

            var exitCode = await ExecuteCommandAsync(commandToRun, currentDirectory, cancellationToken);

            AnsiConsole.WriteLine();
            if (exitCode == 0)
            {
                AnsiConsole.MarkupLine($"[green]?[/] Script '[yellow]{settings.Script}[/]' completed successfully");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]?[/] Script '[yellow]{settings.Script}[/]' failed with exit code {exitCode}");
            }

            return exitCode;
        }
        catch (JsonException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error parsing ox.json: {ex.Message}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error reading ox.json: {ex.Message}[/]");
            return 1;
        }
    }

    /// <summary>
    /// Asynchronously executes a shell command in the specified working directory.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="workingDirectory">The working directory for command execution.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with the exit code of the executed command.</returns>
    private async Task<int> ExecuteCommandAsync(string command, string workingDirectory, CancellationToken cancellationToken)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = GetShellExecutable(),
                Arguments = GetShellArguments(command),
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };

            // Start the process first
            process.Start();
            
            // Handle output and error streams after starting the process
            var outputTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                    {
                        Console.WriteLine(line);
                    }
                }
                catch (Exception)
                {
                    // Ignore exceptions in output reading
                }
            }, cancellationToken);

            var errorTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await process.StandardError.ReadLineAsync()) != null)
                    {
                        Console.Error.WriteLine(line);
                    }
                }
                catch (Exception)
                {
                    // Ignore exceptions in error reading
                }
            }, cancellationToken);

            // Wait for process completion
            await process.WaitForExitAsync(cancellationToken);
            
            // Wait for output tasks to complete
            try
            {
                await Task.WhenAll(outputTask, errorTask);
            }
            catch (Exception)
            {
                // Ignore exceptions in output task completion
            }

            return process.ExitCode;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error executing command: {ex.Message}[/]");
            return 1;
        }
    }

    /// <summary>
    /// Gets the appropriate shell executable for the current operating system.
    /// </summary>
    /// <returns>The shell executable path ('cmd.exe' on Windows, '/bin/bash' on Unix).</returns>
    private static string GetShellExecutable()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT ? "cmd.exe" : "/bin/bash";
    }

    /// <summary>
    /// Formats the command for execution by the appropriate shell on the current operating system.
    /// </summary>
    /// <param name="command">The command to format for shell execution.</param>
    /// <returns>The shell arguments to execute the command.</returns>
    private static string GetShellArguments(string command)
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT 
            ? $"/c {command}" 
            : $"-c \"{command.Replace("\"", "\\\"")}\"";
    }
}