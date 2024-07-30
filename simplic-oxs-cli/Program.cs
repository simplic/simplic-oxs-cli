using CommonServiceLocator;
using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity;
using Simplic.OxS.CLI.Organization;
using Simplic.OxS.CLI.Studio;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Globalization;
using Unity;
using Unity.ServiceLocation;

namespace Simplic.OxS.CLI
{
    public static class Program
    {
        private async static Task Main(string[] args)
        {
            AnsiConsole.WriteLine("Simplic Ox CLI");

            try
            {
                var container = new UnityContainer();
                var locator = new UnityServiceLocator(container);
                ServiceLocator.SetLocatorProvider(() => locator);
                Util.InitializeContainer(container);

                var registry = new CommandRegistry();
                registry.Add<IdentityCommandGroup>();
                registry.Add<OrganizationCommandGroup>();
                registry.Add<StudioCommandGroup>();
                await registry.RunAsync(args, container, config =>
                {
                    config.SetExceptionHandler((ex, resolver) => AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything));
                    config.SetApplicationCulture(CultureInfo.InvariantCulture);
                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Simplix Ox CLI crashed![/]");
                AnsiConsole.WriteException(ex);
            }
        }
    }
}
