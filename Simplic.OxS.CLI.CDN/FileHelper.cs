using Spectre.Console;

namespace Simplic.OxS.CLI.CDN
{
    internal static class FileHelper
    {
        public static string SanitizeFileName(string original) =>
            string.Join("_", original.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }
}
