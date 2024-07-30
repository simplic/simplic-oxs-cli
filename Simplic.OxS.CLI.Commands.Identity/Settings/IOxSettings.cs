namespace Simplic.OxS.CLI.Identity.Settings
{
    public interface IOxSettings : IUrlSettings
    {
        string? Email { get; init; }
        string? Password { get; init; }

        /// <summary>
        /// This property is set by the login module
        /// </summary>
        Client? AuthClient { get; set; }
    }
}
