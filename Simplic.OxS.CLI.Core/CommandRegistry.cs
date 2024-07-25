using Spectre.Console.Cli;
using Unity;

namespace Simplic.OxS.CLI.Core
{
    public class CommandRegistry
    {
        private readonly List<ICommandGroup> group = [];
        private readonly Dictionary<Type, List<Type>> modules = [];

        public void Add<T>() where T : ICommandGroup, new() => Add(new T());
        public void Add(ICommandGroup command) => group.Add(command);

        public CommandApp Configure(IUnityContainer container, Action<IConfigurator>? extra = null)
        {
            var app = new CommandApp(new UnityRegistrar(container));
            app.Configure(config =>
            {
                extra?.Invoke(config);
                foreach (var group in group)
                {
                    config.AddBranch(group.Name, config =>
                    {
                        var builder = new CommandGroupBuilder(config, container, [group.Name]);
                        group.Register(builder);
                        foreach (var pair in builder.modules)
                            modules.Add(pair.Key, pair.Value);
                    });
                }
                config.SetInterceptor(new CommandInterceptor(container, modules));
            });
            return app;
        }

        public Task<int> RunAsync(IEnumerable<string> args, IUnityContainer container, Action<IConfigurator>? extra = null)
        {
            return Configure(container, extra).RunAsync(args);
        }
    }
}
