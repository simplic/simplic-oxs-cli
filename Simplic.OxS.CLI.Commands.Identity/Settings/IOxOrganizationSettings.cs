namespace Simplic.OxS.CLI.Identity.Settings
{
    public interface IOxOrganizationSettings : IOxSettings
    {
        Guid? OrganizationId { get; init; }
    }
}
