namespace Simplic.OxS.CLI.Core
{
    public static class FormatHelper
    {
        private const long Multiplier = 1024;
        private const long Threshold = 2048;
        private readonly static string[] ByteSuffixes = ["kiB", "MiB", "GiB", "TiB"];

        public static string FormatByteSize(long bytes)
        {
            if (Math.Abs(bytes) < Threshold)
                return $"{bytes}B";
            bytes /= Multiplier;
            foreach (var suffix in ByteSuffixes)
            {
                if (Math.Abs(bytes) < Threshold)
                    return $"{bytes:F2}{suffix}";
                bytes /= Multiplier;
            }
            return $"{bytes:F2}PiB";
        }
    }
}
