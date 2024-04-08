using CommonServiceLocator;
using Dapper;
using Simplic.Base;
using Simplic.Cache;
using Simplic.Cache.Service;
using Simplic.Configuration;
using Simplic.Configuration.Data.DB;
using Simplic.Configuration.Service;
using Simplic.Framework.Base;
using Simplic.Framework.DAL;
using Simplic.MessageBroker;
using Simplic.Ox.CLI.Dummy;
using Simplic.Sql;
using Simplic.Sql.SqlAnywhere.Service;
using Simplic.Studio.Ox;
using Spectre.Console;
using System.IO;
using System.Reflection;
using System.Windows;
using Unity;
using Unity.ServiceLocation;

namespace Simplic.Ox.CLI
{
    public static class Util
    {
        public static readonly UnityContainer container = new UnityContainer();

        /// <summary>
        /// Setup the framework
        /// </summary>
        public static void InitializeFramework()
        {
            AnsiConsole.Status().Start("Initializing framework", ctx =>
            {
                Thread.Sleep(1000);
                container.RegisterType<ISqlService, SqlService>();
                container.RegisterType<ISqlColumnService, SqlColumnService>();

                var locator = new UnityServiceLocator(container);
                ServiceLocator.SetLocatorProvider(() => locator);

                // Initialize framework
                GlobalSettings.UseIni = false;
                GlobalSettings.UserId = 0;
                GlobalSettings.MainThread = Thread.CurrentThread;
                GlobalSettings.UserName = "ProjectCLI";

                AnsiConsole.WriteLine("Initialized framework");
            });
        }

        public static void SetConnectionString(string connection)
        {
            GlobalSettings.SetPrivateConnectionString(connection);
            GlobalSettings.UserConnectionString = connection;
            DALManager.Init(connection);
        }

        public static void RegisterTypes()
        {
            IUnityContainer instance = ServiceLocator.Current.GetInstance<IUnityContainer>();
            container.RegisterType<ICacheService, CacheService>();
            container.RegisterType<IConnectionConfigurationService, ConnectionConfigurationService>();
            container.RegisterType<IConnectionConfigurationRepository, ConnectionConfigurationRepository>();
            container.RegisterType<IMessageBus, MessageBus>();
        }

        /// <summary>
        /// Counts all DLLs from the standard paths: /Bin/ and /Bin/[culture].
        /// </summary>
        /// <param name="path">local output path</param>
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
        /// <param name="path">local output path</param>
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

        public static void RegisterAllAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                RegisterAllModules(assembly);
            }
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
            {
                RegisterModule(moduleType!);
            }
        }

        public static void RegisterModule(Type moduleType)
        {
            AnsiConsole.MarkupLineInterpolated($"Registering module: [aqua]{moduleType.FullName}[/]");
            try
            {
                if (!moduleType.IsAssignableTo(typeof(IFrameworkEntryPoint)))
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Module is not an entry point[/]: [aqua]{moduleType.FullName}[/] (No constructor)");
                    return;
                }
                var constructor = moduleType.GetConstructor(Type.EmptyTypes);
                if (constructor is null)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Module has no applicable constructor[/]: [aqua]{moduleType.FullName}[/]");
                    return;
                }
                var module = (IFrameworkEntryPoint)constructor.Invoke(Array.Empty<object>());
                module.Initilize();
                AnsiConsole.MarkupLine("[green]Registered module[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Module load failed[/]");
                AnsiConsole.WriteException(ex);
            }
        }

        public static void RegisterModule<TModule>() where TModule : IFrameworkEntryPoint, new()
        {
            AnsiConsole.MarkupLineInterpolated($"Registering module: [aqua]{typeof(TModule).FullName}[/]");

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

            AnsiConsole.MarkupLine("[green]Registered module[/]");
        }

        public static void InitializeOx()
        {
            AnsiConsole.WriteLine("Initializing Ox");
            AnsiConsole.WriteLine("Initialized Ox");
        }

        public static IEnumerable<IInstanceDataUploadService> GetAllServices()
        {
            return container.ResolveAll<IInstanceDataUploadService>();
        }
    }
}
