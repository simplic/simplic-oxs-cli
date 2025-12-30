using System.Text.Json;

namespace oxs.Configuration;

/// <summary>
/// Manages OXS CLI configuration and credentials files in the user's home directory.
/// </summary>
public class ConfigurationManager
{
    private static readonly string ConfigDirectoryName = ".oxs";
    private static readonly string ConfigFileName = "config";
    private static readonly string CredentialsFileName = "credentials";

    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private readonly string _credentialsFilePath;

    /// <summary>
    /// Initializes a new instance of the ConfigurationManager class and ensures the configuration directory exists.
    /// </summary>
    public ConfigurationManager()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configDirectory = Path.Combine(homeDirectory, ConfigDirectoryName);
        _configFilePath = Path.Combine(_configDirectory, ConfigFileName);
        _credentialsFilePath = Path.Combine(_configDirectory, CredentialsFileName);

        EnsureConfigDirectoryExists();
    }

    /// <summary>
    /// Saves the specified OXS configuration for a given section.
    /// </summary>
    /// <param name="section">The configuration section name (e.g., 'default', 'staging').</param>
    /// <param name="configuration">The OXS configuration to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public async Task SaveConfigurationAsync(string section, OxsConfiguration configuration)
    {
        var config = await LoadConfigFileAsync();

        if (config.Sections == null)
            config.Sections = new Dictionary<string, OxsConfigurationSection>();

        config.Sections[section] = new OxsConfigurationSection
        {
            Api = configuration.Api,
            Email = configuration.Email,
            Organization = configuration.Organization,
            OrganizationId = configuration.OrganizationId
        };

        await SaveConfigFileAsync(config);
    }

    /// <summary>
    /// Saves the authentication token for the specified section.
    /// </summary>
    /// <param name="section">The configuration section name.</param>
    /// <param name="token">The authentication token to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public async Task SaveCredentialsAsync(string section, string token)
    {
        var credentials = await LoadCredentialsFileAsync();

        if (credentials.Sections == null)
            credentials.Sections = new Dictionary<string, OxsCredentialsSection>();

        credentials.Sections[section] = new OxsCredentialsSection
        {
            Token = token
        };

        await SaveCredentialsFileAsync(credentials);
    }

    /// <summary>
    /// Loads the complete OXS configuration for the specified section, combining configuration and credentials.
    /// </summary>
    /// <param name="section">The configuration section name to load. Defaults to 'default'.</param>
    /// <returns>A task representing the asynchronous load operation, returning the OXS configuration or null if not found.</returns>
    public async Task<OxsConfiguration?> LoadConfigurationAsync(string section = "default")
    {
        var config = await LoadConfigFileAsync();
        var credentials = await LoadCredentialsFileAsync();

        if (config.Sections?.TryGetValue(section, out var configSection) == true &&
            credentials.Sections?.TryGetValue(section, out var credentialsSection) == true)
        {
            return new OxsConfiguration
            {
                Api = configSection.Api,
                Email = configSection.Email,
                Organization = configSection.Organization,
                OrganizationId = configSection.OrganizationId,
                Token = credentialsSection.Token
            };
        }

        return null;
    }

    /// <summary>
    /// Gets a list of all available configuration sections.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, returning a list of section names.</returns>
    public async Task<List<string>> GetSectionsAsync()
    {
        var config = await LoadConfigFileAsync();
        return config.Sections?.Keys.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Loads the configuration file from disk, creating an empty configuration if the file doesn't exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, returning the configuration file contents.</returns>
    private async Task<OxsConfigFile> LoadConfigFileAsync()
    {
        if (!File.Exists(_configFilePath))
            return new OxsConfigFile();

        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath);
            return JsonSerializer.Deserialize<OxsConfigFile>(json) ?? new OxsConfigFile();
        }
        catch
        {
            return new OxsConfigFile();
        }
    }

    /// <summary>
    /// Loads the credentials file from disk, creating an empty credentials structure if the file doesn't exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, returning the credentials file contents.</returns>
    private async Task<OxsCredentialsFile> LoadCredentialsFileAsync()
    {
        if (!File.Exists(_credentialsFilePath))
            return new OxsCredentialsFile();

        try
        {
            var json = await File.ReadAllTextAsync(_credentialsFilePath);
            return JsonSerializer.Deserialize<OxsCredentialsFile>(json) ?? new OxsCredentialsFile();
        }
        catch
        {
            return new OxsCredentialsFile();
        }
    }

    /// <summary>
    /// Saves the configuration file to disk in JSON format.
    /// </summary>
    /// <param name="config">The configuration file contents to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    private async Task SaveConfigFileAsync(OxsConfigFile config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_configFilePath, json);
    }

    /// <summary>
    /// Saves the credentials file to disk in JSON format.
    /// </summary>
    /// <param name="credentials">The credentials file contents to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    private async Task SaveCredentialsFileAsync(OxsCredentialsFile credentials)
    {
        var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_credentialsFilePath, json);
    }

    /// <summary>
    /// Ensures the configuration directory exists in the user's home directory, creating it if necessary.
    /// </summary>
    private void EnsureConfigDirectoryExists()
    {
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
        }
    }
}