using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Datahub.Commands
{
    public class DatahubUserCreateCommand : IAsyncCommand<DatahubUserCreateCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var username = settings.DatahubUsername ?? Interactive.EnterUsername();
            var password = settings.DatahubPassword ?? Interactive.EnterPassword();

            var client = new DatahubClient(settings.HttpClient);
            var response = await client.UserPOSTAsync(new CreateUserRequest
            {
                Name = username,
                Password = password,
                IsActive = true,
            });

            AnsiConsole.MarkupLine("[green]User created[/]");
            AnsiConsole.MarkupLineInterpolated($"Username: [yellow]{response.Name}[/]");
            AnsiConsole.MarkupLineInterpolated($"User-ID : [gray]{response.Id}[/]");
            AnsiConsole.MarkupLineInterpolated($"API Key : [gray]{response.ApiKey}[/]");

            return 0;
        }

        public interface ISettings : IOxSettings
        {
            [CommandArgument(0, "[USERNAME]")]
            [Description("Username for datahub")]
            string? DatahubUsername { get; set; }

            [CommandArgument(1, "[PASSWORD]")]
            [Description("Password for datahub")]
            string? DatahubPassword { get; set; }
        }
    }
}
