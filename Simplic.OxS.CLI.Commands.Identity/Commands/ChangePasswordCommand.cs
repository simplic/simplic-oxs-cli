using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Identity.Commands
{
    internal class ChangePasswordCommand : AsyncCommand<ChangePasswordCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var password = settings.NewPassword ?? Interactive.EnterNewPassword();

            await settings.AuthClient!.ChangePassword(password);

            AnsiConsole.MarkupLine("[green]Password changed[/]");
            return 0;
        }

        public class Settings : CommandSettings, IOxSettings
        {
            [CommandOption("-u|--uri")]
            public Uri? Uri { get; init; }

            [CommandOption("-e|--email")]
            public string? Email { get; init; }

            [CommandOption("-p|--password")]
            public string? Password { get; init; }

            public Client? AuthClient { get; set; }

            [CommandArgument(0, "[PASSWORD]")]
            public string? NewPassword { get; init; }
        }
    }
}
