using Simplic.OxS.CLI.CDN.Settings;
using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.CDN.Commands
{
    internal class CdnDownloadCommand : AsyncCommand<CdnDownloadCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var client = new CdnClient(settings.AuthClient!.HttpClient);

            var metadatas = await Interactive.RetrieveMetadata(client, [settings.BlobId]);
            Interactive.ShowMetadata(metadatas);

            string path;
            if (settings.TargetPath != null)
            {
                var dir = Path.GetDirectoryName(settings.TargetPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);
                path = settings.TargetPath;
            }
            else
            {
                path = settings.TargetPath ?? FileHelper.SanitizeFileName(metadatas[0].FileName);
            }

            var token = new CancellationTokenSource();
            var file = File.Create(path);
            try
            {
                file.SetLength(metadatas[0].Size);

                var response = await AnsiConsole.Progress()
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new DownloadedColumn(),
                        new TransferSpeedColumn(),
                        new RemainingTimeColumn()
                    )
                    .StartAsync(async context =>
                    {
                        var progress = context.AddTask("Downloading", new ProgressTaskSettings { AutoStart = false, MaxValue = metadatas[0].Size });

                        var response = await client.GetFileAsync(settings.BlobId);
                        var task = Task.Run(async () =>
                        {
                            while (!token.Token.IsCancellationRequested)
                            {
                                progress.Value = file.Position;
                                if (progress.Value == file.Length)
                                    break;
                                await Task.Delay(500);
                            }
                            progress.Value = progress.MaxValue;
                            progress.StopTask();
                        });
                        response.Stream.CopyTo(file);

                        token.Cancel();
                        await task;
                        return response;
                    });

                AnsiConsole.MarkupLineInterpolated($"[green]Downloaded 1 file[/]");

                return 0;
            }
            finally
            {
                // Stop progress update thread
                token.Cancel();
                file.Dispose();
            }
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
            public required Guid BlobId { get; init; }

            [CommandArgument(0, "[target]")]
            [Description("Download path")]
            public string? TargetPath { get; init; }

            public Client? AuthClient { get; set; }

            public CdnClient? CdnClient { get; set; }
        }
    }
}
