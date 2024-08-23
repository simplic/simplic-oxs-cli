using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Simplic.OxS.CLI.Studio.Settings;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Studio.Commands
{
    public class UploadCommand : IAsyncCommand<UploadCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var tenantId = settings.TenantId ?? Interactive.EnterTenant();

            if (settings.Contexts == null)
                await Interactive.Upload(tenantId, settings.AuthClient!.Token!);
            else
                await UploadHelper.Upload(settings.Contexts, tenantId, settings.AuthClient!.Token!);
            return 0;
        }

        public interface ISettings : IOxOrganizationSettings, IStudioLoginSettings, IPluginLoaderSettings
        {
            [CommandOption("-s|--sync <CONTEXT>")]
            [Description("Synchronize a context")]
            public string[]? Contexts { get; init; }

            [CommandOption("-t|--tenant <TENANT>")]
            [Description("Studio tenant")]
            public Guid? TenantId { get; init; }
        }
    }
}
