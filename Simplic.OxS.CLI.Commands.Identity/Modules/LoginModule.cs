using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;

namespace Simplic.OxS.CLI.Identity.Modules
{
    public class LoginModule : IAsyncModule<ILoginSettings>
    {
        public Task Execute(ILoginSettings settings)
        {
            var uri = settings.Uri ?? Interactive.EnterUri();
            var email = settings.Email ?? Interactive.EnterEmail();
            var password = settings.Password ?? Interactive.EnterPassword();

            var client = new Client(uri, email, password);
            settings.AuthClient = client;

            return AnsiConsole.Status()
                .Spinner(Spinner.Known.Flip)
                .StartAsync("[olive]Logging in[/]", context => client.Login());
        }
    }
}
