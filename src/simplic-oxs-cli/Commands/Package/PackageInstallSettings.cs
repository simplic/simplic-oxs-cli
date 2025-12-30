using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs.Commands.Package;

/// <summary>
/// Settings for installing packages from the OXS package registry or from local artifacts.
/// </summary>
public class PackageInstallSettings : PackageSettings
{
    /// <summary>
    /// Gets or sets the package ID to install from the registry.
    /// </summary>
    [Description("Package ID to install")]
    [CommandOption("--package-id <PackageId>")]
    public string? PackageId { get; set; }

    /// <summary>
    /// Gets or sets the version of the package to install.
    /// </summary>
    [Description("Version of the package to install")]
    [CommandOption("--version <Version>")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the directory containing package artifacts in the format: package-id-version.zip.
    /// </summary>
    [Description("Directory containing package artifacts (*.zip files with format: <package-id>-<version>.zip)")]
    [CommandOption("--artifact <ArtifactDirectory>")]
    public string? ArtifactDirectory { get; set; }

    /// <summary>
    /// Gets or sets the configuration section to use for authentication and API settings. Default is 'default'.
    /// </summary>
    [Description("Configuration section to use for installation")]
    [CommandOption("-s|--section <Section>")]
    public string Section { get; set; } = "default";
}