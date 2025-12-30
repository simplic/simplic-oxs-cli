using oxs.Commands.Configure;
using oxs.Commands.Package;
using oxs.Commands.Project;
using Spectre.Console.Cli;

namespace oxs;

/// <summary>
/// Main program class for the OXS CLI application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Entry point for the OXS CLI application. Configures and runs the command-line interface.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>An exit code indicating success (0) or failure (non-zero).</returns>
    public static int Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            RegisterConfigure.RegisterCommands(config);
            RegisterProject.RegisterCommands(config);
            RegisterPackage.RegisterCommands(config);
        });

        return app.Run(args);
    }
}


