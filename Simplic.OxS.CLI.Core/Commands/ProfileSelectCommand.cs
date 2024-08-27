using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Core.Commands
{
    public class ProfileSelectCommand(ProfileManager profileManager) : IAsyncCommand<ProfileSelectCommand.ISettings>
    {
        public Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var profile = settings.Profile ?? Interactive.EnterProfile();
            if (profileManager.Select(profile))
            {
                AnsiConsole.MarkupLineInterpolated($"[green]Profile [yellow]{profile}[/] selected[/]");
                return Task.FromResult(0);
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Profile [yellow]{profile}[/] not found[/]");
                return Task.FromResult(1);
            }
        }

        public interface ISettings
        {
            [CommandArgument(0, "[NAME]")]
            [Description("Name of the profile to select")]
            public string? Profile { get; set; }
        }
    }
}
