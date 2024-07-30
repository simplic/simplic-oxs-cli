using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Identity.Commands
{
    internal class RegisterCommand : AsyncCommand<RegisterCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Creating account[/]", context => settings.AuthClient!.Register());

            return 0;
        }

        internal class Settings : CommandSettings, IOxSettings
        {
            [CommandOption("-u|--uri")]
            public Uri? Uri { get; init; }

            [CommandOption("-e|--email")]
            public string? Email { get; init; }

            [CommandOption("-p|--password")]
            public string? Password { get; init; }

            public Client? AuthClient { get; set; }
        }
    }
}
