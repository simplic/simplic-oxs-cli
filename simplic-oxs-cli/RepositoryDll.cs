namespace Simplic.Ox.CLI
{
    public class RepositoryDll
    {
        /// <summary>
        /// Name of the file
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Content of the file
        /// </summary>
        public byte[] Content { get; set; } = [];
    }
}
