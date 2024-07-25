namespace Simplic.OxS.CLI.Core
{
    public class CommandBuilder<TSettings>
    {
        internal string[]? example;
        internal List<Type> modules = [];

        internal CommandBuilder() { }

        public CommandBuilder<TSettings> Example(string[]? example)
        {
            this.example = example;
            return this;
        }

        public CommandBuilder<TSettings> RequireModule<TModule>() where TModule : IAsyncModule<TSettings>
        {
            modules.Add(typeof(TModule));
            return this;
        }
    }
}
