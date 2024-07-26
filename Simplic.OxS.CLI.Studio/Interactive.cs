using Spectre.Console;

namespace Simplic.OxS.CLI.Studio
{
    public class Interactive
    {
        private const string LocalConnectionString = "UID=admin;PWD=school;Server=Local;dbn=DocCenter;charset=UTF-8;Links=TCPIP";

        /// <summary>
        /// Lets the user select plugins to load for synchronization
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static List<string> SelectPlugins(IEnumerable<string> paths)
        {
            var plugins = PluginHelper.Scan(paths).Where(p => p.Name is not null).Select(p => p.Name!);

            return AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                .Title("Select Plugins to load")
                .AddChoices(plugins)
                .Required(false));
        }

        /// <summary>
        /// Lets the user select a database connection
        /// </summary>
        /// <returns></returns>
        public static string SelectConnectionString()
        {
            var selConn = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select Connection String")
                    .AddChoices("Local", "Custom")
            );

            if (selConn == "Local")
                return LocalConnectionString;
            else
                return AnsiConsole.Ask<string>("Input connection string");
        }
    }
}
