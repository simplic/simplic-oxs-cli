using Spectre.Console.Cli;

namespace oxs.Commands.Project;

/// <summary>
/// Provides registration methods for project management commands in the CLI application.
/// </summary>
internal static class RegisterProject
{
    /// <summary>
    /// Registers all project management commands with the CLI configurator.
    /// </summary>
    /// <param name="config">The configurator to register commands with.</param>
    public static void RegisterCommands(IConfigurator config)
    {
        config.AddBranch<ProjectSettings>("project", add =>
        {
            add.AddCommand<ProjectInitCommand>("init");
            add.AddCommand<ProjectBuildCommand>("build");
            add.AddCommand<ProjectCleanCommand>("clean");
            add.AddCommand<ProjectDeployCommand>("deploy");
            add.AddCommand<ProjectRunCommand>("run");
        });
    }
}