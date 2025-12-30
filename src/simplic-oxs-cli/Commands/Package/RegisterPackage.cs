using Spectre.Console.Cli;

namespace oxs.Commands.Package;

/// <summary>
/// Provides registration methods for package management commands in the CLI application.
/// </summary>
internal static class RegisterPackage
{
    /// <summary>
    /// Registers all package management commands with the CLI configurator.
    /// </summary>
    /// <param name="config">The configurator to register commands with.</param>
    public static void RegisterCommands(IConfigurator config)
    {
        config.AddBranch<PackageSettings>("package", add =>
        {
            add.AddCommand<PackageInstallCommand>("install");
        });
    }
}
