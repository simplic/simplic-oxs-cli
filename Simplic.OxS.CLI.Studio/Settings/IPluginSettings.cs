namespace Simplic.OxS.CLI.Studio.Settings
{
    public interface IPluginSettings
    {
        public string[] DllPaths { get; init; }

        public string[]? Plugins { get; init; }
    }
}
