namespace Simplic.OxS.CLI.Core
{
    public class CommandGroupBuilder
    {
        private readonly List<Type> modules = [];

        public CommandGroupBuilder Command<TCommand>(string name, Action<CommandBuilder> action) where TCommand : ICustomCommand
        {
            var builder = new CommandBuilder();
            action(builder);
            return this;
        }

        public CommandGroupBuilder Module<TModule>()
        {
            modules.Add(typeof(TModule));
            return this;
        }

        internal List<Type> Modules => modules;
    }
}
