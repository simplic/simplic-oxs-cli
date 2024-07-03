using CommonServiceLocator;
using Dapper;
using Simplic.Sql;
using Simplic.Studio.Ox;
using Spectre.Console;

namespace Simplic.Ox.CLI
{
    internal class OxManager
    {
        /// <summary>
        /// Creates an Ox organization in test mode as well
        /// as a Studio organization and links both together
        /// </summary>
        /// <param name="client"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async static Task<OrganizationModel> CreateDummyOrganization(Client client, string name)
        {
            var tenantMapService = ServiceLocator.Current.GetInstance<ITenantMapService>();
            var tenantService = ServiceLocator.Current.GetInstance<TenantSystem.IOrganizationService>();
            var sqlService = ServiceLocator.Current.GetInstance<ISqlService>();

            var organization = await client.CreateDummyOrganization(name, new AddressModel
            {
                City = "Nowhere",
                Street = "Middle",
                HouseNumber = "-1",
                ZipCode = "12345",
                CountryIso = "any",
            });
            var oxsId = organization.Id;
            var studioId = Guid.NewGuid();
            AnsiConsole.MarkupLineInterpolated($"Organization Id={organization.Id} Name={organization.Name}");

            // Using SQL here since many classes from the framework do not work properly in this environment
            sqlService.OpenConnection(conn =>
            {
                try
                {
                    conn.Execute("INSERT INTO Tenant (Id, Name)" +
                                 "VALUES (:studioId, :name)",
                                 new { studioId, name });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Failed to create studio tenant[/] [gray]{studioId}[/]");
                    AnsiConsole.WriteException(ex);
                }
                try
                {
                    conn.Execute("INSERT INTO Tenant_Organization (Id, Name, IsActive, ComputedName)" +
                                 "VALUES (:studioId, :name, 1, :name)",
                                 new { studioId, name });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Failed to create studio organization[/] [gray]{studioId}[/]");
                    AnsiConsole.WriteException(ex);
                }
                try
                {
                    conn.Execute("INSERT INTO OxS_TenantMap (OxsTenantId, StudioTenantId)" +
                                 "VALUES (:oxsId, :studioId)",
                                 new { oxsId, studioId });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Failed to link studio and Ox organization[/] [gray]{studioId}[/]");
                    AnsiConsole.WriteException(ex);
                }
            });

            return organization;
        }

        /// <summary>
        /// Deletes an organization from Ox and Studio, given its Ox id
        /// </summary>
        /// <param name="client"></param>
        /// <param name="oxsId"></param>
        /// <returns></returns>
        public async static Task DeleteDummyOrganization(Client client, Guid oxsId)
        {
            var tenantMapService = ServiceLocator.Current.GetInstance<ITenantMapService>();
            var tenantService = ServiceLocator.Current.GetInstance<TenantSystem.IOrganizationService>();
            var sqlService = ServiceLocator.Current.GetInstance<ISqlService>();

            var studioId = await tenantMapService.GetByOxSTenant(oxsId);

            AnsiConsole.MarkupLineInterpolated($"Deleting OxS organization [gray]{oxsId}[/]");
            await client.DeleteOrganization(oxsId);

            sqlService.OpenConnection(conn =>
            {
                try
                {
                    conn.Execute("DELETE FROM OxS_TenantMap WHERE StudioTenantId = :studioId", new { studioId });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Failed to delete tenant mapping[/] [gray]{studioId}[/]");
                    AnsiConsole.WriteException(ex);
                }

                AnsiConsole.MarkupLineInterpolated($"Deleting studio tenant [gray]{studioId}[/]");
                try
                {
                    conn.Execute("DELETE FROM Tenant_Organization_Tenant WHERE TenantId = :studioId", new { studioId });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Failed to delete studio tenant mapping[/] [gray]{studioId}[/]");
                    AnsiConsole.WriteException(ex);
                }
                try
                {
                    conn.Execute("DELETE FROM Tenant_Organization WHERE Id = :studioId", new { studioId });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Failed to delete studio organization[/] [gray]{studioId}[/]");
                    AnsiConsole.WriteException(ex);
                }
                try
                {
                    conn.Execute("DELETE FROM Tenant WHERE Id = :studioId", new { studioId });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Failed to delete studio tenant[/] [gray]{studioId}[/]");
                    AnsiConsole.WriteException(ex);
                }
            });
        }
    }
}
