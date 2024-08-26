using CommonServiceLocator;
using Simplic.OxS.CLI.CDN;
using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Datahub;
using Simplic.OxS.CLI.Identity;
using Simplic.OxS.CLI.Organization;
using Simplic.OxS.CLI.Studio;
using Spectre.Console;
using System.Reflection;
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

                container.RegisterInstance(new SettingsGenerator(new AssemblyName("Simplic.OxS.CLI.DynamicSettings")));

                var registry = new CommandRegistry();
                registry.Add<CdnCommandGroup>();
                registry.Add<IdentityCommandGroup>();
                registry.Add<OrganizationCommandGroup>();
                registry.Add<StudioCommandGroup>();
                registry.Add<DatahubCommandGroup>();
                await registry.RunAsync(args, container);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Simplix Ox CLI crashed![/]");
                AnsiConsole.WriteException(ex);
            }
        }
    }
}
