using Simplic.OxS.CLI.CDN.Settings;
using Simplic.OxS.CLI.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.CDN.Commands
{
    public class CdnDownloadCommand : IAsyncCommand<CdnDownloadCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
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

        public interface ISettings : ICdnSettings
        {
            [CommandOption("-b|--blob <UUID>")]
            [Description("Blob id")]
            public Guid BlobId { get; init; }

            [CommandArgument(0, "[target]")]
            [Description("Download path")]
            public string? TargetPath { get; init; }
        }
    }
}
