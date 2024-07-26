using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Studio.Commands;
using Simplic.OxS.CLI.Studio.Modules;

namespace Simplic.OxS.CLI.Studio
{
    internal class StudioCommandGroup : ICommandGroup
    {
        public string Name => "studio";

        public void Register(CommandGroupBuilder builder) => builder.
            Command<UploadCommand, UploadCommand.Settings>("upload", builder => builder
                .RequireModule<StudioModule>()
                .RequireModule<PluginModule>()
            );
    }
}
