using oxs.Configuration;
using Spectre.Console;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace oxs.Services
{
    public class HttpService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigurationManager _configManager;

        public HttpService()
        {
            _httpClient = new HttpClient();
            _configManager = new ConfigurationManager();
        }

        public async Task<HttpServiceResult> ExecuteRequestAsync(
            string method, 
            string endpoint, 
            string section, 
            string? body = null,
            string? headers = null)
        {
            try
            {
                // Load configuration
                var config = await _configManager.LoadConfigurationAsync(section);
                if (config == null)
                {
                    return new HttpServiceResult
                    {
                        Success = false,
                        ErrorMessage = $"Configuration not found for section '{section}'. Please run 'oxs configure env' first."
                    };
                }

                // Set base URL
                if (config.Api == "prod")
                    _httpClient.BaseAddress = new Uri("https://oxs.simplic.io/");
                else if (config.Api == "staging")
                    _httpClient.BaseAddress = new Uri("https://dev-oxs.simplic.io/");
                else
                {
                    return new HttpServiceResult
                    {
                        Success = false,
                        ErrorMessage = $"Unknown API environment: {config.Api}"
                    };
                }

                // Set authorization header
                if (!string.IsNullOrWhiteSpace(config.Token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", config.Token);
                }

                // Parse and add custom headers
                if (!string.IsNullOrWhiteSpace(headers))
                {
                    ParseAndAddHeaders(headers);
                }

                // Prepare request content
                HttpContent? content = null;
                if (!string.IsNullOrWhiteSpace(body) && (method.ToLower() == "post" || method.ToLower() == "put" || method.ToLower() == "patch"))
                {
                    string bodyContent;
                    
                    // Check if body is a file path (starts with $)
                    if (body.StartsWith("$"))
                    {
                        var filePath = body.Substring(1);
                        if (!File.Exists(filePath))
                        {
                            return new HttpServiceResult
                            {
                                Success = false,
                                ErrorMessage = $"File not found: {filePath}"
                            };
                        }
                        bodyContent = await File.ReadAllTextAsync(filePath);
                    }
                    else
                    {
                        bodyContent = body;
                    }

                    content = new StringContent(bodyContent, Encoding.UTF8, "application/json");
                }

                // Execute request
                HttpResponseMessage response = method.ToLower() switch
                {
                    "get" => await _httpClient.GetAsync(endpoint),
                    "delete" => await _httpClient.DeleteAsync(endpoint),
                    "post" => await _httpClient.PostAsync(endpoint, content),
                    "put" => await _httpClient.PutAsync(endpoint, content),
                    "patch" => await _httpClient.PatchAsync(endpoint, content),
                    _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
                };

                var responseBody = await response.Content.ReadAsStringAsync();
                var responseHeaders = response.Headers.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => string.Join(", ", kvp.Value)
                );

                return new HttpServiceResult
                {
                    Success = true,
                    StatusCode = response.StatusCode,
                    ResponseBody = responseBody,
                    ResponseHeaders = responseHeaders,
                    ContentType = response.Content.Headers.ContentType?.MediaType
                };
            }
            catch (Exception ex)
            {
                return new HttpServiceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private void ParseAndAddHeaders(string headers)
        {
            var headerPairs = headers.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var headerPair in headerPairs)
            {
                var colonIndex = headerPair.IndexOf(':');
                if (colonIndex > 0 && colonIndex < headerPair.Length - 1)
                {
                    var key = headerPair.Substring(0, colonIndex).Trim();
                    var value = headerPair.Substring(colonIndex + 1).Trim();
                    
                    if (_httpClient.DefaultRequestHeaders.Contains(key))
                    {
                        _httpClient.DefaultRequestHeaders.Remove(key);
                    }
                    _httpClient.DefaultRequestHeaders.Add(key, value);
                }
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class HttpServiceResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public System.Net.HttpStatusCode? StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public Dictionary<string, string>? ResponseHeaders { get; set; }
        public string? ContentType { get; set; }
    }
}