using Simplic.Cache;
using Simplic.Cache.Service;
using Simplic.Configuration;
using Simplic.Configuration.Data;
using Simplic.Configuration.Data.DB;
using Simplic.Configuration.Service;
using Simplic.MessageBroker;
using Simplic.Ox.CLI.Dummy;
using Simplic.Session;
using Simplic.Session.Service;
using Simplic.Sql;
using Simplic.Sql.SqlAnywhere.Service;
using Simplic.Studio.Ox;
using Simplic.Studio.Ox.Data.DB;
using Simplic.Studio.Ox.Service;
using Unity;

namespace Simplic.OxS.CLI
{
    public static class Util
    {
        /// <summary>
        /// Initialize dependency injection and register some basic types
        /// </summary>
        public static void InitializeContainer(IUnityContainer container)
        {
            container.RegisterType<ISqlService, SqlService>();
            container.RegisterType<ISqlColumnService, SqlColumnService>();
            container.RegisterType<ICacheService, CacheService>();
            container.RegisterType<IConnectionConfigurationService, ConnectionConfigurationService>();
            container.RegisterType<IConnectionConfigurationRepository, ConnectionConfigurationRepository>();
            container.RegisterType<IConfigurationService, ConfigurationService>();
            container.RegisterType<IConfigurationRepository, ConfigurationRepository>();
            container.RegisterType<ISessionService, SessionService>();
            container.RegisterType<IMessageBus, DummyMessageBus>();
            container.RegisterType<TenantSystem.IOrganizationService, TenantSystem.Service.OrganizationService>();
            container.RegisterType<TenantSystem.IOrganizationRepository, TenantSystem.Data.DB.OrganizationRepository>();
            container.RegisterType<ISharedIdRepository, SharedIdRepository>();
            container.RegisterType<ISharedIdService, SharedIdService>();
        }
    }
}
