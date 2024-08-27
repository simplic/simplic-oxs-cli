using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Organization.Commands
{
    public class OrganizationCreateCommand : IAsyncCommand<OrganizationCreateCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var client = new OrganizationClient(settings.HttpClient);

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Deleting organization[/]", _ =>
                    client.OrganizationPostAsync(new CreateOrganizationRequest
                    {
                        Address = new AddressModel
                        {
                            Additional01 = settings.Additional01,
                            City = settings.City,
                            CountryIso = settings.Country,
                            HouseNumber = settings.House?.ToString(),
                            Street = settings.Street,
                            ZipCode = settings.ZipCode,
                        },
                        Name = settings.OrganizationName ?? Interactive.EnterOrganizationName(),
                        Dummy = settings.Dummy,
                    }));

            AnsiConsole.MarkupLine("[green]Organization created[/]");
            return 0;
        }

        public interface ISettings : IOxSettings
        {
            [CommandOption("--additional01")]
            public string? Additional01 { get; init; }

            [CommandOption("--street <STRING>")]
            public string? Street { get; init; }

            [CommandOption("--house <NUMBER>")]
            public int? House { get; init; }

            [CommandOption("--zipcode <STRING>")]
            public string? ZipCode { get; init; }

            [CommandOption("--city <STRING>")]
            public string? City { get; init; }

            [CommandOption("--country <STRING>")]
            public string? Country { get; init; }

            [CommandOption("--dummy <FLAG>")]
            public bool Dummy { get; init; }

            [CommandArgument(0, "[NAME]")]
            public string? OrganizationName { get; init; }
        }
    }
}
