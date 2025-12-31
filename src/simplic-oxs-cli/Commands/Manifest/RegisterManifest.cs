using oxs.Commands.Manifest;
using Spectre.Console.Cli;

namespace oxs.Commands.Manifest;

/// <summary>
/// Register manifest commands
/// </summary>
internal static class RegisterManifest
{
    /// <summary>
    /// Registers all manifest commands with the CLI configurator.
    /// </summary>
    /// <param name="config">The configurator to register commands with.</param>
    public static void RegisterCommands(IConfigurator config)
    {
        config.AddBranch("manifest", manifest =>
        {
            manifest.AddCommand<ManifestInitCommand>("init");
            manifest.AddCommand<ManifestAddDeploymentCommand>("add-deployment");
            manifest.AddCommand<ManifestListTemplatesCommand>("list-templates");
        });
    }
}