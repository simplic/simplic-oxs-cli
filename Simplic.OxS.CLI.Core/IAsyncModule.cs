namespace Simplic.OxS.CLI.Core
{
    public interface IAsyncModule<in TSettings>
    {
        public Task Execute(TSettings settings);
    }
}
