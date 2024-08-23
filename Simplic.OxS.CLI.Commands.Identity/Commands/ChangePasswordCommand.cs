using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Identity.Commands
{
    public class ChangePasswordCommand : IAsyncCommand<ChangePasswordCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var password = settings.NewPassword ?? Interactive.EnterNewPassword();

            await settings.AuthClient!.ChangePassword(password);

            AnsiConsole.MarkupLine("[green]Password changed[/]");
            return 0;
        }

        public interface ISettings : IOxSettings
        {
            [CommandArgument(0, "[PASSWORD]")]
            public string? NewPassword { get; init; }
        }
    }
}
