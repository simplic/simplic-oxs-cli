using Simplic.OxS.CLI.CDN.Settings;
using Simplic.OxS.CLI.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.CDN.Commands
{
    public class CdnDeleteCommand : IAsyncCommand<CdnDeleteCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var client = new CdnClient(settings.HttpClient);

            var metadatas = await Interactive.RetrieveMetadata(client, settings.BlobIds);
            Interactive.ShowMetadata(metadatas);

            await AnsiConsole.Progress()
                .StartAsync(async context =>
                {
                    var progress = context.AddTask("Deleting", new ProgressTaskSettings { MaxValue = settings.BlobIds.Length });
                    var i = 0;
                    foreach (var blobId in settings.BlobIds)
                    {
                        await client.CDNAsync(blobId);
                        progress.Value = ++i;
                    }

                    progress.StopTask();
                });

            AnsiConsole.MarkupLineInterpolated($"[green]Deleted {settings.BlobIds.Length} file(s)[/]");

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
