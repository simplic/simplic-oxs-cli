using Spectre.Console;

namespace Simplic.OxS.CLI.Core
{
    internal class Interactive
    {
        public static string EnterProfile() => AnsiConsole.Ask<string>("[bold magenta]Enter profile[/]  [gray]>[/]");
    }
}
