using CommonServiceLocator;
using Simplic.Studio.Ox;
using Spectre.Console;

namespace Simplic.OxS.CLI.Studio
{
    public class Interactive
    {
        private const string LocalConnectionString = "UID=admin;PWD=school;Server=Local;dbn=DocCenter;charset=UTF-8;Links=TCPIP";

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
        /// Lets the user select which data to upload and in what order.
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public async static Task Upload(Guid tenantId, string authToken)
        {
            var sharedIdRepository = ServiceLocator.Current.GetInstance<ISharedIdRepository>();

            AnsiConsole.WriteLine("Getting services");
            var services = UploadHelper.GetUploadServices().ToList();
            var contexts = services.Select(s => s.ContextName).ToList();

            var contextsToSync = new List<string>();
            uint order = 1;
            while (contexts.Count > 0)
            {
                var context = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title("Select context to synchronise")
                    .AddChoices("-- Start Sync --")
                    .AddChoices(contexts));

                if (context == "-- Start Sync --")
                    break;

                try
                {
                    AnsiConsole.MarkupLineInterpolated($"[bold]Selected context:[/] [gray]{order} ->[/] [yellow]{context}[/]");
                    contexts.Remove(context);
                    contextsToSync.Add(context);
                    order += 1;
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
            }

            bool rerun = false;
            do
            {
                await UploadHelper.Upload(contextsToSync, tenantId, authToken);
                rerun = AnsiConsole.Confirm("Rerun sync", rerun);
            } while (rerun);
        }

        /// <summary>
        /// Lets the user select a studio tenant
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static Guid EnterTenant() => AnsiConsole.Ask<Guid>("[bold magenta]Enter tenant id[/][gray]>[/]");
    }
}
