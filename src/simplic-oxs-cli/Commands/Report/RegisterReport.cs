using Spectre.Console.Cli;

namespace oxs.Commands.Report;

/// <summary>
/// Provides registration methods for report management commands in the CLI application.
/// </summary>
internal static class RegisterReport
{
    /// <summary>
    /// Registers all report management commands with the CLI configurator.
    /// </summary>
    /// <param name="config">The configurator to register commands with.</param>
    public static void RegisterCommands(IConfigurator config)
    {
        config.AddBranch("report", report =>
        {
            report.AddCommand<ReportListCommand>("list");
            report.AddCommand<ReportDownloadCommand>("download");
            report.AddCommand<ReportUploadCommand>("upload");
        });
    }
}
