using MimeMapping;
using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Datahub.Commands
{
    public class DatahubEnqueueCommand : IAsyncCommand<DatahubEnqueueCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var definitionId = settings.DefinitionId ?? Interactive.EnterDefinitionId();
            var apiKey = settings.ApiKey ?? Interactive.EnterApiKey();
            var filePath = settings.File ?? Interactive.EnterFilePath();

            var client = new DatahubClient(settings.HttpClient);

            using var file = File.OpenRead(filePath);
            var fileParam = new FileParameter(file, Path.GetFileName(filePath), MimeUtility.GetMimeMapping(filePath));
            var response = await client.EnqueueFileAsync(definitionId, apiKey, fileParam);
            AnsiConsole.MarkupLine("[green]Item added to queue[/]");
            AnsiConsole.MarkupLineInterpolated($"Queue  : [yellow]{response.Definition.Name}[/]");
            AnsiConsole.MarkupLineInterpolated($"State  : [yellow]{response.State}[/]");
            AnsiConsole.MarkupLineInterpolated($"Path   : [yellow]{response.Path}[/]");
            AnsiConsole.MarkupLineInterpolated($"Item ID: [gray]{response.Id}[/]");

            return 0;
        }

        public interface ISettings : IOxUrlSettings
        {
            [CommandOption("-k|--api-key <KEY>")]
            public string? ApiKey { get; set; }

            [CommandOption("-d|--definition <GUID>")]
            public Guid? DefinitionId { get; set; }

            [CommandArgument(0, "<FILE>")]
            public string? File { get; set; }
        }
    }
}
