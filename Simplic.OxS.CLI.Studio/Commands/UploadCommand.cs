using Simplic.OxS.CLI.Identity;
using Simplic.OxS.CLI.Identity.Settings;
using Simplic.OxS.CLI.Studio.Settings;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Studio.Commands
{
    internal class UploadCommand : AsyncCommand<UploadCommand.Settings>
    {
        public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            throw new NotImplementedException();
        }

        internal class Settings : CommandSettings, ILoginSettings, IStudioSettings, IPluginSettings
        {
            [CommandOption("-u|--uri")]
            public Uri? Uri { get; init; }

            [CommandOption("-e|--email")]
            public string? Email { get; init; }

            [CommandOption("-p|--password")]
            public string? Password { get; init; }

            public Client? AuthClient { get; set; }

            [CommandOption("-c|--conn <CONNECTION>")]
            [Description("Database connection string")]
            public string? ConnectionString { get; init; }

            [CommandOption("-D|--dlls <DIR>")]
            [Description("Add a path containing DLLs that can be loaded")]
            public string[] DllPaths { get; init; } = [];

            [CommandOption("-P|--plugin <NAME>")]
            [Description("Load a plugin")]
            public string[]? Plugins { get; init; }

            [CommandOption("-s|--sync <CONTEXT>")]
            [Description("Synchronize a context")]
            public string[]? Contexts { get; init; }
        }
    }
}
