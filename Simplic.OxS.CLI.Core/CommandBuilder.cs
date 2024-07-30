namespace Simplic.OxS.CLI.Core
{
    public class CommandBuilder<TSettings>
    {
        internal List<string[]> examples = [];
        internal List<Type> modules = [];

        internal CommandBuilder() { }

        public CommandBuilder<TSettings> Example(string[] example)
        {
            examples.Add(example);
            return this;
        }

        public CommandBuilder<TSettings> Depends<TModule>() where TModule : IAsyncModule<TSettings>
        {
            modules.Add(typeof(TModule));
            return this;
        }
    }
}
