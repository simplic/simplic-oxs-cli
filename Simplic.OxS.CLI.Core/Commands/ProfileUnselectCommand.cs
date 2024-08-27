using Spectre.Console;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Core.Commands
{
    public class ProfileUnselectCommand(ProfileManager profileManager) : IAsyncCommand<ProfileUnselectCommand.ISettings>
    {
        public Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            if (profileManager.Unselect())
            {
                AnsiConsole.MarkupLineInterpolated($"[green]Profile unselected[/]");
                return Task.FromResult(0);
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated($"[red]No profile was previously selected[/]");
                return Task.FromResult(1);
            }
        }

        public interface ISettings { }
    }
}
