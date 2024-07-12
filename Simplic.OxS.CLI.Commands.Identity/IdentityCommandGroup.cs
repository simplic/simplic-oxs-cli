using Simplic.OxS.CLI.Core;

namespace Simplic.OxS.CLI.Commands.Identity
{
    public class IdentityCommandGroup : ICommandGroup
    {
        public void Register(CommandGroupBuilder context) => context
            .Module<LoginModule>()
            .Command<GetTokenCommand>("get-token", builder => builder
                .RequiresModule<LoginModule>()
                .Example(["hi"])
            );
    }
}
