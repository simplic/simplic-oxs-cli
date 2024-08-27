using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Core.Commands
{
    public class ProfileDeleteCommand(ProfileManager profileManager) : IAsyncCommand<ProfileDeleteCommand.ISettings>
    {
        public Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var profile = settings.Profile ?? Interactive.EnterProfile();
            if (profileManager.Delete(profile))
            {
                AnsiConsole.MarkupLineInterpolated($"[green]Profile [yellow]{profile}[/] deleted[/]");
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
            [Description("Name of the profile to delete")]
            public string? Profile { get; set; }
        }
    }
}
