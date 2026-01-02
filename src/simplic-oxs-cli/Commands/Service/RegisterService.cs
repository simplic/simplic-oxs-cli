using Spectre.Console.Cli;

namespace oxs.Commands.Service;

/// <summary>
/// Register service commands
/// </summary>
internal static class RegisterService
{
    /// <summary>
    /// Registers all service commands with the CLI configurator.
    /// </summary>
    /// <param name="config">The configurator to register commands with.</param>
    public static void RegisterCommands(IConfigurator config)
    {
        config.AddBranch("service", service =>
        {
            service.AddCommand<ServiceGetDefinitionCommand>("get-definition");
        });
    }
}
