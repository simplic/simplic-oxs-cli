using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Modules;
using Simplic.OxS.CLI.Organization.Commands;

namespace Simplic.OxS.CLI.Organization
{
    public class OrganizationCommandGroup : ICommandGroup
    {
        public string Name => "organization";

        public void Register(CommandGroupBuilder builder) => builder
            .Command<CreateOrganizationCommand, CreateOrganizationCommand.Settings>("create", builder => builder
                .RequireModule<LoginModule>()
                .Example([
                    "create",
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password",
                    "MyCoolOrganization",
                ])
            )
            .Command<DeleteOrganizationCommand, DeleteOrganizationCommand.Settings>("delete", builder => builder
                .RequireModule<LoginModule>()
                .Example([
                    "delete",
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password",
                    "01234567-89ab-cdef-0123-456789abcdef"
                ])
            )
            .Command<ListOrganizationsCommand, ListOrganizationsCommand.Settings>("list", builder => builder
                .RequireModule<LoginModule>()
                .Example([
                    "list",
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password",
                ])
            );
    }
}
