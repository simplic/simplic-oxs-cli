using Microsoft.Win32;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Security.Principal;

namespace oxs.Commands.Configure;

/// <summary>
/// Command for managing the OXS CLI in the system or user PATH environment variable.
/// </summary>
public class ConfigurePathCommand : Command<ConfigurePathSettings>
{
    /// <summary>
    /// Executes the configure path command with the specified settings.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The configuration settings for PATH management.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An exit code indicating success (0) or failure (1).</returns>
    public override int Execute(CommandContext context, ConfigurePathSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously executes the PATH configuration process, adding or removing the CLI directory from the system or user PATH.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The configuration settings for PATH management.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with an exit code indicating success (0) or failure (1).</returns>
    private async Task<int> ExecuteAsync(CommandContext context, ConfigurePathSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Get the current executable's directory
            var currentExecutablePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExecutablePath))
            {
                AnsiConsole.MarkupLine("[red]Unable to determine current executable path[/]");
                return 1;
            }

            var cliDirectory = Path.GetDirectoryName(currentExecutablePath);
            if (string.IsNullOrEmpty(cliDirectory))
            {
                AnsiConsole.MarkupLine("[red]Unable to determine CLI directory[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[blue]CLI Directory:[/] [yellow]{cliDirectory}[/]");

            // Determine target (system or user PATH)
            var target = settings.User ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Machine;
            var targetName = settings.User ? "user" : "system";

            // Check if running as administrator for system PATH
            if (!settings.User && !IsRunningAsAdministrator())
            {
                AnsiConsole.MarkupLine("[red]Administrator privileges required to modify system PATH.[/]");
                AnsiConsole.MarkupLine("[yellow]Run the command with --user flag to modify user PATH instead, or run as administrator.[/]");
                return 1;
            }

            // Get current PATH
            var currentPath = Environment.GetEnvironmentVariable("PATH", target) ?? string.Empty;
            var pathEntries = currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Check if CLI directory is already in PATH
            var cliDirectoryInPath = pathEntries.Any(entry => 
                string.Equals(entry.Trim(), cliDirectory, StringComparison.OrdinalIgnoreCase));

            if (settings.Remove)
            {
                if (!cliDirectoryInPath)
                {
                    AnsiConsole.MarkupLine($"[yellow]CLI directory is not in {targetName} PATH[/]");
                    return 0;
                }

                // Remove CLI directory from PATH
                pathEntries.RemoveAll(entry => 
                    string.Equals(entry.Trim(), cliDirectory, StringComparison.OrdinalIgnoreCase));

                var newPath = string.Join(";", pathEntries);
                Environment.SetEnvironmentVariable("PATH", newPath, target);

                // Refresh environment variables for current process
                RefreshEnvironmentPath();

                AnsiConsole.MarkupLine($"[green]?[/] CLI removed from {targetName} PATH");
                AnsiConsole.MarkupLine("[yellow]Note: You may need to restart your terminal for changes to take effect[/]");
            }
            else
            {
                if (cliDirectoryInPath)
                {
                    AnsiConsole.MarkupLine($"[yellow]CLI directory is already in {targetName} PATH[/]");
                    return 0;
                }

                // Add CLI directory to PATH
                pathEntries.Add(cliDirectory);
                var newPath = string.Join(";", pathEntries);
                Environment.SetEnvironmentVariable("PATH", newPath, target);

                // Refresh environment variables for current process
                RefreshEnvironmentPath();

                AnsiConsole.MarkupLine($"[green]?[/] CLI added to {targetName} PATH");
                AnsiConsole.MarkupLine("[yellow]Note: You may need to restart your terminal for changes to take effect[/]");
            }

            // Verify the change
            var updatedPath = Environment.GetEnvironmentVariable("PATH", target) ?? string.Empty;
            var updatedEntries = updatedPath.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var isNowInPath = updatedEntries.Any(entry => 
                string.Equals(entry.Trim(), cliDirectory, StringComparison.OrdinalIgnoreCase));

            if (settings.Remove && isNowInPath)
            {
                AnsiConsole.MarkupLine("[red]Warning: CLI directory still appears to be in PATH[/]");
            }
            else if (!settings.Remove && !isNowInPath)
            {
                AnsiConsole.MarkupLine("[red]Warning: CLI directory does not appear to be in PATH[/]");
            }

            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            AnsiConsole.MarkupLine("[red]Access denied. Administrator privileges required to modify system PATH.[/]");
            AnsiConsole.MarkupLine("[yellow]Try using --user flag to modify user PATH instead, or run as administrator.[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error modifying PATH: {ex.Message}[/]");
            return 1;
        }
    }

    /// <summary>
    /// Determines whether the current process is running with administrator privileges.
    /// </summary>
    /// <returns>True if running as administrator; otherwise, false.</returns>
    private bool IsRunningAsAdministrator()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Refreshes the PATH environment variable for the current process by combining machine and user PATH variables.
    /// </summary>
    private void RefreshEnvironmentPath()
    {
        try
        {
            // Refresh PATH for current process by combining machine and user PATH
            var machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
            var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
            
            var combinedPath = string.IsNullOrEmpty(userPath) ? machinePath : $"{machinePath};{userPath}";
            Environment.SetEnvironmentVariable("PATH", combinedPath, EnvironmentVariableTarget.Process);
        }
        catch
        {
            // Ignore errors when refreshing PATH for current process
        }
    }
}