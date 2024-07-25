namespace Simplic.OxS.CLI.Core
{
    public interface ICommandGroup
    {
        public void Register(CommandGroupBuilder builder);

        public string Name { get; }
    }
}
