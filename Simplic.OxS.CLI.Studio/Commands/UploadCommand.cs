using Simplic.OxS.CLI.Identity;
using Simplic.OxS.CLI.Identity.Settings;
using Simplic.OxS.CLI.Studio.Settings;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Studio.Commands
{
    internal class UploadCommand : AsyncCommand<UploadCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var tenantId = settings.TenantId ?? Interactive.EnterTenant();

            if (settings.Contexts == null)
                await Interactive.Upload(tenantId, settings.AuthClient!.Token!);
            else
                await UploadHelper.Upload(settings.Contexts, tenantId, settings.AuthClient!.Token!);
            return 0;
        }

        internal class Settings : CommandSettings, IOxOrganizationSettings, IStudioLoginSettings, IPluginLoaderSettings
        {
            [CommandOption("-u|--uri")]
            public Uri? Uri { get; init; }

            [CommandOption("-e|--email")]
            public string? Email { get; init; }

            [CommandOption("-p|--password")]
            public string? Password { get; init; }

            [CommandOption("-o|--organization")]
            [Description("Database connection string")]
            public Guid? OrganizationId { get; init; }

            [CommandOption("-c|--conn <CONNECTION>")]
            [Description("Database connection string")]
            public string? ConnectionString { get; init; }

            [CommandOption("-D|--dlls <DIR>")]
            [Description("Add a path containing DLLs that can be loaded")]
            [DefaultValue("./.simplic/bin")]
            public string DllPath { get; init; } = null!;

            [CommandOption("-P|--plugin <NAME>")]
            [Description("Load a plugin")]
            public string[]? Plugins { get; init; }

            [CommandOption("-s|--sync <CONTEXT>")]
            [Description("Synchronize a context")]
            public string[]? Contexts { get; init; }

            [CommandOption("-t|--tenant <TENANT>")]
            [Description("Studio tenant")]
            public Guid? TenantId { get; init; }

            public Client? AuthClient { get; set; }
        }
    }
}
