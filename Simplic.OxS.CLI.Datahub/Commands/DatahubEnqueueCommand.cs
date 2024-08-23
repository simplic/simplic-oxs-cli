using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Datahub.Commands
{
    public class DatahubEnqueueCommand : IAsyncCommand<DatahubEnqueueCommand.ISettings>
    {
        public Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            using var httpClient = new HttpClient { BaseAddress = settings.Uri };
            var client = new DatahubClient(httpClient);

            throw new NotImplementedException();
        }

        public interface ISettings : IUrlSettings
        {
        }
    }
}
