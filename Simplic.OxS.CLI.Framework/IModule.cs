namespace Simplic.OxS.CLI.Core
{
    public interface IModule
    {
        public void Execute(object settings);
    }

    public interface IModule<TSettings> : IModule
    {
        public void Execute(TSettings settings);
    }
}
