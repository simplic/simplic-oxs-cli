using CommonServiceLocator;
using Dapper;
using Simplic.Framework.Base;
using Simplic.Sql;
using Spectre.Console;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Simplic.Ox.CLI
{
    public class Plugins
    {
        private static readonly IList<IFrameworkEntryPoint> uninitialized = [];

        /// <summary>
        /// Counts all DLLs from the standard paths: /Bin/ and /Bin/[culture].
        /// </summary>
        public static uint CountDlls()
        {
            var sqlService = ServiceLocator.Current.GetInstance<ISqlService>();
            var currentCulture = Thread.CurrentThread.CurrentCulture.Name;
            return sqlService.OpenConnection(connection =>
            {
                return connection.QueryFirst<uint>(
                    $"SELECT COUNT(1) " +
                    $"FROM Repository_Head " +
                    $"WHERE DirectoryPath = '/Bin/' " +
                    $"OR DirectoryPath = '/Bin/{currentCulture}/'");
            });
        }

        /// <summary>
        /// Download all DLLs from the standard paths: /Bin/ and /Bin/[culture].
        /// </summary>
        public static IEnumerable<RepositoryDll> DownloadDlls()
        {
            var sqlService = ServiceLocator.Current.GetInstance<ISqlService>();
            var currentCulture = Thread.CurrentThread.CurrentCulture.Name;
            return sqlService.OpenConnection(connection =>
            {
                return connection.Query<RepositoryDll>(
                    $"SELECT Name, Content " +
                    $"FROM Repository_Head " +
                    $"WHERE DirectoryPath = '/Bin/' " +
                    $"OR DirectoryPath = '/Bin/{currentCulture}/'", buffered: false);
            });
        }

        /// <summary>
        /// Registers multiple assembly loaders
        /// </summary>
        /// <param name="paths"></param>
        public static void RegisterAssemblyLoaders(IEnumerable<string> paths)
        {
            var dllPaths = paths.ToList();
            dllPaths.Add(RuntimeEnvironment.GetRuntimeDirectory());
            foreach (var dllPath in dllPaths)
                RegisterAssemblyLoader(Path.GetFullPath(dllPath));
        }

        /// <summary>
        /// Registers an assembly loader (directory containing DLLs)
        /// </summary>
        /// <param name="folder"></param>
        public static void RegisterAssemblyLoader(string folder)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string assemblyPath = Path.Combine(folder, new AssemblyName(args.Name).Name + ".dll");
                if (!File.Exists(assemblyPath))
                    return null;
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            };
        }

        /// <summary>
        /// Scans given directories for DLLs that can be loaded as PlugIns
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static IList<AssemblyName> Scan(IEnumerable<string> paths)
        {
            // Get all DLLs. Those might not be plugins, but 
            var dlls = paths.Append(RuntimeEnvironment.GetRuntimeDirectory()).SelectMany(p => Directory.EnumerateFiles(p, "*.dll"));
            var dlls2 = dlls.Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
            // Get all potential plugins
            var dllsToScan = dlls.Where(p => Path.GetFileNameWithoutExtension(p).StartsWith("Simplic.PlugIn.")).ToList();

            var resolver = new PathAssemblyResolver(dlls);
            using var mlc = new MetadataLoadContext(resolver);

            var found = new List<AssemblyName>();

            var numDlls = dllsToScan.Count;
            AnsiConsole.Progress().Start(progress =>
            {
                var task = progress.AddTask("Scanning for plugins");
                var i = 0;
                foreach (var file in dllsToScan)
                {
                    Assembly assembly;
                    Type[] types;
                    try
                    {
                        assembly = mlc.LoadFromAssemblyPath(file);
                        types = assembly.GetExportedTypes();
                    }
                    catch { continue; }
                    foreach (var type in types)
                    {
                        try
                        {
                            if (type.GetInterface("IFrameworkEntryPoint") != null)
                                found.Add(assembly.GetName());
                        }
                        catch { }
                    }

                    i++;
                    task.Value = 100 * i / numDlls;
                }
                task.StopTask();
            });

            return found;
        }

        /// <summary>
        /// Loads and registers plugins with the given names
        /// </summary>
        /// <param name="pluginNames"></param>
        public static void Register(IEnumerable<string> pluginNames)
        {
            AnsiConsole.MarkupLine("[bold]Selected plugins:[/]");
            foreach(var plugin in pluginNames)
                AnsiConsole.MarkupLineInterpolated($"[yellow]{plugin}[/]");

            foreach (var plugin in pluginNames)
            {
                var assembly = Assembly.Load(plugin);
                Register(assembly);
            }
        }

        /// <summary>
        /// Registers a single plugin
        /// </summary>
        /// <param name="assembly"></param>
        public static void Register(Assembly assembly)
        {
            Type?[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            var moduleTypes = types.Where(t => t is not null && t.IsAssignableTo(typeof(IFrameworkEntryPoint)));
            foreach (var moduleType in moduleTypes)
                Register(moduleType!);
        }

        /// <summary>
        /// Registers a plugin entrypoint
        /// </summary>
        /// <param name="moduleType"></param>
        public static void Register(Type moduleType)
        {
            AnsiConsole.MarkupLineInterpolated($"Registering module: [yellow]{moduleType.FullName}[/]");
            try
            {
                if (!moduleType.IsAssignableTo(typeof(IFrameworkEntryPoint)))
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Module is not an entry point[/]: [yellow]{moduleType.FullName}[/] (No constructor)");
                    return;
                }
                var constructor = moduleType.GetConstructor(Type.EmptyTypes);
                if (constructor is null)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Module has no applicable constructor[/]: [yellow]{moduleType.FullName}[/]");
                    return;
                }
                uninitialized.Add((IFrameworkEntryPoint)constructor.Invoke(Array.Empty<object>()));
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Module load failed[/]");
                AnsiConsole.WriteException(ex);
            }
        }

        /// <summary>
        /// Initializes all loaded plugins
        /// </summary>
        public static void InitializeAll()
        {
            foreach (var module in uninitialized)
            {
                try
                {
                    AnsiConsole.MarkupLineInterpolated($"Initializing module: [yellow]{module.GetType().FullName}[/]");
                    module.Initilize();
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine("[red]Module init failed[/]");
                    AnsiConsole.WriteException(ex);
                }
            }
            uninitialized.Clear();
        }

        /// <summary>
        /// Registers and intializes a single plugin
        /// </summary>
        /// <typeparam name="TModule"></typeparam>
        public static void RegisterAndInitializeModule<TModule>() where TModule : IFrameworkEntryPoint, new()
        {
            AnsiConsole.MarkupLineInterpolated($"Registering module: [yellow]{typeof(TModule).FullName}[/]");

            try
            {
                var module = new TModule();
                module.Initilize();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Module load failed[/]");
                AnsiConsole.WriteException(ex);
            }
        }
    }
}
