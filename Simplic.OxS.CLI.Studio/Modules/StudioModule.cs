using Simplic.Base;
using Simplic.Framework.DAL;
using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Studio.Settings;
using Spectre.Console;

namespace Simplic.OxS.CLI.Studio.Modules
{
    public class StudioModule : IAsyncModule<IStudioSettings>
    {
        public Task Execute(IStudioSettings settings)
        {
            var connection = settings.ConnectionString ?? Interactive.SelectConnectionString();

            // Studio initialization
            AnsiConsole.WriteLine("Initializing framework");
            GlobalSettings.UseIni = false;
            GlobalSettings.UserId = 0;
            GlobalSettings.MainThread = Thread.CurrentThread;
            GlobalSettings.UserName = "OxS_CLI";
            GlobalSettings.SetPrivateConnectionString(connection);
            GlobalSettings.UserConnectionString = connection;
            DALManager.Init(connection);

            return Task.CompletedTask;
        }
    }
}
