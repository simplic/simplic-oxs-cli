using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Modules;
using Simplic.OxS.CLI.Studio.Commands;
using Simplic.OxS.CLI.Studio.Modules;
using Simplic.OxS.CLI.Studio.Settings;

namespace Simplic.OxS.CLI.Studio
{
    public class StudioCommandGroup : ICommandGroup
    {
        public string Name => "studio";

        public void Register(CommandGroupBuilder builder) => builder
            .Module<PluginLoaderModule, IPluginLoaderSettings>()
            .Module<StudioLoginModule, IStudioLoginSettings>()
            .Command<InstallPluginsCommand, InstallPluginsCommand.Settings>("download-plugins", builder => builder
                .RequireModule<StudioLoginModule>()
            )
            .Command<UploadCommand, UploadCommand.Settings>("upload", builder => builder
                .RequireModule<OxOrganizationModule>()
                .RequireModule<StudioLoginModule>()
                .RequireModule<PluginLoaderModule>()
            );
    }
}
