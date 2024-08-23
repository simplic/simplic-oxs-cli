using Spectre.Console;

namespace Simplic.OxS.CLI.Datahub
{
    public static class Interactive
    {
        public static string EnterUsername() => AnsiConsole.Ask<string>("[bold magenta]Enter username[/] [gray]>[/]");
        public static string EnterPassword() => AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter password[/] [gray]>[/]").Secret());
    }
}
