using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Datahub.Commands;

namespace Simplic.OxS.CLI.Datahub
{
    public class DatahubCommandGroup : ICommandGroup
    {
        public string Name => "datahub";

        public void Register(CommandGroupBuilder builder) => builder
            .Command<DatahubUserCreateCommand, DatahubUserCreateCommand.ISettings>("create");
    }
}
