using oxs.Configuration;
using System.Net.Http.Headers;
using System.Text;

namespace oxs.Services;

/// <summary>
/// Provides HTTP request functionality with OXS configuration integration for making authenticated API calls.
/// </summary>
public class HttpService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ConfigurationManager _configManager;

    /// <summary>
    /// Initializes a new instance of the HttpService class.
    /// </summary>
    public HttpService()
    {
        _httpClient = new HttpClient();
        _configManager = new ConfigurationManager();
    }

    /// <summary>
    /// Executes an HTTP request with the specified parameters using configuration from the given section.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, PATCH, DELETE).</param>
    /// <param name="endpoint">The API endpoint to call.</param>
    /// <param name="section">The configuration section to use for authentication and API settings.</param>
    /// <param name="body">The request body content. Use $filename to load from file or provide inline content.</param>
    /// <param name="headers">Additional headers in format 'Key:Value;Key2:Value2'.</param>
    /// <returns>A task representing the asynchronous operation with the HTTP service result.</returns>
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

    /// <summary>
    /// Parses a header string and adds the headers to the HTTP client's default request headers.
    /// </summary>
    /// <param name="headers">Header string in format 'Key:Value;Key2:Value2'.</param>
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

    /// <summary>
    /// Releases all resources used by the HttpService.
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Represents the result of an HTTP service operation, including success status, response data, and error information.
/// </summary>
public class HttpServiceResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the HTTP request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the request failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code returned by the server.
    /// </summary>
    public System.Net.HttpStatusCode? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response body content as a string.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Gets or sets the response headers as a dictionary of header names and values.
    /// </summary>
    public Dictionary<string, string>? ResponseHeaders { get; set; }

    /// <summary>
    /// Gets or sets the content type of the response.
    /// </summary>
    public string? ContentType { get; set; }
}