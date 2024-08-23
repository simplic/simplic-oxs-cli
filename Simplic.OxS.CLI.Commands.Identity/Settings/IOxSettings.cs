using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Identity.Settings
{
    public interface IOxSettings : IUrlSettings
    {
        [CommandOption("-e|--email <EMAIL>")]
        [Description("Ox user account email")]
        public string? Email { get; init; }

        [CommandOption("-p|--password <PASSWORD>")]
        [Description("Ox user account password")]
        public string? Password { get; init; }

        /// <summary>
        /// This property is set by the login module
        /// </summary>
        public Client? AuthClient { get; set; }
    }
}
