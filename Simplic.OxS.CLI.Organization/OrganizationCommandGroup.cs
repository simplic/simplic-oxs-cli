using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Modules;
using Simplic.OxS.CLI.Organization.Commands;

namespace Simplic.OxS.CLI.Organization
{
    public class OrganizationCommandGroup : ICommandGroup
    {
        public string Name => "organization";

        public void Register(CommandGroupBuilder builder) => builder
            .Command<OrganizationCreateCommand, OrganizationCreateCommand.ISettings>("create", builder => builder
                .Depends<OxLoginModule>()
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password",
                    "MyCoolOrganization",
                ])
            )
            .Command<OrganizationDeleteCommand, OrganizationDeleteCommand.ISettings>("delete", builder => builder
                .Depends<OxLoginModule>()
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password",
                    "01234567-89ab-cdef-0123-456789abcdef"
                ])
            )
            .Command<OrganizationListCommand, OrganizationListCommand.ISettings>("list", builder => builder
                .Depends<OxLoginModule>()
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password",
                ])
            )
            .Command<OrganizationGetTokenCommand, OrganizationGetTokenCommand.ISettings>("get-token", builder => builder
                .Depends<OxOrganizationModule>()
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password",
                    "--organization", "01234567-89ab-cdef-0123-456789abcdef"
                ])
            );
    }
}
