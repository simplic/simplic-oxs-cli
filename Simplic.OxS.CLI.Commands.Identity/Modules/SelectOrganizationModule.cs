using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;

namespace Simplic.OxS.CLI.Identity.Modules
{
    internal class SelectOrganizationModule : IAsyncModule<ISelectOrganizationSettings>
    {
        public Task Execute(ISelectOrganizationSettings settings)
        {
            var id = settings.OrganizationId ?? Interactive.EnterOrganizationId();

            return settings.AuthClient!.LoginOrganization(id);
        }

    }
}
