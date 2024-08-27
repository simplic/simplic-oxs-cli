using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;

namespace Simplic.OxS.CLI.Identity.Modules
{
    public class OxOrganizationModule : IAsyncModule<IOxOrganizationSettings>
    {
        public Task Execute(IOxOrganizationSettings settings)
        {
            var id = settings.OrganizationId ?? Interactive.EnterOrganizationId();

            return AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Selecting organization[/]", context => settings.AuthClient!.LoginOrganization(id));
        }

    }
}
