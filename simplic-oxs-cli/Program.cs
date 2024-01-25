using CommonServiceLocator;
using Simplic.Studio.Ox;

namespace simplic_oxs_cli
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Util.InitializeFramework();
            Util.AddModule<Simplic.PlugIn.Studio.Ox.Server.FrameworkEntryPoint>();
            Util.InitializeOxS();

            Util.SetConnectionString("UID=admin;PWD=school;Server=sc-dev02;dbn=simplic;ASTART=No;links=tcpip");

            var tenantService = ServiceLocator.Current.GetInstance<ITenantMapService>();
            Console.WriteLine("Getting tenants");
            var tenants = await tenantService.GetStudioMap();
            foreach(var tenant in tenants.Keys)
            {
                Console.WriteLine(tenant);
            }

            Console.WriteLine("Getting services");
            foreach (var service in Util.GetAllServices())
            {
                Console.WriteLine(service.ContextName);
            }
        }
    }
}