using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;

namespace Simplic.OxS.CLI.Identity.Modules
{
    public class OxUrlModule : IAsyncModule<IOxUrlSettings>
    {
        public Task Execute(IOxUrlSettings settings)
        {
            var uri = settings.Uri ?? Interactive.EnterUri();
            settings.HttpClient = new HttpClient
            {
                BaseAddress = uri,
            };

            return Task.CompletedTask;
        }
    }
}
