namespace Simplic.OxS.CLI.Core
{
    public class ModuleBuilder<TSettings>
    {
        internal List<Type> required = [];

        internal ModuleBuilder() { }
        public ModuleBuilder<TSettings> RequireModule<TModule>() where TModule : IAsyncModule<TSettings>
        {
            required.Add(typeof(TModule));
            return this;
        }

    }
}
