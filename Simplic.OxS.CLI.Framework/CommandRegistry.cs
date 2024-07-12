using Spectre.Console.Cli;

namespace Simplic.OxS.CLI.Core
{
    public class CommandRegistry
    {
        private readonly List<ICommandGroup> commands = [];

        public void Add<T>() where T : ICommandGroup, new() => Add(new T());
        public void Add(ICommandGroup command) => commands.Add(command);

        public void Execute(CommandApp app)
        {
            var modules = new List<Type>();
            app.Configure(config =>
            {
                foreach (var command in commands)
                {
                    var builder = new CommandGroupBuilder();
                    command.Register(builder);
                }
            });
        }
    }
}
