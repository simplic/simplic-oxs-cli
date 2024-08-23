using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Identity.Settings;
using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Datahub.Commands
{
    public class DatahubUserCreateCommand : IAsyncCommand<DatahubUserCreateCommand.ISettings>
    {
        public async Task<int> ExecuteAsync(CommandContext context, ISettings settings)
        {
            var username = settings.Username ?? Interactive.EnterUsername();
            var password = settings.Password ?? Interactive.EnterPassword();

            using var httpClient = new HttpClient { BaseAddress = settings.Uri };
            var client = new DatahubClient(httpClient);
            var response = await client.UserPOSTAsync(new CreateUserRequest
            {
                Name = username,
                Password = password,
                IsActive = true,
            });

            Console.WriteLine(response.ApiKey);

            return 0;
        }

        public interface ISettings : IUrlSettings
        {
            [CommandOption("-n|--username")]
            string? Username { get; set; }

            [CommandOption("-p|--password")]
            string? Password { get; set; }
        }
    }
}
