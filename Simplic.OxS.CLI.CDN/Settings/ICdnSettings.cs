using Simplic.OxS.CLI.Identity.Settings;

namespace Simplic.OxS.CLI.CDN.Settings
{
    public interface ICdnSettings : IOxSettings
    {
        public CdnClient? CdnClient { get; set; }
    }
}
