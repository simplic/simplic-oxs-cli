using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Identity.Commands
{
    public class RegisterCommand : IAsyncCommand<RegisterCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Creating account[/]", context => settings.AuthClient!.Register());

            return 0;
        }

        public interface ISettings : IOxSettings
        {
        }
    }
}
