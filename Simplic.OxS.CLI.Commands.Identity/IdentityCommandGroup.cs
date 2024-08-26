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
            .Module<OxUrlModule, IOxUrlSettings>()
            .Module<OxModule, IOxSettings>(builder => builder
                .Depends<OxUrlModule>()
            )
            .Module<OxLoginModule, IOxSettings>(builder => builder
                .Depends<OxModule>()
            )
            .Module<OxOrganizationModule, IOxOrganizationSettings>(builder => builder
                .Depends<OxLoginModule>()
            )
            .Command<RegisterCommand, RegisterCommand.ISettings>("register", builder => builder
                .Depends<OxModule>()
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "new-user@simplic.biz",
                    "--password", "password1234",
                ])
            )
            .Command<GetTokenCommand, GetTokenCommand.ISettings>("get-token", builder => builder
                .Depends<OxLoginModule>()
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "password1234",
                ])
            )
            .Command<ChangePasswordCommand, ChangePasswordCommand.ISettings>("change-password", builder => builder
                .Depends<OxLoginModule>()
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "--password", "old-password1234",
                    "new-password5678",
                ])
            )
            .Command<ResetPasswordCommand, ResetPasswordCommand.ISettings>("reset-password", builder => builder
                .Example([
                    "--uri", "https://dev-oxs.simplic.io",
                    "--email", "user@simplic.biz",
                    "new-password5678",
                ])
            );
    }
}
