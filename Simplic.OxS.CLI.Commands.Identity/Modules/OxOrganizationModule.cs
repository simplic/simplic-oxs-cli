using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;

namespace Simplic.OxS.CLI.Identity.Modules
{
    public class OxOrganizationModule : IAsyncModule<IOxOrganizationSettings>
    {
        public Task Execute(IOxOrganizationSettings settings)
        {
            var id = settings.OrganizationId ?? Interactive.EnterOrganizationId();

            return settings.AuthClient!.LoginOrganization(id);
        }

    }
}
