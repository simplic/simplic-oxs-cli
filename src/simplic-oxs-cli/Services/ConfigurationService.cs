using oxs.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace oxs.Services
{
    /// <summary>
    /// Provides configuration-related services including configuration retrieval and authenticated HTTP client creation.
    /// </summary>
    public class ConfigurationService
    {
        private readonly ConfigurationManager _configManager;

        /// <summary>
        /// Initializes a new instance of the ConfigurationService class.
        /// </summary>
        public ConfigurationService()
        {
            _configManager = new ConfigurationManager();
        }

        /// <summary>
        /// Gets the OXS configuration for the specified section.
        /// </summary>
        /// <param name="section">The configuration section name. Defaults to 'default'.</param>
        /// <returns>A task representing the asynchronous operation, returning the configuration or null if not found.</returns>
        public async Task<OxsConfiguration?> GetConfigurationAsync(string section = "default")
        {
            return await _configManager.LoadConfigurationAsync(section);
        }

        /// <summary>
        /// Creates an authenticated HttpClient configured with the appropriate base address and authorization headers.
        /// </summary>
        /// <param name="section">The configuration section to use for authentication. Defaults to 'default'.</param>
        /// <returns>A task representing the asynchronous operation, returning an authenticated HttpClient or null if configuration is invalid.</returns>
        public async Task<HttpClient?> GetAuthenticatedHttpClientAsync(string section = "default")
        {
            var config = await GetConfigurationAsync(section);
            if (config == null || string.IsNullOrEmpty(config.Token))
                return null;

            var httpClient = new HttpClient();
            
            // Set base address based on API
            if (config.Api == "prod")
                httpClient.BaseAddress = new System.Uri("https://oxs.simplic.io/");
            else if (config.Api == "staging")
                httpClient.BaseAddress = new System.Uri("https://dev-oxs.simplic.io/");

            // Set authorization header
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);

            return httpClient;
        }

        /// <summary>
        /// Checks whether the specified configuration section is properly configured with a valid token.
        /// </summary>
        /// <param name="section">The configuration section to check. Defaults to 'default'.</param>
        /// <returns>A task representing the asynchronous operation, returning true if configured; otherwise, false.</returns>
        public async Task<bool> IsConfiguredAsync(string section = "default")
        {
            var config = await GetConfigurationAsync(section);
            return config != null && !string.IsNullOrEmpty(config.Token);
        }
    }
}