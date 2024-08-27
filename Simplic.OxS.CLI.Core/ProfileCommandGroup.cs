using Simplic.OxS.CLI.Core.Commands;

namespace Simplic.OxS.CLI.Core
{
    public class ProfileCommandGroup : ICommandGroup
    {
        public string Name => "profile";

        public void Register(CommandGroupBuilder builder) => builder
            .Command<ProfileDeleteCommand, ProfileDeleteCommand.ISettings>("delete")
            .Command<ProfileSelectCommand, ProfileSelectCommand.ISettings>("select")
            .Command<ProfileUnselectCommand, ProfileUnselectCommand.ISettings>("unselect");
    }
}
