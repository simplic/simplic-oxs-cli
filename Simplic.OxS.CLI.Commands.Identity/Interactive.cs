using Spectre.Console;

namespace Simplic.OxS.CLI.Identity
{
    public static class Interactive
    {
        public static Uri EnterUri() => new(AnsiConsole.Ask<string>("[bold magenta]Enter uri[/]      [gray]>[/]"));
        public static string EnterEmail() => AnsiConsole.Ask<string>("[bold magenta]Enter email[/]    [gray]>[/]");
        public static string EnterPassword() => AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]Enter password[/] [gray]>[/]").Secret());
        public static string EnterNewPassword() => AnsiConsole.Prompt(new TextPrompt<string>("[bold magenta]New password[/]   [gray]>[/]").Secret());
        public static Guid EnterOrganizationId() => Guid.Parse(AnsiConsole.Ask<string>("[bold magenta]Enter org id[/]   [gray]>[/]"));
    }
}
