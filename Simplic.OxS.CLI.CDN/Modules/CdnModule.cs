using Simplic.OxS.CLI.CDN.Settings;
using Simplic.OxS.CLI.Core;

namespace Simplic.OxS.CLI.CDN.Modules
{
    public class CdnModule : IAsyncModule<ICdnSettings>
    {
        public Task Execute(ICdnSettings settings)
        {
            settings.CdnClient = new CdnClient(settings.AuthClient!.HttpClient);
            return Task.CompletedTask;
        }
    }
}
