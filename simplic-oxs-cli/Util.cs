using CommonServiceLocator;
using Simplic.Base;
using Simplic.Cache;
using Simplic.Cache.Service;
using Simplic.Configuration;
using Simplic.Configuration.Data;
using Simplic.Configuration.Data.DB;
using Simplic.Configuration.Service;
using Simplic.Framework.DAL;
using Simplic.MessageBroker;
using Simplic.Ox.CLI.Dummy;
using Simplic.Session;
using Simplic.Session.Service;
using Simplic.Sql;
using Simplic.Sql.SqlAnywhere.Service;
using Simplic.Studio.Ox;
using Simplic.Studio.Ox.Data.DB;
using Simplic.Studio.Ox.Service;
using Spectre.Console;
using System.IO;
using System.Reflection;
using Unity;
using Unity.ServiceLocation;

namespace Simplic.Ox.CLI
{
    public static class Util
    {
        public static readonly UnityContainer container = new UnityContainer();

        /// <summary>
        /// Initialize the simplic framework
        /// </summary>
        public static void InitializeFramework()
        {
            AnsiConsole.WriteLine("Initializing framework");
            GlobalSettings.UseIni = false;
            GlobalSettings.UserId = 0;
            GlobalSettings.MainThread = Thread.CurrentThread;
            GlobalSettings.UserName = "ProjectCLI";
        }

        /// <summary>
        /// Set the active connection string
        /// </summary>
        /// <param name="connection"></param>
        public static void SetConnectionString(string connection)
        {
            GlobalSettings.SetPrivateConnectionString(connection);
            GlobalSettings.UserConnectionString = connection;
            DALManager.Init(connection);
        }

        /// <summary>
        /// Initialize dependency injection and register some basic types
        /// </summary>
        public static void InitializeContainer()
        {
            var locator = new UnityServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => locator);

            container.RegisterType<ISqlService, SqlService>();
            container.RegisterType<ISqlColumnService, SqlColumnService>();
            container.RegisterType<ICacheService, CacheService>();
            container.RegisterType<IConnectionConfigurationService, ConnectionConfigurationService>();
            container.RegisterType<IConnectionConfigurationRepository, ConnectionConfigurationRepository>();
            container.RegisterType<IConfigurationService, ConfigurationService>();
            container.RegisterType<IConfigurationRepository, ConfigurationRepository>();
            container.RegisterType<ISessionService, SessionService>();
            container.RegisterType<IMessageBus, MessageBus>();
            container.RegisterType<TenantSystem.IOrganizationService, TenantSystem.Service.OrganizationService>();
            container.RegisterType<TenantSystem.IOrganizationRepository, TenantSystem.Data.DB.OrganizationRepository>();
            container.RegisterType<ISharedIdRepository, SharedIdRepository>();
            container.RegisterType<ISharedIdService, SharedIdService>();
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

        public static void InitializeOx()
        {
            AnsiConsole.WriteLine("Initializing Ox");
            Plugins.RegisterAndInitializeModule<PlugIn.Studio.Ox.Server.FrameworkEntryPoint>();
            AnsiConsole.WriteLine("Initialized Ox");
        }

        public static IEnumerable<IInstanceDataUploadService> GetAllServices()
        {
            return container.ResolveAll<IInstanceDataUploadService>();
        }
    }
}
