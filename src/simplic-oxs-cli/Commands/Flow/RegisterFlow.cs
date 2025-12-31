using oxs.Commands.Http;
using Spectre.Console.Cli;

namespace oxs.Commands.Configure;

/// <summary>
/// Provides registration methods for configuration-related commands in the CLI application.
/// </summary>
internal static class RegisterFlow
{
    /// <summary>
    /// Registers all configuration commands including configure, http, and flow commands with the CLI configurator.
    /// </summary>
    /// <param name="config">The configurator to register commands with.</param>
    public static void RegisterCommands(IConfigurator config)
    {
        config.AddBranch<Flow.FlowSettings>("flow", add =>
        {
            add.AddCommand<Flow.FlowPushNodeDefCommand>("push-node-def");
        });
    }
}
