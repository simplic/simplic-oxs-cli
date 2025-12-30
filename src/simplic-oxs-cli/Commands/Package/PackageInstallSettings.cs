using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Package
{
    public class PackageInstallSettings : PackageSettings
    {
        [Description("Package ID to install")]
        [CommandOption("--package-id <PackageId>")]
        public string? PackageId { get; set; }

        [Description("Version of the package to install")]
        [CommandOption("--version <Version>")]
        public string? Version { get; set; }

        [Description("Directory containing package artifacts (*.zip files with format: <package-id>-<version>.zip)")]
        [CommandOption("--artifact <ArtifactDirectory>")]
        public string? ArtifactDirectory { get; set; }

        [Description("Configuration section to use for installation")]
        [CommandOption("-s|--section <Section>")]
        public string Section { get; set; } = "default";
    }
}