using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Organization.Commands
{
    public class OrganizationGetTokenCommand : IAsyncCommand<OrganizationGetTokenCommand.ISettings>
    {
        public Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            Console.WriteLine(settings.AuthClient!.Token);
            return Task.FromResult(0);
        }

        public interface ISettings : IOxOrganizationSettings { }
    }
}
