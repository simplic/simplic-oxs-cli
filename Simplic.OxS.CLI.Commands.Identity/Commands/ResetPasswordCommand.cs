using Simplic.Studio.Ox.Service;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Identity.Commands
{
    internal class ResetPasswordCommand : AsyncCommand<ResetPasswordCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var uri = settings.Uri ?? Interactive.EnterUri();
            var email = settings.Email ?? Interactive.EnterEmail();
            var password = settings.NewPassword ?? Interactive.EnterNewPassword();

            using var httpClient = new HttpClient() { BaseAddress = uri };
            var authClient = new AuthClient(httpClient);
            await authClient.RestorePasswordAsync(new ResetPasswordRequest { Email = email, NewPassword = password });

            AnsiConsole.MarkupLine("[green]Password restored[/]");
            return 0;
        }

        public class Settings : CommandSettings
        {
            [CommandOption("-u|--uri")]
            public Uri? Uri { get; init; }

            [CommandOption("-e|--email")]
            public string? Email { get; init; }

            [CommandArgument(0, "[PASSWORD]")]
            public string? NewPassword { get; init; }
        }
    }
}
