using Simplic.OxS.CLI.Core;
using Simplic.OxS.CLI.Studio.Settings;
using Spectre.Console;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Simplic.OxS.CLI.Studio.Modules
{
    public class PluginLoaderModule : IAsyncModule<IPluginLoaderSettings>
    {
        public Task Execute(IPluginLoaderSettings settings)
        {
            // Add DLL paths
            PluginHelper.RegisterAssemblyLoader(Path.GetFullPath(settings.DllPath));
            PluginHelper.RegisterAssemblyLoader(Path.GetFullPath(RuntimeEnvironment.GetRuntimeDirectory()));

            // Plugin initialization (interactive)
            var plugins = settings.Plugins ?? (IEnumerable<string>)Interactive.SelectPlugins([settings.DllPath, RuntimeEnvironment.GetRuntimeDirectory()]);
            AnsiConsole.MarkupLine("[bold]Selected plugins:[/]");
            foreach (var plugin in plugins)
                AnsiConsole.MarkupLineInterpolated($"[yellow]{plugin}[/]");

            foreach (var plugin in plugins)
            {
                var assembly = Assembly.Load(plugin);
                PluginHelper.Register(assembly);
            }
            PluginHelper.InitializeAll();

            return Task.CompletedTask;
        }
    }
}
