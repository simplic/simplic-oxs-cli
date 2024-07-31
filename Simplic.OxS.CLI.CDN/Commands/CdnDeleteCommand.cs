using Simplic.OxS.CLI.CDN.Settings;
using Simplic.OxS.CLI.Identity;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.CDN.Commands
{
    internal class CdnDeleteCommand : AsyncCommand<CdnDeleteCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var client = new CdnClient(settings.AuthClient!.HttpClient);

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

        internal class Settings : CommandSettings, ICdnSettings
        {
            [CommandOption("-u|--uri <SERVER>")]
            [Description("URI of Ox Server instance")]
            public Uri? Uri { get; init; }

            [CommandOption("-e|--email <EMAIL>")]
            [Description("Ox user account email")]
            public string? Email { get; init; }

            [CommandOption("-p|--password <PASSWORD>")]
            [Description("Ox user account password")]
            public string? Password { get; init; }

            [CommandOption("-o|--organization <UUID>")]
            [Description("Ox organization id")]
            public Guid? OrganizationId { get; init; }

            [CommandOption("-b|--blob <UUID>")]
            [Description("Blob id")]
            public Guid[] BlobIds { get; init; } = [];

            public Client? AuthClient { get; set; }

            public CdnClient? CdnClient { get; set; }
        }
    }
}
