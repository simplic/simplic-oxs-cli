using oxs.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace oxs.Services
{
    public class ConfigurationService
    {
        private readonly ConfigurationManager _configManager;

        public ConfigurationService()
        {
            _configManager = new ConfigurationManager();
        }

        public async Task<OxsConfiguration?> GetConfigurationAsync(string section = "default")
        {
            return await _configManager.LoadConfigurationAsync(section);
        }

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

        public async Task<bool> IsConfiguredAsync(string section = "default")
        {
            var config = await GetConfigurationAsync(section);
            return config != null && !string.IsNullOrEmpty(config.Token);
        }
    }
}