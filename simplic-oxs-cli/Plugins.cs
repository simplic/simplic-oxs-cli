using CommonServiceLocator;
using Dapper;
using Simplic.Framework.Base;
using Simplic.Sql;
using Spectre.Console;
using System.IO;
using System.Reflection;

namespace Simplic.Ox.CLI
{
    public class Plugins
    {
        private static readonly IList<IFrameworkEntryPoint> uninitialized = new List<IFrameworkEntryPoint>();

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

        public static IList<AssemblyName> Scan(IEnumerable<string> paths)
        {
            // Get all DLLs. Those might not be plugins, but 
            var dlls = paths.SelectMany(p => Directory.EnumerateFiles(p, "*.dll"));
            // Get all potential plugins
            var dllsToScan = dlls.Where(p => p.StartsWith("Simplic.PlugIn.")).ToList();

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

        public static void RegisterAllAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
                RegisterAllModules(assembly);
        }

        public static void RegisterAllModules(Assembly assembly)
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
                RegisterModule(moduleType!);
        }

        public static void RegisterModule(Type moduleType)
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

        public static void InitializeAllModules()
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
