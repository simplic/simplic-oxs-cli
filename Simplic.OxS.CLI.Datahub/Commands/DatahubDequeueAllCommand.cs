using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Datahub.Commands
{
    public class DatahubDequeueAllCommand : IAsyncCommand<DatahubDequeueAllCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var definitionId = settings.DefinitionId ?? Interactive.EnterDefinitionId();

            var client = new DatahubGraphQlClient(settings.HttpClient!);
            var items = await client.GetUncomitted(definitionId);
            foreach (var ids in items)
                AnsiConsole.MarkupLineInterpolated($"Item [gray]{ids}[/]");
            return 0;
        }

        public interface ISettings : IOxSettings
        {
            [CommandOption("-d|--definition <GUID>")]
            public Guid? DefinitionId { get; set; }
        }
    }
}
