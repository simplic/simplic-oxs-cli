namespace Simplic.OxS.CLI.Core
{
    /// <summary>
    /// This exception is thrown to return from a command interceptor without showing an exception
    /// </summary>
    public class CancelCommandException : Exception
    {
    }
}
