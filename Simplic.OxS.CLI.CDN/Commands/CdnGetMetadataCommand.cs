using Simplic.OxS.CLI.CDN.Settings;
using Simplic.OxS.CLI.Core;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.CDN.Commands
{
    public class CdnMetadataCommand : IAsyncCommand<CdnMetadataCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var client = new CdnClient(settings.HttpClient);

            var metadatas = await Interactive.RetrieveMetadata(client, settings.BlobIds);
            Interactive.ShowMetadata(metadatas);

            return 0;
        }

        public interface ISettings : ICdnSettings
        {
            [CommandOption("-b|--blob <UUID>")]
            [Description("Blob id")]
            public Guid[] BlobIds { get; init; }
        }
    }
}
