using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Commands.Identity
{
    public interface IUrlSettings
    {
        [CommandOption("--url")]
        string? Url { get; init; }
    }
}
