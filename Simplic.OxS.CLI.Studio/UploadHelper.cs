using CommonServiceLocator;
using Simplic.Studio.Ox;
using Spectre.Console;
using Unity;

namespace Simplic.OxS.CLI.Studio
{
    internal class UploadHelper
    {
        /// <summary>
        /// Resolves all registered services for Ox upload. This is done instead of
        /// container.ResolveAll to be able to catch registration errors.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IInstanceDataUploadService> GetUploadServices()
        {
            var container = ServiceLocator.Current.GetInstance<IUnityContainer>();
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
        /// <param name="studioTenant"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public static async Task Upload(IEnumerable<string> contexts, Guid studioTenant, string authToken)
        {
            var sharedIdRepository = ServiceLocator.Current.GetInstance<ISharedIdRepository>();
            var services = GetUploadServices();
            foreach (var context in contexts)
            {
                var service = services.First(s => s.ContextName == context);
                var instanceIds = (await service.GetAllInstanceDataIds(studioTenant)).ToList();
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
                            await UploadData(service, sharedIdRepository, authToken, instanceId, studioTenant);
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

        private async static Task<Guid> UploadData(IInstanceDataUploadService service, ISharedIdRepository sharedIdRepository, string authToken, Guid instanceId, Guid tenantId)
        {
            var sharedId = await sharedIdRepository.GetSharedIdByStudioId(instanceId, service.ContextName, tenantId);

            // Call create method when either no entry for a shared id is found or the oxs id in empty.
            if (sharedId == null || sharedId.OxSId == Guid.Empty)
            {
                var oxsId = await service.CreateUpload(instanceId, authToken);
                if (oxsId == default)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Data not uploaded:[/] context={service.ContextName}  id=[gray]{instanceId}[/]  tenant=[gray]{tenantId}[/]");
                    return oxsId;
                }

                sharedId = new SharedId
                {
                    InstanceDataId = instanceId,
                    OxSId = oxsId,
                    Context = service.ContextName,
                    TenantId = tenantId
                };

                await sharedIdRepository.Save(sharedId);
                return sharedId.OxSId;
            }
            await service.UpdateUpload(sharedId.InstanceDataId, sharedId.OxSId, authToken);
            return sharedId.OxSId;
        }
    }
}
