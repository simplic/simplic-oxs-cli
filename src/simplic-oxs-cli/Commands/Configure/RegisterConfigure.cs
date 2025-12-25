using oxs.Commands.Http;
using Spectre.Console.Cli;

namespace oxs.Commands.Configure
{
    internal static class RegisterConfigure
    {
        public static void RegisterCommands(IConfigurator config)
        {
            config.AddBranch<ConfigureSettings>("configure", add =>
            {
                add.AddCommand<ConfigureEnvCommand>("env");
            });

            config.AddCommand<HttpCommand>("http");

            config.AddBranch<Flow.FlowSettings>("flow", add =>
            {
                add.AddCommand<Flow.FlowPushNodeDefCommand>("push-node-def");
            });
        }
    }
}
