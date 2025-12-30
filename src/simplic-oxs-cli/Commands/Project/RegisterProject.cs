using Spectre.Console.Cli;

namespace oxs.Commands.Project
{
    internal static class RegisterProject
    {
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
}