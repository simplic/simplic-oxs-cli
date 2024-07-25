using Spectre.Console.Cli;
using Unity;

namespace Simplic.OxS.CLI.Core
{
    internal class CommandInterceptor(IUnityContainer container, Dictionary<Type, List<Type>> modules) : ICommandInterceptor
    {
        /// <summary>
        /// Execute all modules required by the command
        /// </summary>
        /// <param name="context"></param>
        /// <param name="settings"></param>
        void ICommandInterceptor.Intercept(CommandContext context, CommandSettings settings)
        {
            var data = (CommandData)context.Data!;
            var alreadyInvoked = new HashSet<Type>();
            foreach (var module in data.RequiredModules)
                InvokeModule(module, settings, alreadyInvoked);
        }

        private void InvokeModule(Type type, CommandSettings settings, HashSet<Type> alreadyInvoked)
        {
            // Prevent modules from being executed multiple times
            if (alreadyInvoked.Contains(type))
                return;

            var required = modules[type];
            foreach (var module in required)
                InvokeModule(module, settings, alreadyInvoked);
            var instance = container.Resolve(type);
            var method = instance.GetType().GetMethod("Execute");
            var task = (Task)method!.Invoke(instance, [settings])!;
            task.GetAwaiter().GetResult();
        }
    }
}
