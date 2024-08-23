using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Organization.Commands
{
    public class ListOrganizationsCommand : IAsyncCommand<ListOrganizationsCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var client = new OrganizationClient(settings.AuthClient!.HttpClient);
            var organizations = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Listing organizations[/]", _ => client.GetForUserAsync());

            foreach (var organization in organizations)
                AnsiConsole.MarkupLineInterpolated($"[gray]{organization.OrganizationId}[/] - [blue]{organization.OrganizationName}[/]");

            return 0;
        }

        public interface ISettings : IOxSettings
        {
        }
    }
}
