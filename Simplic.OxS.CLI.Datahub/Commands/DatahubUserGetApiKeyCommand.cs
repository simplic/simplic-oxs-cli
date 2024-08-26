using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Datahub.Commands
{
    public class DatahubUserGetApiKeyCommand : IAsyncCommand<DatahubUserGetApiKeyCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var userId = settings.DatahubUserId ?? Interactive.EnterUserId();

            var client = new DatahubClient(settings.HttpClient);
            var key = await client.GenerateApiKeyAsync(userId);
            Console.WriteLine(key);

            return 0;
        }

        public interface ISettings : IOxSettings
        {
            [CommandOption("-i|--userid <GUID>")]
            [Description("User-ID for datahub")]
            Guid? DatahubUserId { get; set; }
        }
    }
}
