using Simplic.OxS.CLI.Core;

namespace Simplic.OxS.CLI.Commands.Identity
{
    public class GetTokenCommand : ICustomCommand<GetTokenCommand.Settings>
    {

        public class Settings : ILoginSettings
        {
            public string? Url { get; init; }
            public string? Email { get; init; }
            public string? Password { get; init; }
        }
    }
}
