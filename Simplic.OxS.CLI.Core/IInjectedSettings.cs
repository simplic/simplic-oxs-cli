using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Core
{
    /// <summary>
    /// These properties will be injected into every settings class generated
    /// </summary>
    public interface IInjectedSettings
    {
        [CommandOption("-P|--profile <NAME>")]
        [Description("Use a profile to fill out some arguments")]
        public string[]? Profiles { get; init; }

        [CommandOption("--profile-add <NAME>")]
        [Description("Add current arguments to a profile")]
        public string? AddProfile { get; init; }

        [CommandOption("--profile-store <NAME>")]
        [Description("Save current arguments to a profile")]
        public string? StoreProfile { get; init; }
    }
}
