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
                .Depends<OxOrganizationModule>()
            )
            .Command<CdnMetadataCommand, CdnMetadataCommand.ISettings>("metadata", builder => builder
                .Depends<CdnModule>()
            )
            .Command<CdnUploadCommand, CdnUploadCommand.ISettings>("upload", builder => builder
                .Depends<CdnModule>()
            )
            .Command<CdnDownloadCommand, CdnDownloadCommand.ISettings>("download", builder => builder
                .Depends<CdnModule>()
            )
            .Command<CdnDeleteCommand, CdnDeleteCommand.ISettings>("delete", builder => builder
                .Depends<CdnModule>()
            );
    }
}
