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
                config.AddCommand<SetupCommand>("setup")
                    .WithDescription(SetupCommand.Description)
                    .WithExample(SetupCommand.Example);
                config.SetExceptionHandler(ex => AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything));
                config.SetApplicationCulture(CultureInfo.InvariantCulture);
            });

            try
            {
                Util.InitializeContainer();
                Util.InitializeFramework();

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

            [CommandOption("--pw|--password <PASSWORD>")]
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

            public override ValidationResult Validate()
            {
                if (Id is not null && Name is not null)
                    return ValidationResult.Error("Only organization name or id can be set");

                return base.Validate();
            }
        }

        /// <summary>
        /// Settings for commands that communicate with simplic studio
        /// </summary>
        internal class StudioSettings : CommandSettings
        {
            [CommandOption("-d|--db <CONNECTION>")]
            [Description("Database connection string")]
            public string? DbConn { get; init; }

            public override ValidationResult Validate()
            {
                return base.Validate();
            }
        }

        /// <summary>
        /// Settings for commands that operate on an Ox organization and communicate with simplic studio
        /// </summary>
        internal class StudioOxOrganizationSettings : OxOrganizationSettings
        {
            [CommandOption("-d|--db <CONNECTION>")]
            [Description("Database connection string")]
            public string? DbConn { get; init; }

            public override ValidationResult Validate()
            {
                return base.Validate();
            }
        }

        /// <summary>
        /// Command that opens up an interface to setup an organization and synchronize data
        /// </summary>
        internal sealed class InteractiveCommand : AsyncCommand<InteractiveCommand.Settings>
        {
            public const string Description = "Setup a development environment interactively";
            public static string[] Example = [
                "interactive",
                "--uri", "dev-oxs.simplic.io"
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
            {
                await Interactive.Run(settings);

                return 0;
            }

            public sealed class Settings : StudioOxOrganizationSettings
            {
                [CommandOption("--dp|--dlls <DIR>")]
                [Description("Add a path containing DLLs that can be loaded")]
                public string[] DllPaths { get; init; } = [];

                [CommandOption("-p|--plugin <FILE>")]
                [Description("Load a plugin")]
                public string[] Plugins { get; init; } = [];
            }
        }

        internal sealed class RegisterCommand : AsyncCommand<OxAuthSettings>
        {
            public const string Description = "Register an account";
            public static string[] Example = [
                "register",
                "--uri", "dev-oxs.simplic.io",
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, OxAuthSettings settings)
            {
                var uri = settings.Uri ?? Interactive.EnterUri();
                var email = settings.Email ?? Interactive.EnterEmail();
                var password = settings.Password ?? Interactive.EnterPassword();

                using var client = new Client(uri, email, password);
                await client.Register();
                AnsiConsole.MarkupLineInterpolated($"[green]Registration successfull[/]");

                return 0;
            }
        }


        internal sealed class CreateCommand : AsyncCommand<CreateCommand.Settings>
        {
            public const string Description = "Create a dummy organization";
            public static string[] Example = [
                "create",
                "dev-oxs.simplic.io", "\"Pipeline check\"",
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
            {
                var uri = settings.Uri ?? Interactive.EnterUri();
                var email = settings.Email ?? Interactive.EnterEmail();
                var password = settings.Password ?? Interactive.EnterPassword();
                var name = settings.Name ?? Interactive.EnterName();

                using var client = new Client(uri, email, password);
                await client.Login();

                var organization = await OxManager.CreateDummyOrganization(client, name);
                AnsiConsole.MarkupLineInterpolated($"Created organization [gray]{organization.Id}[/] - [yellow]{organization.Name}[/]");

                return 0;
            }

            public sealed class Settings : OxAuthSettings
            {
                [CommandArgument(0, "[name]")]
                [Description("Name of the organization to create")]
                public string? Name { get; set; }
            }
        }

        internal sealed class DeleteCommand : AsyncCommand<OxOrganizationSettings>
        {
            public const string Description = "Delete a dummy organization";
            public static string[] Example = [
                "delete",
                "--uri", "dev-oxs.simplic.io",
                "-i", "<id>",
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, OxOrganizationSettings settings)
            {
                var uri = settings.Uri ?? Interactive.EnterUri();
                var email = settings.Email ?? Interactive.EnterEmail();
                var password = settings.Password ?? Interactive.EnterPassword();
                var id = settings.Id;

                using var client = new Client(uri, email, password);
                await client.Login();

                if (id is null)
                {
                    var name = settings.Name ?? Interactive.EnterName();
                    var organization = await client.GetOrganizationByName(name);
                    if (organization == null)
                        return -1;

                    id = organization.Id;
                }

                await OxManager.DeleteDummyOrganization(client, id.Value);
                AnsiConsole.MarkupLineInterpolated($"Deleted organization [gray]{id}[/]");

                return 0;
            }
        }

        internal sealed class ListCommand : AsyncCommand<OxAuthSettings>
        {
            public const string Description = "List organizations linked with user";
            public static string[] Example = [
                "list",
                "--uri", "dev-oxs.simplic.io",
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, OxAuthSettings settings)
            {
                var uri = settings.Uri ?? Interactive.EnterUri();
                var email = settings.Email ?? Interactive.EnterEmail();
                var password = settings.Password ?? Interactive.EnterPassword();

                using var client = new Client(uri, email, password);
                await client.Login();

                foreach (var organization in await client.ListOrganizations())
                {
                    AnsiConsole.MarkupLineInterpolated($"[gray]{organization.OrganizationId}[/] - [blue]{organization.OrganizationName}[/]");
                }

                return 0;
            }
        }

        internal sealed class InstallCommand : AsyncCommand<InstallCommand.Settings>
        {
            public const string Description = "Download plugins from database";
            public static string[] Example = [
                "install", "./simplic/bin/",
                "--dlls", "C:/Program Files/Simplic Studio/",
            ];

            public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
            {
                var downloadPath = settings.DownloadPath ?? "./simplic/bin/";

                var numDlls = Plugins.CountDlls();

                if (Directory.Exists(downloadPath))
                    Directory.Delete(downloadPath, true);
                Directory.CreateDirectory(downloadPath);

                AnsiConsole.WriteLine($"Downloading {numDlls} Dlls to {downloadPath}");
                await AnsiConsole.Progress().StartAsync(async progress =>
                {
                    var task = progress.AddTask("Downloading Dlls");

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

            public sealed class Settings : StudioSettings
            {
                [CommandArgument(0, "[DIR]")]
                [Description("Store DLLs to this directory")]
                public string? DownloadPath { get; init; }
            }
        }

        internal sealed class SetupCommand : AsyncCommand<SetupCommand.Settings>
        {
            public const string Description = "Setup a testing environment with a single command";
            public static string[] Example = [
                "setup",
                "--uri", "dev-oxs.simplic.io",
                "--email", "automated@example.com",
                "--password", "1234",
                "--download", "./.simplic/bin",
                "--dlls", "./simplic/bin",
                "--plugin", "Simplic.PlugIn.SAC",
            ];

            public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
            {
                throw new NotImplementedException();
            }

            public sealed class Settings : OxOrganizationSettings
            {
                [CommandOption("--dl|--download")]
                [Description("Download DLLs from Database to this directory")]
                public string? Download { get; init; }

                [CommandOption("--dp|--dlls")]
                [Description("Add a path containing DLLs that can be loaded")]
                public string[] DllPaths { get; init; } = [];

                [CommandOption("-p|--plugin")]
                [Description("Load a plugin")]
                public string[] Plugins { get; init; } = Array.Empty<string>();
            }
        }
    }
}
