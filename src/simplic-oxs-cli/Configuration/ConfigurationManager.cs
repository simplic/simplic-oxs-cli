using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace oxs.Configuration
{
    public class ConfigurationManager
    {
        private static readonly string ConfigDirectoryName = ".oxs";
        private static readonly string ConfigFileName = "config";
        private static readonly string CredentialsFileName = "credentials";

        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly string _credentialsFilePath;

        public ConfigurationManager()
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _configDirectory = Path.Combine(homeDirectory, ConfigDirectoryName);
            _configFilePath = Path.Combine(_configDirectory, ConfigFileName);
            _credentialsFilePath = Path.Combine(_configDirectory, CredentialsFileName);

            EnsureConfigDirectoryExists();
        }

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

        public async Task<List<string>> GetSectionsAsync()
        {
            var config = await LoadConfigFileAsync();
            return config.Sections?.Keys.ToList() ?? new List<string>();
        }

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

        private async Task SaveConfigFileAsync(OxsConfigFile config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_configFilePath, json);
        }

        private async Task SaveCredentialsFileAsync(OxsCredentialsFile credentials)
        {
            var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_credentialsFilePath, json);
        }

        private void EnsureConfigDirectoryExists()
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }
    }
}