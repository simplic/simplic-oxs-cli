namespace Simplic.OxS.CLI.Commands.Identity
{
    public interface ILoginSettings : IUrlSettings
    {
        string? Email { get; init; }
        string? Password { get; init; }
    }
}
