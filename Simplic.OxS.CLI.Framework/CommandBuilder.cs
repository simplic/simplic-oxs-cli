namespace Simplic.OxS.CLI.Core
{
    public class CommandBuilder
    {
        private string[]? example;
        private List<Type> modules = [];

        public CommandBuilder Example(string[]? example)
        {
            this.example = example;
            return this;
        }

        public CommandBuilder RequiresModule<TModule>() where TModule : IModule
        {
            modules.Add(typeof(TModule));
            return this;
        }
    }
}
