using Spectre.Console.Cli;

namespace oxs.Commands.Package
{
    internal static class RegisterPackage
    {
        public static void RegisterCommands(IConfigurator config)
        {
            config.AddBranch<PackageSettings>("package", add =>
            {
                add.AddCommand<PackageInstallCommand>("install");
            });
        }
    }
}
