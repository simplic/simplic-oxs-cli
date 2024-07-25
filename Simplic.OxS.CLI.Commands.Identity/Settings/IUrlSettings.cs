using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Identity.Settings
{
    public interface IUrlSettings
    {
        [CommandOption("--url")]
        Uri? Uri { get; init; }
    }
}
