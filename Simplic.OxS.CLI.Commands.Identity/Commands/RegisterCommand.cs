using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Identity.Commands
{
    internal class RegisterCommand : AsyncCommand<RegisterCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var uri = settings.Uri ?? Interactive.EnterUri();
            var email = settings.Email ?? Interactive.EnterEmail();
            var password = settings.Password ?? Interactive.EnterPassword();

            var client = new Client(uri, email, password);

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Creating account[/]", context => client.Register());

            return 0;
        }

        internal class Settings : CommandSettings
        {
            [CommandOption("-u|--uri")]
            public Uri? Uri { get; init; }

            [CommandOption("-e|--email")]
            public string? Email { get; init; }

            [CommandOption("-p|--password")]
            public string? Password { get; init; }
        }
    }
}
