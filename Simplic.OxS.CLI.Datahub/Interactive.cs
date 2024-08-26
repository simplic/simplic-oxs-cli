using Spectre.Console;

namespace Simplic.OxS.CLI.Datahub
{
    public static class Interactive
    {
        public static string EnterUsername() => AnsiConsole.Ask<string>("[bold magenta]Enter username[/] [gray]>[/]");
        public static string EnterPassword() => AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter password[/] [gray]>[/]").Secret());
        public static string EnterApiKey() => AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter api-key[/]  [gray]>[/]"));
        public static string EnterFilePath() => AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter file path[/][gray]>[/]"));
        public static Guid EnterUserId() => AnsiConsole.Ask<Guid>("[bold magenta]Enter user-id[/]  [gray]>[/]");
        public static Guid EnterDefinitionId() => AnsiConsole.Ask<Guid>("[bold magenta]Enter queue-id[/] [gray]>[/]");
    }
}
