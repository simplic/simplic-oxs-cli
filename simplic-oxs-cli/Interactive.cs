using CommonServiceLocator;
using Simplic.Studio.Ox;
using Spectre.Console;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Simplic.Ox.CLI
{
    internal static class Interactive
    {
        private const string DefaultConnectionString = "dbn=Simplic;server=Local;charset=UTF-8;Links=TCPIP;UID=admin;PWD=school";

        private static async Task<Client> Login(Uri? uri, string? email, string? password)
        {
            AnsiConsole.MarkupLine("[magenta]OxS login[/]");
            uri ??= EnterUri();
            email ??= EnterEmail();
            password ??= EnterPassword();

            var client = new Client(uri, email, password);
            await client.Login();
            return client;
        }

        private async static Task<OrganizationMemberModel> SelectOrganization(Client client)
        {
            var organizations = await client.ListOrganizations();
            var organization = AnsiConsole.Prompt(
                new SelectionPrompt<OrganizationMemberModel>()
                    .Title("Select an organization")
                    .WrapAround()
                    .AddChoices(organizations)
                    .UseConverter(o => o.OrganizationName)
            );
            return organization;
        }

        private async static Task<(Guid, string?)> OrganizationAction(Client client, Guid? id, string? name)
        {
            if (id != null)
            {
                var organization = await client.GetOrganizationById(id.Value);
                if(organization != null)
                    return (organization.OrganizationId, organization.OrganizationName);
            }
            if (name != null)
            {
                var organization = await client.GetOrganizationByName(name);
                if (organization != null)
                    return (organization.OrganizationId, organization.OrganizationName);
            }
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .Title("Select an action")
                .WrapAround()
                .AddChoices(
                    "Create temporary OxS organization",
                    "Use existing OxS organization",
                    "Delete temporary OxS organization"
                )
            );

            if (selection == "Create temporary OxS organization")
            {
                name = EnterName();
                var oxsTenant = await OxManager.CreateDummyOrganization(client, name);
                return (oxsTenant.Id, oxsTenant.Name);
            }
            else if (selection == "Use existing OxS organization")
            {
                var organization = await SelectOrganization(client);
                return (organization.OrganizationId, organization.OrganizationName);
            }
            else
            {
                var organization = await SelectOrganization(client);
                var ok = AnsiConsole.Confirm($"[red]Delete[/] [gray]{organization.OrganizationId}?[/] - [yellow]{organization.OrganizationName}[/]", false);
                if (ok)
                    await OxManager.DeleteDummyOrganization(client, organization.OrganizationId);

                return (Guid.Empty, null);
            }
        }

        private async static Task<Guid?> OrganizationActions(Client client, Guid? id, string? name)
        {
            var tenantMapService = ServiceLocator.Current.GetInstance<ITenantMapService>();

            Guid oxsId = Guid.Empty;
            string? oxsName = null;
            while (oxsName == null)
                (oxsId, oxsName) = await OrganizationAction(client, id, name);

            AnsiConsole.WriteLine("Selecting organization");
            await client.LoginOrganization(oxsId);
            AnsiConsole.MarkupLine($"[green]Successfully selected[/] [yellow]{oxsName}[/]");

            return await tenantMapService.GetByOxSTenant(oxsId);
        }

        private static void LoadModules(string dllPath)
        {
            var numDlls = Plugins.CountDlls();
            if (AnsiConsole.Confirm($"Download {numDlls} Dlls to {dllPath}", !Directory.Exists(dllPath)))
            {
                if (Directory.Exists(dllPath))
                    Directory.Delete(dllPath, true);
                Directory.CreateDirectory(dllPath);
                AnsiConsole.Progress().Start(progress =>
                {
                    var task = progress.AddTask("Downloading Dlls");

                    var i = 0;
                    foreach (var dll in Plugins.DownloadDlls())
                    {
                        File.WriteAllBytes(Path.Join(dllPath, dll.Name + ".dll"), dll.Content);
                        i++;
                        task.Value = 100 * i / numDlls;
                    }
                });
            }

            var paths = new List<string> {
                dllPath,
                "C:\\Users\\m.bergmann\\source\\repos\\simplic-framework\\src\\Simplic.Main\\bin\\Debug",
                RuntimeEnvironment.GetRuntimeDirectory()
            };
            var plugins = Plugins.Scan(paths, Plugins.GetAllDlls(dllPath))
                                 .Select(p => p.Name)
                                 .Where(n => n is not null)
                                 .Select(n => n!);

            var pluginsToLoad = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                .Title("Select Plugins to load")
                .AddChoices(plugins)
                .Required(false));

            AnsiConsole.MarkupLine("[bold]Selected plugins:[/]");
            foreach (var plugin in pluginsToLoad)
                AnsiConsole.MarkupLineInterpolated($"[yellow]{plugin}[/]");

            foreach (var plugin in pluginsToLoad)
            {
                var assembly = Assembly.Load(plugin);
                Plugins.RegisterAllModules(assembly);
            }

            Plugins.InitializeAllModules();
        }

        private async static Task UploadData(IInstanceDataUploadService service, ISharedIdRepository sharedIdRepository, string authToken, Guid instanceId, Guid tenantId)
        {
            var sharedId = await sharedIdRepository.GetSharedIdByStudioId(instanceId, service.ContextName, tenantId);

            // Call create method when either no entry for a shared id is found or the oxs id in empty.
            if (sharedId == null || sharedId.OxSId == Guid.Empty)
            {
                var oxsId = await service.CreateUpload(instanceId, authToken);
                if (oxsId == default)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Data not uploaded:[/] context={service.ContextName}  id=[gray]{instanceId}[/]  tenant=[gray]{tenantId}[/]");
                    return;
                }

                sharedId = new SharedId
                {
                    InstanceDataId = instanceId,
                    OxSId = oxsId,
                    Context = service.ContextName,
                    TenantId = tenantId
                };

                await sharedIdRepository.Save(sharedId);
                return;
            }
            await service.UpdateUpload(sharedId.InstanceDataId, sharedId.OxSId, authToken);
        }

        private async static Task SyncData(Guid tenantId, string authToken)
        {
            var sharedIdRepository = ServiceLocator.Current.GetInstance<ISharedIdRepository>();

            AnsiConsole.WriteLine("Getting services");
            var services = Util.GetAllServices().ToList();
            var serviceNames = services.Select(s => s.ContextName).ToList();

            var servicesToSync = new List<string>();
            uint order = 1;
            while (serviceNames.Count > 0)
            {
                var serviceToSync = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title("Select context to synchronise")
                    .AddChoices("-- Done --")
                    .AddChoices(serviceNames));

                if (serviceToSync == "-- Done --")
                    break;

                try
                {
                    var service = services.First(s => s.ContextName == serviceToSync);
                    AnsiConsole.MarkupLineInterpolated($"[bold]Selected context:[/] [gray]{order} ->[/] [yellow]{serviceToSync}[/]");
                    var instanceIds = (await service.GetAllInstanceDataIds(tenantId)).ToList();
                    var count = instanceIds.Count;
                    AnsiConsole.MarkupLineInterpolated($"Synchronizing [cyan]{count}[/] items");

                    await AnsiConsole.Progress().StartAsync(async progress =>
                    {
                        var task = progress.AddTask("Synchronizing");

                        var i = 0;
                        foreach (var instanceId in instanceIds)
                        {
                            try
                            {
                                await UploadData(service, sharedIdRepository, authToken, instanceId, tenantId);
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.WriteException(ex);
                            }
                            i++;
                            task.Value = 100 * i / count;
                        }
                        task.StopTask();
                    });

                    serviceNames.Remove(serviceToSync);
                    servicesToSync.Add(serviceToSync);
                    order += 1;
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
            }
        }

        public async static Task Run(Program.RootCommand.Settings settings)
        {
            var dllPath = "./.simplic/bin";

            Util.InitializeOx();

            using var client = await Login(settings.Uri, settings.Email, settings.Password);
            var oxsId = await OrganizationActions(client, settings.Id, settings.Name);

            // The user cancelled login
            if (!oxsId.HasValue)
                return;

            if (client.Token == null)
                return;

            LoadModules(dllPath);

            await SyncData(oxsId.Value, client.Token);
        }

        public static void SelectConnectionString()
        {
            var selConn = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select Connection String")
                    .AddChoices("Default", "Custom")
            );

            string connectionString;
            if (selConn == "Default")
                connectionString = DefaultConnectionString;
            else
                connectionString = AnsiConsole.Prompt(new TextPrompt<string>("Input connection string"));

            Util.SetConnectionString(connectionString);
        }

        public static Uri EnterUri() => new(AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter uri[/]      [gray]>[/]")));
        public static string EnterEmail() => AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter email[/]    [gray]>[/]"));
        public static string EnterPassword() => AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter password[/] [gray]>[/]").Secret('ö'));
        public static string EnterName() => AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter name[/]     [gray]>[/]"));
    }
}
