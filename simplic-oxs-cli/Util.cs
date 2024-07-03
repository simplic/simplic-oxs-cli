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
using Unity;
using Unity.ServiceLocation;

namespace Simplic.Ox.CLI
{
    public static class Util
    {
        public static readonly UnityContainer container = new();

        /// <summary>
        /// Initialize the simplic framework
        /// </summary>
        public static void InitializeFramework()
        {
            AnsiConsole.WriteLine("Initializing framework");
            GlobalSettings.UseIni = false;
            GlobalSettings.UserId = 0;
            GlobalSettings.MainThread = Thread.CurrentThread;
            GlobalSettings.UserName = "OxCLI";
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

        /// <summary>
        /// Run the Studio-Ox main entry point
        /// </summary>
        public static void InitializeOx()
        {
            AnsiConsole.WriteLine("Initializing Ox");
            Plugins.RegisterAndInitializeModule<PlugIn.Studio.Ox.Server.FrameworkEntryPoint>();
            AnsiConsole.WriteLine("Initialized Ox");
        }

        /// <summary>
        /// Resolves all registered services for Ox upload. This is done instead of
        /// container.ResolveAll to be able to catch registration errors.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IInstanceDataUploadService> GetUploadServices()
        {
            foreach (var registration in container.Registrations)
            {
                if (registration.RegisteredType == typeof(IInstanceDataUploadService))
                {
                    IInstanceDataUploadService? service = null;
                    try
                    {
                        service = container.Resolve<IInstanceDataUploadService>(registration.Name);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.WriteException(ex, ExceptionFormats.NoStackTrace);
                    }
                    if (service != null)
                        yield return service;
                }
            }
        }

        /// <summary>
        /// Runs all synchronization services with the given names. Plugins have to be loaded beforehand.
        /// </summary>
        /// <param name="contexts"></param>
        /// <param name="tenantId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public static async Task SynchronizeContexts(IEnumerable<string> contexts, Guid tenantId, string authToken)
        {
            var sharedIdRepository = ServiceLocator.Current.GetInstance<ISharedIdRepository>();
            var services = GetUploadServices();
            foreach (var context in contexts)
            {
                var service = services.First(s => s.ContextName == context);
                var instanceIds = (await service.GetAllInstanceDataIds(tenantId)).ToList();
                var count = instanceIds.Count;
                AnsiConsole.MarkupLineInterpolated($"Synchronizing {context} ([cyan]{count}[/] items)");

                await AnsiConsole.Progress().StartAsync(async progress =>
                {
                    var task = progress.AddTask("Synchronizing");

                    var i = 0;
                    foreach (var instanceId in instanceIds)
                    {
                        try
                        {
                            await UploadData(service, sharedIdRepository, authToken, instanceId, tenantId);
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.WriteException(ex);
                        }
                        i++;
                        task.Value = 100 * i / count;
                    }
                    task.StopTask();
                });
            }
        }

        private async static Task UploadData(IInstanceDataUploadService service, ISharedIdRepository sharedIdRepository, string authToken, Guid instanceId, Guid tenantId)
        {
            var sharedId = await sharedIdRepository.GetSharedIdByStudioId(instanceId, service.ContextName, tenantId);

            // Call create method when either no entry for a shared id is found or the oxs id in empty.
            if (sharedId == null || sharedId.OxSId == Guid.Empty)
            {
                var oxsId = await service.CreateUpload(instanceId, authToken);
                if (oxsId == default)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Data not uploaded:[/] context={service.ContextName}  id=[gray]{instanceId}[/]  tenant=[gray]{tenantId}[/]");
                    return;
                }

                sharedId = new SharedId
                {
                    InstanceDataId = instanceId,
                    OxSId = oxsId,
                    Context = service.ContextName,
                    TenantId = tenantId
                };

                await sharedIdRepository.Save(sharedId);
                return;
            }
            await service.UpdateUpload(sharedId.InstanceDataId, sharedId.OxSId, authToken);
        }
    }
}
