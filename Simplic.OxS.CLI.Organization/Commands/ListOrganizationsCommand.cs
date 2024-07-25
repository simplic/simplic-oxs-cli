using Simplic.OxS.CLI.Identity;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Organization.Commands
{
    internal class ListOrganizationsCommand : AsyncCommand<ListOrganizationsCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var client = new OrganizationClient(settings.AuthClient!.HttpClient);
            var organizations = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Listing organizations[/]", _ => client.GetForUserAsync());

            foreach (var organization in organizations)
                AnsiConsole.MarkupLineInterpolated($"[gray]{organization.OrganizationId}[/] - [blue]{organization.OrganizationName}[/]");

            return 0;
        }

        public class Settings : CommandSettings, ILoginSettings
        {
            [CommandOption("-u|--uri <SERVER>")]
            public Uri? Uri { get; init; }

            [CommandOption("-e|--email <EMAIL>")]
            public string? Email { get; init; }

            [CommandOption("-p|--password <PASSWORD>")]
            public string? Password { get; init; }

            public Client? AuthClient { get; set; }
        }
    }
}
