using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Identity.Settings
{
    public interface IOxOrganizationSettings : IOxSettings
    {
        [CommandOption("-o|--organization <UUID>")]
        [Description("Ox organization id")]
        Guid? OrganizationId { get; init; }
    }
}
