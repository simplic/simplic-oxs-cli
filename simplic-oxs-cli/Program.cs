using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.Ox.CLI
{
    public static class Program
    {
        private const string Description = "Easy setup of test environments for OxS. Use without arguments to enable interactive mode";

        private async static Task Main(string[] args)
        {
            AnsiConsole.WriteLine("Simplic Ox CLI");

            var app = new CommandApp<RootCommand>().WithDescription(Description);
            try
            {
                await app.RunAsync(args);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Simplix Ox CLI crashed![/]");
                AnsiConsole.WriteException(ex);
            }
        }

        internal sealed class RootCommand : AsyncCommand<RootCommand.Settings>
        {
            public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
            {
                await Interactive.Run();

                return 0;
            }

            public sealed class Settings : CommandSettings
            {

            }
        }
    }
}
