using Simplic.OxS.CLI.CDN.Commands;
using Simplic.OxS.CLI.CDN.Modules;
using Simplic.OxS.CLI.CDN.Settings;
using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Modules;

namespace Simplic.OxS.CLI.CDN
{
    public class CdnCommandGroup : ICommandGroup
    {
        public string Name => "cdn";

        public void Register(CommandGroupBuilder builder) => builder
            .Module<CdnModule, ICdnSettings>(builder => builder
                .Depends<OxLoginModule>()
            )
            .Command<CdnUploadCommand, CdnUploadCommand.Settings>("upload", builder => builder
                .Depends<CdnModule>()
            );
    }
}
