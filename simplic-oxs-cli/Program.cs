using CommonServiceLocator;
using Simplic.Studio.Ox;
using Spectre.Console;
using System.IO;
using System.Reflection;

namespace Simplic.Ox.CLI
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            AnsiConsole.WriteLine("Simplic Ox CLI");

            try
            {
                await Run();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }
        }

        private async static Task Run()
        {
            Util.InitializeFramework();
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
            Util.RegisterTypes();

            var dllPath = "./.simplic/bin";
            var numDlls = Util.CountDlls();
            if (AnsiConsole.Confirm($"Saving {numDlls} Dlls to {dllPath}"))
            {
                if (Directory.Exists(dllPath))
                    Directory.Delete(dllPath, true);
                Directory.CreateDirectory(dllPath);
                AnsiConsole.Progress().Start(progress =>
                {
                    var task1 = progress.AddTask("Downloading Dlls");

                    var i = 0;
                    foreach (var dll in Util.DownloadDlls())
                    {
                        File.WriteAllBytes(Path.Join(dllPath, dll.Name + ".dll"), dll.Content);
                        i++;
                        task1.Value = 100 * i / numDlls;
                    }
                });
            }
            Util.RegisterAssemblyLoader("C:\\Users\\m.bergmann\\source\\repos\\simplic-framework\\src\\Simplic.Main\\bin\\Debug");
            Util.RegisterAssemblyLoader(Path.GetFullPath(dllPath));
            var assembly = Assembly.Load("Simplic.PlugIn.ArticleMaster");
            Util.RegisterModule<PlugIn.Studio.Ox.Server.FrameworkEntryPoint>();

            Util.RegisterAllModules(assembly);

            Util.InitializeOx();

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
