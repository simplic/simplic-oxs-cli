using oxs.Commands.Http;
using Spectre.Console.Cli;

namespace oxs.Commands.Configure;

/// <summary>
/// Register http commands
/// </summary>
internal static class RegisterHttp
{
    /// <summary>
    /// Registers all configuration commands including configure, http, and flow commands with the CLI configurator.
    /// </summary>
    /// <param name="config">The configurator to register commands with.</param>
    public static void RegisterCommands(IConfigurator config)
    {
        config.AddCommand<HttpCommand>("http");
    }
}
