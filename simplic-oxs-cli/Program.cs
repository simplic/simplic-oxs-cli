using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace Simplic.Ox.CLI
{
    public static class Program
    {
        private const string Description = "Easy setup of test environments for OxS. Use without arguments to enable interactive mode";

        private async static Task Main(string[] args)
        {
            AnsiConsole.WriteLine("Simplic Ox CLI");

            var app = new CommandApp<RootCommand>().WithDescription(Description);
            app.Configure(config =>
            {
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
                Interactive.SelectConnectionString();
                var dllPath = "./.simplic/bin";
                Util.RegisterAssemblyLoader(Path.GetFullPath(dllPath));
                Util.RegisterAssemblyLoader("C:\\Users\\m.bergmann\\source\\repos\\simplic-framework\\src\\Simplic.Main\\bin\\Debug");
                Util.InitializeOx();

                await app.RunAsync(args);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Simplix Ox CLI crashed![/]");
                AnsiConsole.WriteException(ex);
            }
        }

        internal class BaseSettings : CommandSettings
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

        internal class OxSettings : BaseSettings
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

        internal sealed class RootCommand : AsyncCommand<RootCommand.Settings>
        {
            public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
            {
                await Interactive.Run(settings);

                return -1;
            }

            public sealed class Settings : OxSettings
            {
                [CommandOption("--dl|--download <DIR>")]
                [Description("Download DLLs from Database to this directory")]
                public string? Download { get; init; }

                [CommandOption("--dp|--dlls <DIR>")]
                [Description("Add a path containing DLLs that can be loaded")]
                public string[] DllPaths { get; init; } = Array.Empty<string>();

                [CommandOption("-p|--plugin <FILE>")]
                [Description("Load a plugin")]
                public string[] Plugins { get; init; } = Array.Empty<string>();
            }
        }

        internal sealed class RegisterCommand : AsyncCommand<BaseSettings>
        {
            public const string Description = "Register an account";
            public const string Example = "register --url dev-oxs.simplic.io";

            public override async Task<int> ExecuteAsync(CommandContext context, BaseSettings settings)
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
            public const string Example = "create dev-oxs.simplic.io \"Pipeline check\"";

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

            public sealed class Settings : BaseSettings
            {
                [CommandArgument(0, "[name]")]
                [Description("Name of the organization to create")]
                public string? Name { get; set; }
            }
        }

        internal sealed class DeleteCommand : AsyncCommand<OxSettings>
        {
            public const string Description = "Delete a dummy organization";
            public const string Example = "delete dev-oxs.simplic.io -i <id>";

            public override async Task<int> ExecuteAsync(CommandContext context, OxSettings settings)
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

        internal sealed class ListCommand : AsyncCommand<BaseSettings>
        {
            public const string Description = "List organizations linked with user";
            public const string Example = "list dev-oxs.simplic.io";

            public override async Task<int> ExecuteAsync(CommandContext context, BaseSettings settings)
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

        internal sealed class SetupCommand : AsyncCommand<SetupCommand.Settings>
        {
            public const string Description = "Setup a testing environment with a single command";
            public const string Example = "setup dev-oxs.simplic.io" +
                "--email automated@example.com " +
                "--password 1234 " +
                "--download ./.simplic/bin " +
                "--dlls ./simplic/bin " +
                "--plusing Simplic.PlugIn.SAC";

            public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
            {
                throw new NotImplementedException();
            }

            public sealed class Settings : OxSettings
            {
                [CommandOption("--dl|--download")]
                [Description("Download DLLs from Database to this directory")]
                public string? Download { get; init; }

                [CommandOption("--dp|--dlls")]
                [Description("Add a path containing DLLs that can be loaded")]
                public string[] DllPaths { get; init; } = Array.Empty<string>();

                [CommandOption("-p|--plugin")]
                [Description("Load a plugin")]
                public string[] Plugins { get; init; } = Array.Empty<string>();
            }
        }
    }
}
