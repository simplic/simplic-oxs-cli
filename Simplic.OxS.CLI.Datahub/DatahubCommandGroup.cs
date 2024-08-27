using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Datahub.Commands;
using Simplic.OxS.CLI.Identity.Modules;

namespace Simplic.OxS.CLI.Datahub
{
    public class DatahubCommandGroup : ICommandGroup
    {
        public string Name => "datahub";

        public void Register(CommandGroupBuilder builder) => builder
            .Group("user", builder => builder
                .Command<DatahubUserCreateCommand, DatahubUserCreateCommand.ISettings>("create", builder => builder
                    .Depends<OxLoginModule>()
                )
                .Command<DatahubUserGetApiKeyCommand, DatahubUserGetApiKeyCommand.ISettings>("get-token", builder => builder
                    .Depends<OxLoginModule>()
                )
            )
            .Command<DatahubEnqueueCommand, DatahubEnqueueCommand.ISettings>("enqueue", builder => builder
                    .Depends<OxUrlModule>()
            )
            .Command<DatahubDequeueCommand, DatahubDequeueCommand.ISettings>("dequeue", builder => builder
                    .Depends<OxLoginModule>()
            )
            .Command<DatahubDequeueAllCommand, DatahubDequeueAllCommand.ISettings>("dequeue-all", builder => builder
                    .Depends<OxLoginModule>()
            );
    }
}
