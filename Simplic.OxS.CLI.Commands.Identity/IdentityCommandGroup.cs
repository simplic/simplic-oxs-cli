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
            .Module<OxModule, IOxSettings>()
            .Module<OxLoginModule, IOxSettings>(builder => builder
                .Depends<OxModule>()
            )
            .Module<OxOrganizationModule, IOxOrganizationSettings>(builder => builder
                .Depends<OxLoginModule>()
            )
            .Command<RegisterCommand, RegisterCommand.Settings>("register", builder => builder
                .RequireModule<OxModule>()
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "new-user@simplic.biz",
                    "--password", "password1234",
                ])
            )
            .Command<GetTokenCommand, GetTokenCommand.Settings>("get-token", builder => builder
                .RequireModule<OxLoginModule>()
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password1234",
                ])
            )
            .Command<ChangePasswordCommand, ChangePasswordCommand.Settings>("change-password", builder => builder
                .RequireModule<OxLoginModule>()
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "old-password1234",
                    "new-password5678",
                ])
            )
            .Command<ResetPasswordCommand, ResetPasswordCommand.Settings>("reset-password", builder => builder
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "new-password5678",
                ])
            );
    }
}
