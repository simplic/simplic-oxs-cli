using oxs.Clients;
using oxs.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oxs.Commands.Configure;

/// <summary>
/// Settings for configuring the OXS CLI environment including API connection and authentication.
/// </summary>
public class ConfigureEnvSettings : ConfigureSettings
{
    /// <summary>
    /// Gets or sets the API environment to use. Available options are 'prod' for production or 'staging' for staging environment.
    /// </summary>
    [Description("Api to use. Options: prod or staging")]
    [CommandOption("-a|--api <API>")]
    public string? Api { get; set; }

    /// <summary>
    /// Gets or sets the account email address for authentication.
    /// </summary>
    [Description("Account mail address")]
    [CommandOption("-e|--email <Email>")]
    public string? EMail { get; set; }

    /// <summary>
    /// Gets or sets the account password for authentication.
    /// </summary>
    [Description("Account password")]
    [CommandOption("-p|--password <Password>")]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the organization name to connect to.
    /// </summary>
    [Description("Organization to use")]
    [CommandOption("-o|--organization <Organization>")]
    public string? Organization { get; set; }

    /// <summary>
    /// Gets or sets the configuration section name to store the environment settings. Default value is 'default'.
    /// </summary>
    [Description("Location section to connect the configuration to. Default value is `default`")]
    [CommandOption("-s|--section <Section>")]
    [DefaultValue("default")]
    public string? Section { get; set; }
}


/// <summary>
/// Command for configuring the OXS CLI environment including API connection, authentication, and organization selection.
/// </summary>
public class ConfigureEnvCommand : Command<ConfigureEnvSettings>
{
    /// <summary>
    /// Executes the configure environment command with the specified settings.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The configuration settings for the environment setup.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An exit code indicating success (0) or failure (1).</returns>
    public override int Execute(CommandContext context, ConfigureEnvSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously executes the environment configuration process, including API selection, authentication, and organization setup.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The configuration settings for the environment setup.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with an exit code indicating success (0) or failure (1).</returns>
    private async Task<int> ExecuteAsync(CommandContext context, ConfigureEnvSettings settings, CancellationToken cancellationToken)
    {
        var apis = new[] { "staging", "prod" };

        if (string.IsNullOrWhiteSpace(settings.Api))
        {
            settings.Api = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Please select an [green]api[/]?")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to select the api)[/]")
                    .AddChoices(apis));
        }
        else if (!apis.Contains(settings.Api))
        {
            AnsiConsole.MarkupLine($"[red]Invalid Api. Available options: {string.Join(',', apis)}: {settings.Api}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"Selected api: [green]{settings.Api}[/]");

        if (string.IsNullOrEmpty(settings.EMail))
        {
            settings.EMail = AnsiConsole.Ask<string>("Account [green]mail address[/]?");
            if (string.IsNullOrWhiteSpace(settings.EMail))
            {
                AnsiConsole.MarkupLine($"[red]Account mail address required[/]");
                return 0;
            }
        }

        if (string.IsNullOrEmpty(settings.Password))
        {
            settings.Password = AnsiConsole.Prompt(
                new TextPrompt<string>("Account [green]password[/]?")
                    .PromptStyle("red")
                    .Secret());

            if (string.IsNullOrWhiteSpace(settings.Password))
            {
                AnsiConsole.MarkupLine($"[red]Account password required[/]");
                return 0;
            }
        }

        using (var httpClient = new HttpClient())
        {
            if (settings.Api == "prod")
                httpClient.BaseAddress = new Uri("https://oxs.simplic.io/");
            else if (settings.Api == "staging")
                httpClient.BaseAddress = new Uri("https://dev-oxs.simplic.io/");

            var authClient = new AuthClient(httpClient);
            var token = "";

            try
            {
                var result = authClient.LoginAsync(new LoginRequest { Email = settings.EMail, Password = settings.Password }).GetAwaiter().GetResult();
                token = result.Token;
            }
            catch
            {
                AnsiConsole.MarkupLine($"[red]> Invalid credentials <[/]");
                return 1;
            }

            if (string.IsNullOrEmpty(token))
            {
                return 1;
            }

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var organizationClient = new OrganizationClient(httpClient);
            var organizations = organizationClient.GetForUserAsync().GetAwaiter().GetResult().OrderBy(x => x.OrganizationName).ToList();

            var organizationText = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Please select an [green]organization[/]?")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to select the api)[/]")
                    .AddChoices(organizations.Select(x => x.OrganizationName).ToArray()));

            var organization = organizations.FirstOrDefault(x => x.OrganizationName == organizationText);


            AnsiConsole.MarkupLine($"Selected organization: [green]{organization.OrganizationName}[/]/[yellow]{organization.OrganizationId}[/]");

            var selectResult = authClient.SelectOrganizationAsync(new SelectOrganizationRequest { OrganizationId = organization.OrganizationId }).GetAwaiter().GetResult();

            // Save configuration to .oxs folder
            var configManager = new ConfigurationManager();
            var section = settings.Section ?? "default";

            var configuration = new OxsConfiguration
            {
                Api = settings.Api,
                Email = settings.EMail,
                Organization = organization.OrganizationName,
                OrganizationId = organization.OrganizationId.ToString(),
                Token = selectResult.Token
            };

            await configManager.SaveConfigurationAsync(section, configuration);
            await configManager.SaveCredentialsAsync(section, selectResult.Token);

            AnsiConsole.MarkupLine($"[green]✓[/] Configuration saved to section '[yellow]{section}[/]'");
            Console.WriteLine($"Token: {selectResult.Token}");
        }

        // Omitted
        return 0;
    }
}
