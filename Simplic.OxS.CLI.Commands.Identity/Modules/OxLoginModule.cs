using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;

namespace Simplic.OxS.CLI.Identity.Modules
{
    public class OxLoginModule : IAsyncModule<IOxSettings>
    {
        public Task Execute(IOxSettings settings)
        {
            return AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Logging in[/]", context => settings.AuthClient!.Login());
        }
    }
}
