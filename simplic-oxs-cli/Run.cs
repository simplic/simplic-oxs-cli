using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity;
using Simplic.OxS.CLI.Organization;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Globalization;
using Unity;

namespace Simplic.Ox.CLI
{
    public class Run
    {
        public static Task<int> MyMain(string[] args)
        {
            var container = new UnityContainer();

            var registry = new CommandRegistry();
            registry.Add<IdentityCommandGroup>();
            registry.Add<OrganizationCommandGroup>();
            return registry.RunAsync(args, container, config =>
            {
                config.SetExceptionHandler((ex, resolver) => AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything));
                config.SetApplicationCulture(CultureInfo.InvariantCulture);
            });
        }
    }
}
