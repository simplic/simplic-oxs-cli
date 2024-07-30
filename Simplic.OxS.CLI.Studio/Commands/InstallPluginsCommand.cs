using Simplic.OxS.CLI.Studio.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Studio.Commands
{
    public class InstallPluginsCommand : AsyncCommand<InstallPluginsCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var downloadPath = settings.DownloadPath ?? "./.simplic/bin/";

            if (Directory.Exists(downloadPath))
                Directory.Delete(downloadPath, true);
            Directory.CreateDirectory(downloadPath);

            return await AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn()
                )
                .StartAsync(async progress =>
                {
                    var downloadProgress = progress.AddTask("Downloading DLLs");
                    var copyProgress = progress.AddTask("Copying DLLs");
                    downloadProgress.IsIndeterminate = true;
                    copyProgress.IsIndeterminate = true;

                    var downloadTask = Task.Run(async () =>
                    {
                        // Downloading plugins
                        var numDlls = PluginHelper.CountDlls();
                        AnsiConsole.WriteLine($"Downloading {numDlls} DLLs to {downloadPath}");

                        downloadProgress.IsIndeterminate = false;

                        var i = 0;
                        downloadProgress.MaxValue = numDlls;
                        foreach (var dll in PluginHelper.DownloadDlls())
                        {
                            await File.WriteAllBytesAsync(Path.Join(downloadPath, dll.Name + ".dll"), dll.Content);
                            downloadProgress.Value = ++i;
                        }
                        downloadProgress.StopTask();
                    });
                    var copyTask = Task.Run(() =>
                    {
                        var paths = new List<string>();
                        foreach (var dir in settings.AdditionalPaths)
                            paths.AddRange(Directory.GetFiles(dir, "*.dll"));

                        copyProgress.IsIndeterminate = false;

                        var i = 0;
                        copyProgress.MaxValue = paths.Count;
                        foreach (var path in paths)
                        {
                            var file = Path.GetFileName(path);
                            File.Copy(path, Path.Join(downloadPath, file), true);
                            copyProgress.Value = ++i;
                        }
                        copyProgress.StopTask();
                    });
                    await Task.WhenAll(downloadTask, copyTask);
                    return 0;
                });
        }

        public class Settings : CommandSettings, IStudioLoginSettings
        {
            [CommandOption("-c|--conn <CONNECTION>")]
            [Description("Database connection string")]
            public string? ConnectionString { get; init; }

            [CommandOption("-p|--path")]
            [Description("Copy DLLs from this path")]
            public string[] AdditionalPaths { get; init; } = [];

            [CommandArgument(0, "[DIR]")]
            [Description("Store DLLs to this directory")]
            public string? DownloadPath { get; init; }
        }
    }
}
