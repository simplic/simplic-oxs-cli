using Simplic.OxS.CLI.CDN.Settings;
using Simplic.OxS.CLI.Identity;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.CDN.Commands
{
    internal class CdnUploadCommand : AsyncCommand<CdnUploadCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var files = new List<FileParameter>();
            var client = new CdnClient(settings.AuthClient!.HttpClient);
            try
            {
                foreach (var file in settings.Files)
                    files.Add(new FileParameter(File.OpenRead(file), Path.GetFileName(file)));

                return await AnsiConsole.Progress()
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new RemainingTimeColumn()
                    )
                    .StartAsync(async context =>
                    {
                        var total = context.AddTask("Uploading", new ProgressTaskSettings { MaxValue = settings.Files.Length });

                        var token = new CancellationTokenSource();
                        var tasks = new List<Task>();
                        foreach (var file in files)
                        {
                            var progress = context.AddTask(file.FileName, new ProgressTaskSettings { MaxValue = file.Data.Length });
                            tasks.Add(Task.Run(async () =>
                            {
                                while (!token.Token.IsCancellationRequested)
                                {
                                    progress.Value = file.Data.Position;
                                    await Task.Delay(500);
                                }
                            }));
                        }
                        var responses = await client.UploadFileAsync(files);
                        token.Cancel();
                        await Task.WhenAll(tasks);
                        return 0;
                    });
            }
            finally
            {
                foreach (var file in files)
                    await file.Data.DisposeAsync();
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

            [CommandArgument(0, "[files]")]
            [Description("Files to upload")]
            public required string[] Files { get; init; }

            public Client? AuthClient { get; set; }

            public CdnClient? CdnClient { get; set; }
        }
    }
}
