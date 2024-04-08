using CommonServiceLocator;
using Simplic.Framework.Core;
using Simplic.Studio.Ox;
using Spectre.Console;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Simplic.Ox.CLI
{
    internal static class Interactive
    {
        private async static Task<Guid> SelectTenant(ITenantMapService tenantMapService, TenantSystem.IOrganizationService tenantService)
        {
            AnsiConsole.WriteLine("Getting tenants");
            var tenants = await tenantMapService.GetStudioMap();
            var tenantGuids = tenants.Keys.ToList();
            var tenantNames = tenantGuids.Select(t => tenantService.Get(t).Name).ToList();

            var tenantIndex = AnsiConsole.Prompt(
                new SelectionPrompt<int>()
                .Title("Select studio tenant")
                .AddChoices(Enumerable.Range(0, tenantNames.Count))
                .UseConverter(t => tenantNames[t]));

            AnsiConsole.MarkupLineInterpolated($"[bold]Selected tenant:[/] [yellow]{tenantNames[tenantIndex]}[/]");

            return tenantGuids[tenantIndex];
        }

        private async static Task<(string, Guid)> LoginOxS(HttpClient httpClient)
        {
            var tenantMapService = ServiceLocator.Current.GetInstance<ITenantMapService>();
            var tenantService = ServiceLocator.Current.GetInstance<TenantSystem.IOrganizationService>();

            AnsiConsole.MarkupLine("[magenta]OxS login[/]");
            var url = AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter url[/]      [gray]>[/]"));
            var email = AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter email[/]    [gray]>[/]"));
            var pass = AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter password[/] [gray]>[/]").Secret());

            httpClient.BaseAddress = new Uri(url);
            var client = new Client(httpClient, email, pass);

            await client.Login();

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .AddChoices("Create temporary OxS organization", "Use existing OxS organization"));
            var newTenantId = Guid.NewGuid();
            var organizationId = Guid.Empty;
            var organizationName = $"({newTenantId:N})";
            if (selection == "Create temporary OxS organization")
            {
                organizationName = "Test::Ox" + AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Name[/]      [gray]>[/]")) + organizationName;
                var organization = await client.CreateOrganization(organizationName, new AddressModel
                {
                    City = "Nowhere",
                    Street = "Middle",
                    HouseNumber = "-1",
                    ZipCode = "12345",
                    CountryIso = "any",
                });
                organizationId = organization.Id;
            }
            else
            {
                var organizations = await client.ListOrganizations();
                var organization = AnsiConsole.Prompt(
                    new SelectionPrompt<OrganizationMemberModel>()
                    .AddChoices(organizations)
                    .UseConverter(o => o.OrganizationName));
                organizationName = organization.OrganizationName;
                organizationId = organization.OrganizationId;
            }

            AnsiConsole.WriteLine("Switching organization context");
            var token = await client.LoginOrganization(organizationId);
            AnsiConsole.MarkupLine($"[green]Selected {organizationName}[/]");

            var tenantId = await tenantMapService.GetByOxSTenant(organizationId);
            if (tenantId == default)
            {
                AnsiConsole.WriteLine("Creating studio tenant");
                tenantId = newTenantId;
                TenantManager.Singleton.Save(new Tenant
                {
                    Id = tenantId,
                    Name = organizationName,
                    ConnectionId = null,
                    ExternId = null,
                    ExternName = null,
                    ExternSystemType = null,
                });
                tenantService.Save(new TenantSystem.Organization
                {
                    Id = tenantId,
                    Name = organizationName,
                    OAuthAppId = null,
                    OAuthTenantId = null,
                    OAuthRedirect = null,
                    CloudOrganizationId = null,
                    CloudQueueId = null,
                    IsActive = true,
                });
                await tenantMapService.Save(new TenantMap
                {
                    OxSTenantId = organizationId,
                    StudioTenantId = tenantId,
                });
            }

            return (token, tenantId);
        }

        private static void LoadModules(string dllPath)
        {
            var numDlls = Util.CountDlls();
            if (AnsiConsole.Confirm($"Download {numDlls} Dlls to {dllPath}", !Directory.Exists(dllPath)))
            {
                if (Directory.Exists(dllPath))
                    Directory.Delete(dllPath, true);
                Directory.CreateDirectory(dllPath);
                AnsiConsole.Progress().Start(progress =>
                {
                    var task = progress.AddTask("Downloading Dlls");

                    var i = 0;
                    foreach (var dll in Util.DownloadDlls())
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

        internal async static Task Run()
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
            Util.RegisterAssemblyLoader(Path.GetFullPath(dllPath));
            Util.RegisterAssemblyLoader("C:\\Users\\m.bergmann\\source\\repos\\simplic-framework\\src\\Simplic.Main\\bin\\Debug");

            Util.InitializeOx();

            using var httpClient = new HttpClient();
            var (authToken, tenantId) = await LoginOxS(httpClient);

            LoadModules(dllPath);

            await SyncData(tenantId, authToken);

            if (AnsiConsole.Confirm("Save current configuraiton"))
            {

            }
        }
    }
}
