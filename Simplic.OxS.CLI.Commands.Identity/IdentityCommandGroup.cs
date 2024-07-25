using Simplic.OxS.CLI.Identity.Commands;
using Simplic.OxS.CLI.Identity.Modules;
using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;

namespace Simplic.OxS.CLI.Identity
{
    public class IdentityCommandGroup : ICommandGroup
    {
        public string Name => "identity";

        public void Register(CommandGroupBuilder builder) => builder
            .Module<LoginModule, ILoginSettings>()
            .Module<SelectOrganizationModule, ISelectOrganizationSettings>(builder => builder
                .RequireModule<LoginModule>()
            )
            .Command<GetTokenCommand, GetTokenCommand.Settings>("get-token", builder => builder
                .RequireModule<LoginModule>()
                .Example([
                    "get-token",
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password",
                ])
            )
            .Command<ChangePasswordCommand, ChangePasswordCommand.Settings>("change-password", builder => builder
                .RequireModule<LoginModule>()
                .Example([
                    "change-password",
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password",
                    "newpassword1234",
                ])
            );
    }
}
