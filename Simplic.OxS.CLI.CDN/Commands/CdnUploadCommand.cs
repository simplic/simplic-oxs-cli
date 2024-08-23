using MimeMapping;
using Simplic.OxS.CLI.CDN.Settings;
using Simplic.OxS.CLI.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.CDN.Commands
{
    public class CdnUploadCommand : IAsyncCommand<CdnUploadCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var files = new List<FileParameter>();
            var client = new CdnClient(settings.AuthClient!.HttpClient);
            var token = new CancellationTokenSource();
            try
            {
                foreach (var file in settings.Files)
                    files.Add(new FileParameter(File.OpenRead(file), Path.GetFileName(file), MimeUtility.GetMimeMapping(file)));

                var responses = await AnsiConsole.Progress()
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new DownloadedColumn(),
                        new TransferSpeedColumn(),
                        new RemainingTimeColumn()
                    )
                    .StartAsync(async context =>
                    {
                        var tasks = new List<Task>();
                        foreach (var file in files)
                        {
                            var progress = context.AddTask(file.FileName, new ProgressTaskSettings { MaxValue = file.Data.Length });
                            tasks.Add(Task.Run(async () =>
                            {
                                while (!token.Token.IsCancellationRequested)
                                {
                                    progress.Value = file.Data.Position;
                                    if (progress.Value == file.Data.Length)
                                        break;
                                    await Task.Delay(500);
                                }
                                progress.Value = progress.MaxValue;
                                progress.StopTask();
                            }));
                        }
                        var responses = await client.UploadFileAsync(files);
                        token.Cancel();
                        await Task.WhenAll(tasks);
                        return responses;
                    });

                AnsiConsole.MarkupLineInterpolated($"[green]Uploaded {files.Count} file(s)[/]");

                var table = new Table()
                    .NoBorder()
                    .AddColumns("Name", "Blob ID", "Size");
                foreach (var metadata in responses.UploadedFiles)
                    table.AddRow(
                        Markup.FromInterpolated($"[yellow]{metadata.Name}[/]"),
                        Markup.FromInterpolated($"[gray]{metadata.BlobId}[/]"),
                        Markup.FromInterpolated($"[blue]{FormatHelper.FormatByteSize(metadata.Size)}[/]")
                    );
                AnsiConsole.Write(table);

                return 0;
            }
            finally
            {
                // Stop progress update thread
                token.Cancel();
                foreach (var file in files)
                    await file.Data.DisposeAsync();
            }
        }

        public interface ISettings : ICdnSettings
        {
            [CommandArgument(0, "[files]")]
            [Description("Files to upload")]
            public string[] Files { get; init; }
        }
    }
}
