using Simplic.OxS.CLI.Identity;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Organization.Commands
{
    internal class DeleteOrganizationCommand : AsyncCommand<DeleteOrganizationCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var id = settings.OrganizationId ?? Interactive.EnterOrganizationId();

            var client = new OrganizationClient(settings.AuthClient!.HttpClient);

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Deleting organization[/]", _ => client.OrganizationDeleteAsync(id));

            AnsiConsole.MarkupLine("[green]Organization deleted[/]");
            return 0;
        }

        public class Settings : CommandSettings, IOxSettings
        {
            [CommandOption("-u|--uri <SERVER>")]
            public Uri? Uri { get; init; }

            [CommandOption("-e|--email <EMAIL>")]
            public string? Email { get; init; }

            [CommandOption("-p|--password <PASSWORD>")]
            public string? Password { get; init; }

            public Client? AuthClient { get; set; }

            [CommandArgument(0, "[ID]")]
            public Guid? OrganizationId { get; init; }
        }
    }
}
