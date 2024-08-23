using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Datahub.Commands
{
    public class DatahubEnqueueCommand : AsyncCommand<DatahubEnqueueCommand.Settings>
    {
        public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            using var httpClient = new HttpClient { BaseAddress = settings.Uri };
            var client = new DatahubClient(httpClient);

            throw new NotImplementedException();
        }

        public class Settings : CommandSettings, IUrlSettings
        {
            [CommandOption("--url")]
            [Description("URI of Ox Server instance")]
            public Uri? Uri { get; init; }
        }
    }
}
