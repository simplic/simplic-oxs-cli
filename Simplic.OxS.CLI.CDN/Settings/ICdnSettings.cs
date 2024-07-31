using Simplic.OxS.CLI.Identity.Settings;

namespace Simplic.OxS.CLI.CDN.Settings
{
    public interface ICdnSettings : IOxOrganizationSettings
    {
        public CdnClient? CdnClient { get; set; }
    }
}
