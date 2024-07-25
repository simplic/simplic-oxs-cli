namespace Simplic.OxS.CLI.Identity.Settings
{
    public interface ISelectOrganizationSettings : ILoginSettings
    {
        Guid? OrganizationId { get; init; }
    }
}
