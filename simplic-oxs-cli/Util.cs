using CommonServiceLocator;
using Simplic.Base;
using Simplic.Framework.Base;
using Simplic.Framework.DAL;
using Simplic.Sql;
using Simplic.Sql.SqlAnywhere.Service;
using Simplic.Studio.Ox;
using Unity;
using Unity.ServiceLocation;

namespace simplic_oxs_cli
{
    public static class Util
    {
        public static readonly UnityContainer container = new UnityContainer();

        /// <summary>
        /// Setup the framework
        /// </summary>
        public static void InitializeFramework()
        {
            Console.WriteLine("Initializing framework...");

            container.RegisterType<ISqlService, SqlService>();
            var locator = new UnityServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => locator);

            // Initialize framework
            GlobalSettings.UseIni = false;
            GlobalSettings.UserId = 0;
            GlobalSettings.MainThread = Thread.CurrentThread;
            GlobalSettings.UserName = "ProjectCLI";

            Console.WriteLine("Initialized framework");
        }

        public static void SetConnectionString(string connection)
        {
            GlobalSettings.SetPrivateConnectionString(connection);
            GlobalSettings.UserConnectionString = connection;
            DALManager.Init(connection);
        }

        public static void AddModule<TModule>() where TModule : IFrameworkEntryPoint, new()
        {
            Console.WriteLine($"Registering module: {typeof(TModule).FullName}");

            var module = new TModule();
            module.Initilize();

            Console.WriteLine($"Registered module");
        }

        public static void InitializeOxS()
        {
            Console.WriteLine("Initializing OxS");
            Console.WriteLine("Initialized OxS");
        }

        public static IEnumerable<IInstanceDataUploadService> GetAllServices()
        {
            return container.ResolveAll<IInstanceDataUploadService>();
        }
    }
}
