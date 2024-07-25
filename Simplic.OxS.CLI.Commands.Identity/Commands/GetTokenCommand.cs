using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Identity.Commands
{
    public class GetTokenCommand : AsyncCommand<GetTokenCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            Console.WriteLine(settings.AuthClient!.Token);
            return 0;
        }

        public class Settings : CommandSettings, ILoginSettings
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
