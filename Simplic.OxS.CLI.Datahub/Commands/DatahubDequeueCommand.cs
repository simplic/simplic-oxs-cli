using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;

namespace Simplic.OxS.CLI.Datahub.Commands
{
    public class DatahubDequeueCommand : IAsyncCommand<DatahubDequeueCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var itemId = settings.ItemId ?? Interactive.EnterDefinitionId();

            var client = new DatahubClient(settings.HttpClient);

            await client.QueueDELETEAsync(itemId);
            AnsiConsole.MarkupLine("[green]Item dequeued[/]");

            return 0;
        }

        public interface ISettings : IOxSettings
        {
            [CommandOption("-i|--item <GUID>")]
            [Description("ID of the queued item")]
            public Guid? ItemId { get; set; }
        }
    }
}
