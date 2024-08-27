using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Organization.Commands
{
    public class OrganizationDeleteCommand : IAsyncCommand<OrganizationDeleteCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var id = settings.OrganizationId ?? Interactive.EnterOrganizationId();

            var client = new OrganizationClient(settings.HttpClient);

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Deleting organization[/]", _ => client.OrganizationDeleteAsync(id));

            AnsiConsole.MarkupLine("[green]Organization deleted[/]");
            return 0;
        }

        public interface ISettings : IOxSettings
        {
            [CommandArgument(0, "[ID]")]
            public Guid? OrganizationId { get; init; }
        }
    }
}
