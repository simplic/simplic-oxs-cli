using CommonServiceLocator;
using Simplic.Studio.Ox;
using Spectre.Console;

namespace Simplic.Ox.CLI
{
    internal static class Interactive
    {
        private const string LocalConnectionString = "UID=admin;PWD=school;Server=Local;dbn=DocCenter;charset=UTF-8;Links=TCPIP";

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
                if (organization != null)
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

        public async static Task<Guid?> OrganizationActions(Client client, Guid? id, string? name)
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

        public static List<string> SelectPlugins(IEnumerable<string> paths)
        {
            var plugins = Plugins.Scan(paths).Where(p => p.Name is not null).Select(p => p.Name!);

            return AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                .Title("Select Plugins to load")
                .AddChoices(plugins)
                .Required(false));
        }

        public async static Task SyncData(Guid tenantId, string authToken)
        {
            var sharedIdRepository = ServiceLocator.Current.GetInstance<ISharedIdRepository>();

            AnsiConsole.WriteLine("Getting services");
            var services = Util.GetUploadServices().ToList();
            var contexts = services.Select(s => s.ContextName).ToList();

            var contextsToSync = new List<string>();
            uint order = 1;
            while (contexts.Count > 0)
            {
                var context = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title("Select context to synchronise")
                    .AddChoices("-- Start Sync --")
                    .AddChoices(contexts));

                if (context == "-- Start Sync --")
                    break;

                try
                {
                    AnsiConsole.MarkupLineInterpolated($"[bold]Selected context:[/] [gray]{order} ->[/] [yellow]{context}[/]");
                    contexts.Remove(context);
                    contextsToSync.Add(context);
                    order += 1;
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
            }

            bool rerun = false;
            do
            {
                await Util.SynchronizeContexts(contextsToSync, tenantId, authToken);
                rerun = AnsiConsole.Confirm("Rerun sync", rerun);
            } while (rerun);
        }

        public static string SelectConnectionString()
        {
            var selConn = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select Connection String")
                    .AddChoices("Local", "Custom")
            );

            if (selConn == "Local")
                return LocalConnectionString;
            else
                return AnsiConsole.Ask<string>("Input connection string");
        }

        public static Uri EnterUri() => new(AnsiConsole.Ask<string>("[bold magenta]Enter uri[/]      [gray]>[/]"));
        public static string EnterEmail() => AnsiConsole.Ask<string>("[bold magenta]Enter email[/]    [gray]>[/]");
        public static string EnterPassword() => AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter password[/] [gray]>[/]").Secret('ö'));
        public static string EnterName() => AnsiConsole.Ask<string>("[bold magenta]Enter name[/]     [gray]>[/]");
    }
}
