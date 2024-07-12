using Simplic.OxS.CLI.Commands.Identity;
using Simplic.OxS.CLI.Core;

namespace Simplic.Ox.CLI
{
    public class Run
    {
        private async static Task MyMain(string[] args)
        {
            var registry = new CommandRegistry();
            registry.Add<IdentityCommandGroup>();
        }
    }
}
