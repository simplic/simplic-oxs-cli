using CommonServiceLocator;
using Simplic.Studio.Ox;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace Simplic.Ox.CLI
{
    public static class Program
    {
        private async static Task Main(string[] args)
        {
            AnsiConsole.WriteLine("Simplic Ox CLI");

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.AddCommand<InteractiveCommand>("interactive")
                    .WithDescription(InteractiveCommand.Description)
                    .WithExample(InteractiveCommand.Example);
                config.AddCommand<RegisterCommand>("register")
                    .WithDescription(RegisterCommand.Description)
                    .WithExample(RegisterCommand.Example);
                config.AddCommand<CreateCommand>("create")
                    .WithDescription(CreateCommand.Description)
                    .WithExample(CreateCommand.Example);
                config.AddCommand<DeleteCommand>("delete")
                    .WithDescription(DeleteCommand.Description)
                    .WithExample(DeleteCommand.Example);
                config.AddCommand<ListCommand>("list")
                    .WithDescription(ListCommand.Description)
                    .WithExample(ListCommand.Example);
                config.AddCommand<InstallCommand>("install")
                    .WithDescription(InstallCommand.Description)
                    .WithExample(InstallCommand.Example);
                config.AddCommand<UploadCommand>("upload")
                    .WithDescription(UploadCommand.Description)
                    .WithExample(UploadCommand.Example);
                config.SetExceptionHandler((ex, resolver) => AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything));
                config.SetApplicationCulture(CultureInfo.InvariantCulture);
            });

            try
            {
                Util.InitializeContainer();
                await app.RunAsync(args);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Simplix Ox CLI crashed![/]");
                AnsiConsole.WriteException(ex);
            }
        }

        /// <summary>
        /// Base settings for every command (authenticate Ox)
        /// </summary>
        internal class OxAuthSettings : CommandSettings
        {
            [CommandOption("-u|--uri <URI>")]
            [Description("URI of the Ox service")]
            public Uri? Uri { get; init; }

            [CommandOption("-e|--email <EMAIL>")]
            [Description("Ox email")]
            public string? Email { get; init; }

            [CommandOption("-p|--password <PASSWORD>")]
            [Description("Ox password")]
            public string? Password { get; init; }
        }

        /// <summary>
        /// Settings only used in commands that operate on an Ox organization
        /// </summary>
        internal class OxOrganizationSettings : OxAuthSettings
        {
            [CommandOption("-i|--id <GUID>")]
            [Description("Ox organization id (mutually exclusive with --name)")]
            public Guid? Id { get; init; }

            [CommandOption("-n|--name <NAME>")]
            [Description("Ox organization name (leave empty for interactive)")]
            public string? Name { get; init; }

            public async Task<Guid> GetId(Client client)
            {
                if (Id != null)
                    return Id.Value;

                var name = Name ?? Interactive.EnterName();
                var organization = await client.GetOrganizationByName(name);
                return organization?.OrganizationId ?? throw new Exception("Organization does not exist");
            }

            public override ValidationResult Validate()
            {
                if (Id is not null && Name is not null)
                    return ValidationResult.Error("Only organization name OR id can be set");

                return base.Validate();
            }
        }

        /// <summary>
        /// Settings for commands that sync data between Studio and Ox
        /// </summary>
        internal class UploadSettings : OxOrganizationSettings
        {
            [CommandOption("-c|--conn <CONNECTION>")]
            [Description("Database connection string")]
            public string? DbConn { get; init; }

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

        /// <summary>
        /// Command that opens up an interface to setup an organization and synchronize data
        /// </summary>
        internal sealed class InteractiveCommand : AsyncCommand<UploadSettings>
        {
            public const string Description = "Setup a development environment interactively";
            public static string[] Example = [
                "interactive",
                "--uri", "https://dev-oxs.simplic.io"
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, UploadSettings settings)
            {
                var uri = settings.Uri ?? Interactive.EnterUri();
                var email = settings.Email ?? Interactive.EnterEmail();
                var password = settings.Password ?? Interactive.EnterPassword();

                // Ox initialization
                Util.InitializeOx();
                using var client = new Client(uri, email, password);
                await client.Login();

                // Studio initialization
                Util.InitializeFramework();
                var dbConn = settings.DbConn ?? Interactive.SelectConnectionString();
                Util.SetConnectionString(dbConn);

                // Plugin initialization
                Plugins.RegisterAssemblyLoaders(settings.DllPaths);

                // Select organization (interactive)
                var oxsId = await Interactive.OrganizationActions(client, settings.Id, settings.Name);

                // The user cancelled login
                if (!oxsId.HasValue || client.Token == null)
                    return -1;

                // Plugin initialization (interactive)
                var plugins = settings.Plugins ?? (IEnumerable<string>)Interactive.SelectPlugins(settings.DllPaths);
                Plugins.Register(plugins);
                Plugins.InitializeAll();

                // Synchronize studio -> Ox (interactive)
                await Interactive.SyncData(oxsId.Value, client.Token);

                return 0;
            }
        }

        /// <summary>
        /// Command that creates an Ox account
        /// </summary>
        internal sealed class RegisterCommand : AsyncCommand<OxAuthSettings>
        {
            public const string Description = "Register an account";
            public static string[] Example = [
                "register",
                "--uri", "https://dev-oxs.simplic.io",
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, OxAuthSettings settings)
            {
                var uri = settings.Uri ?? Interactive.EnterUri();
                var email = settings.Email ?? Interactive.EnterEmail();
                var password = settings.Password ?? Interactive.EnterPassword();

                // Ox initialization
                Util.InitializeOx();
                using var client = new Client(uri, email, password);

                await client.Register();
                AnsiConsole.MarkupLineInterpolated($"[green]Registration successfull[/]");

                return 0;
            }
        }

        /// <summary>
        /// Command to create an OxS organization and link it with a new studio organization
        /// </summary>
        internal sealed class CreateCommand : AsyncCommand<CreateCommand.Settings>
        {
            public const string Description = "Create a dummy OxS and Studio organization + link them";
            public static string[] Example = [
                "create", "\"Pipeline check\"",
                "--uri", "https://dev-oxs.simplic.io",
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
            {
                var uri = settings.Uri ?? Interactive.EnterUri();
                var email = settings.Email ?? Interactive.EnterEmail();
                var password = settings.Password ?? Interactive.EnterPassword();
                var name = settings.Name ?? Interactive.EnterName();

                // Ox initialization
                Util.InitializeOx();
                using var client = new Client(uri, email, password);
                await client.Login();

                // Studio initialization
                var dbConn = settings.DbConn ?? Interactive.SelectConnectionString();
                Util.InitializeFramework();
                Util.SetConnectionString(dbConn);

                // Plugin initialization
                Plugins.RegisterAssemblyLoaders(settings.DllPaths);

                var organization = await OxManager.CreateDummyOrganization(client, name);
                AnsiConsole.MarkupLineInterpolated($"Created organization [gray]{organization.Id}[/] - [yellow]{organization.Name}[/]");

                return 0;
            }

            public sealed class Settings : OxAuthSettings
            {
                [CommandArgument(0, "[name]")]
                [Description("Name of the organization to create")]
                public string? Name { get; set; }

                [CommandOption("-c|--conn <CONNECTION>")]
                [Description("Database connection string")]
                public string? DbConn { get; init; }

                [CommandOption("-D|--dlls <DIR>")]
                [Description("Add a path containing DLLs that can be loaded")]
                public string[] DllPaths { get; init; } = [];
            }
        }

        /// <summary>
        /// Command to delete an OxS and Studio organization
        /// </summary>
        internal sealed class DeleteCommand : AsyncCommand<DeleteCommand.Settings>
        {
            public const string Description = "Delete a dummy organization";
            public static string[] Example = [
                "delete",
                "--uri", "https://dev-oxs.simplic.io",
                "-i", "<id>",
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
            {
                var uri = settings.Uri ?? Interactive.EnterUri();
                var email = settings.Email ?? Interactive.EnterEmail();
                var password = settings.Password ?? Interactive.EnterPassword();

                // Ox initialization
                Util.InitializeOx();
                using var client = new Client(uri, email, password);
                await client.Login();

                // Get Ox organization id
                var id = await settings.GetId(client);

                // Studio initialization
                var dbConn = settings.DbConn ?? Interactive.SelectConnectionString();
                Util.InitializeFramework();
                Util.SetConnectionString(dbConn);

                // Plugin initialization
                Plugins.RegisterAssemblyLoaders(settings.DllPaths);

                await OxManager.DeleteDummyOrganization(client, id);
                AnsiConsole.MarkupLineInterpolated($"Deleted organization [gray]{id}[/]");

                return 0;
            }

            public class Settings : OxOrganizationSettings
            {
                [CommandOption("-c|--conn <CONNECTION>")]
                [Description("Database connection string")]
                public string? DbConn { get; init; }

                [CommandOption("-D|--dlls <DIR>")]
                [Description("Add a path containing DLLs that can be loaded")]
                public string[] DllPaths { get; init; } = [];
            }
        }

        /// <summary>
        /// Command to list Ox organizations linked to the user
        /// </summary>
        internal sealed class ListCommand : AsyncCommand<OxAuthSettings>
        {
            public const string Description = "List organizations linked with user";
            public static string[] Example = [
                "list",
                "--uri", "https://dev-oxs.simplic.io",
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, OxAuthSettings settings)
            {
                var uri = settings.Uri ?? Interactive.EnterUri();
                var email = settings.Email ?? Interactive.EnterEmail();
                var password = settings.Password ?? Interactive.EnterPassword();

                // Ox initialization
                Util.InitializeOx();
                using var client = new Client(uri, email, password);
                await client.Login();

                foreach (var organization in await client.ListOrganizations())
                    AnsiConsole.MarkupLineInterpolated($"[gray]{organization.OrganizationId}[/] - [blue]{organization.OrganizationName}[/]");

                return 0;
            }
        }

        /// <summary>
        /// Command to downlaod DLLs (plugins and plugin dependencies) from the database
        /// </summary>
        internal sealed class InstallCommand : AsyncCommand<InstallCommand.Settings>
        {
            public const string Description = "Download dlls from database";
            public static string[] Example = ["install"];

            public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
            {
                var downloadPath = settings.DownloadPath ?? "./.simplic/bin/";

                // Studio initialization
                var dbConn = settings.DbConn ?? Interactive.SelectConnectionString();
                Util.InitializeFramework();
                Util.SetConnectionString(dbConn);
                var numDlls = Plugins.CountDlls();

                if (Directory.Exists(downloadPath))
                    Directory.Delete(downloadPath, true);
                Directory.CreateDirectory(downloadPath);

                // Downloading plugins
                AnsiConsole.WriteLine($"Downloading {numDlls} DLLs to {downloadPath}");
                await AnsiConsole.Progress().StartAsync(async progress =>
                {
                    var task = progress.AddTask("Downloading DLLs");

                    var i = 0;
                    foreach (var dll in Plugins.DownloadDlls())
                    {
                        await File.WriteAllBytesAsync(Path.Join(downloadPath, dll.Name + ".dll"), dll.Content);
                        i++;
                        task.Value = 100 * i / numDlls;
                    }
                });

                return 0;
            }

            public sealed class Settings : CommandSettings
            {
                [CommandArgument(0, "[DIR]")]
                [Description("Store DLLs to this directory")]
                public string? DownloadPath { get; init; }

                [CommandOption("-c|--conn <CONNECTION>")]
                [Description("Database connection string")]
                public string? DbConn { get; init; }
            }
        }

        /// <summary>
        /// Command to run upload services for instance data
        /// </summary>
        internal sealed class UploadCommand : AsyncCommand<UploadSettings>
        {
            public const string Description = "Upload instance data from studio to Ox";
            public static string[] Example = [
                "upload",
                "--uri", "https://dev-oxs.simplic.io",
                "--email", "automated@example.com",
                "--password", "1234",
                "--name", "\"Pipeline Check\"",
                "--dlls", "./simplic/bin",
                "--plugin", "Simplic.PlugIn.ArticleMaster",
                "--sync", "quantity_unit",
            ];

            public async override Task<int> ExecuteAsync(CommandContext context, UploadSettings settings)
            {
                var uri = settings.Uri ?? Interactive.EnterUri();
                var email = settings.Email ?? Interactive.EnterEmail();
                var password = settings.Password ?? Interactive.EnterPassword();

                // Ox initialization
                Util.InitializeOx();
                using var client = new Client(uri, email, password);
                await client.Login();

                // Check if auth successful
                if (client.Token == null)
                    return -1;

                var oxsId = await settings.GetId(client);

                // Studio initialization
                var dbConn = settings.DbConn ?? Interactive.SelectConnectionString();
                Util.InitializeFramework();
                Util.SetConnectionString(dbConn);

                // Plugin initialization
                Plugins.RegisterAssemblyLoaders(settings.DllPaths);
                var plugins = settings.Plugins ?? (IEnumerable<string>)Interactive.SelectPlugins(settings.DllPaths);
                Plugins.Register(plugins);
                Plugins.InitializeAll();

                // Synchronize studio -> Ox (automatic/interactive)
                if (settings.Contexts != null)
                    await Util.SynchronizeContexts(settings.Contexts, oxsId, client.Token);
                else
                    await Interactive.SyncData(oxsId, client.Token);

                return 0;
            }
        }
    }
}
