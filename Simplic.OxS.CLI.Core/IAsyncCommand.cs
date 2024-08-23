using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Core
{
    public interface IAsyncCommand<TSettings>
    {
        public Task<int> ExecuteAsync(CommandContext context, TSettings settings);
    }
}
