using Spectre.Console.Cli;
using Unity;

namespace Simplic.OxS.CLI.Core
{
    public class CommandGroupBuilder
    {
        private readonly IConfigurator<CommandSettings> configurator;
        private readonly IUnityContainer container;
        private readonly string[] path;
        internal readonly Dictionary<Type, List<Type>> modules = [];

        internal CommandGroupBuilder(IConfigurator<CommandSettings> configurator, IUnityContainer container, string[] path)
        {
            this.configurator = configurator;
            this.container = container;
            this.path = path;
        }

        public CommandGroupBuilder Group(string name, Action<CommandGroupBuilder>? action = null)
        {
            configurator.AddBranch(name, configurator =>
            {
                var builder = new CommandGroupBuilder(configurator, container, [.. path, name]);
                action?.Invoke(builder);
                foreach (var pair in builder.modules)
                    modules.Add(pair.Key, pair.Value);
            });
            return this;
        }

        /// <summary>
        /// Register a command
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <typeparam name="TSettings"></typeparam>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public CommandGroupBuilder Command<TCommand, TSettings>(string name, Action<CommandBuilder<TSettings>>? action = null)
            where TCommand : class, ICommandLimiter<TSettings>
            where TSettings : CommandSettings
        {
            var builder = new CommandBuilder<TSettings>();
            action?.Invoke(builder);
            var config = configurator
                .AddCommand<TCommand>(name)
                .WithData(new CommandData { RequiredModules = builder.modules });
            if (builder.example != null)
                config.WithExample([.. path, .. builder.example]);
            return this;
        }

        /// <summary>
        /// Register a module
        /// </summary>
        /// <typeparam name="TModule"></typeparam>
        /// <typeparam name="TSettings"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public CommandGroupBuilder Module<TModule, TSettings>(Action<ModuleBuilder<TSettings>>? action = null) where TModule : IAsyncModule<TSettings>
        {
            var builder = new ModuleBuilder<TSettings>();
            action?.Invoke(builder);
            modules.Add(typeof(TModule), builder.required);
            return this;
        }

        /// <summary>
        /// Register a type for dependency injection
        /// </summary>
        /// <typeparam name="TModule"></typeparam>
        /// <returns></returns>
        public void Inject<TFrom, TTo>() where TTo : TFrom
        {
            container.RegisterType<TFrom, TTo>();
        }
    }
}
