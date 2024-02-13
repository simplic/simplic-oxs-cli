using CommonServiceLocator;
using Simplic.Studio.Ox;
using Spectre.Console;

namespace simplic_oxs_cli
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            AnsiConsole.WriteLine("Hello, World!");

            Util.InitializeFramework();
            Util.AddModule<Simplic.PlugIn.Studio.Ox.Server.FrameworkEntryPoint>();
            Util.InitializeOxS();

            var selConn = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select Connection String")
                    .AddChoices("Default", "Custom")
            );

            string connectionString;
            if (selConn == "Default")
                connectionString = "UID=admin;PWD=school;Server=sc-dev02;dbn=simplic;ASTART=No;links=tcpip";
            else
                connectionString = AnsiConsole.Prompt(new TextPrompt<string>("Input connection string"));

            Util.SetConnectionString(connectionString);

            var tenantService = ServiceLocator.Current.GetInstance<ITenantMapService>();
            AnsiConsole.WriteLine("Getting tenants");
            var tenants = await tenantService.GetStudioMap();
            foreach (var tenant in tenants.Keys)
            {
                Console.WriteLine(tenant);
            }

            AnsiConsole.WriteLine("Getting services");
            foreach (var service in Util.GetAllServices())
            {
                Console.WriteLine(service.ContextName);
            }
        }
    }
}
