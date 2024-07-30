using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Simplic.PlugIn.Studio.Ox.Server;
using Spectre.Console;

namespace Simplic.OxS.CLI.Identity.Modules
{
    public class OxModule : IAsyncModule<IOxSettings>
    {
        public Task Execute(IOxSettings settings)
        {
            AnsiConsole.MarkupLineInterpolated($"Registering module: [yellow]{typeof(FrameworkEntryPoint).FullName}[/]");

            new FrameworkEntryPoint().Initilize();

            var uri = settings.Uri ?? Interactive.EnterUri();
            var email = settings.Email ?? Interactive.EnterEmail();
            var password = settings.Password ?? Interactive.EnterPassword();

            var client = new Client(uri, email, password);
            settings.AuthClient = client;

            return Task.CompletedTask;
        }
    }
}
