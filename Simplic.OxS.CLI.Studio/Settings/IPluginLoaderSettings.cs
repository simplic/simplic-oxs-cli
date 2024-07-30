namespace Simplic.OxS.CLI.Studio.Settings
{
    public interface IPluginLoaderSettings
    {
        public string DllPath { get; init; }

        public string[]? Plugins { get; init; }
    }
}
