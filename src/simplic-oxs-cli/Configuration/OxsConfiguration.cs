using System.Text.Json.Serialization;

namespace oxs.Configuration;

/// <summary>
/// Represents the complete OXS configuration including API settings, user information, and authentication token.
/// </summary>
public class OxsConfiguration
{
    /// <summary>
    /// Gets or sets the API environment ('prod' or 'staging').
    /// </summary>
    public string? Api { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the organization name.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Gets or sets the organization identifier.
    /// </summary>
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    public string? Token { get; set; }
}

/// <summary>
/// Represents a configuration section stored in the config file, containing API settings and user information.
/// </summary>
public class OxsConfigurationSection
{
    /// <summary>
    /// Gets or sets the API environment ('prod' or 'staging').
    /// </summary>
    [JsonPropertyName("api")]
    public string? Api { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the organization name.
    /// </summary>
    [JsonPropertyName("organization")]
    public string? Organization { get; set; }

    /// <summary>
    /// Gets or sets the organization identifier.
    /// </summary>
    [JsonPropertyName("organization_id")]
    public string? OrganizationId { get; set; }
}

/// <summary>
/// Represents a credentials section stored in the credentials file, containing sensitive authentication information.
/// </summary>
public class OxsCredentialsSection
{
    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

/// <summary>
/// Represents the structure of the configuration file containing multiple named configuration sections.
/// </summary>
public class OxsConfigFile
{
    /// <summary>
    /// Gets or sets the dictionary of configuration sections keyed by section name.
    /// </summary>
    [JsonPropertyName("sections")]
    public Dictionary<string, OxsConfigurationSection>? Sections { get; set; }
}

/// <summary>
/// Represents the structure of the credentials file containing multiple named credential sections.
/// </summary>
public class OxsCredentialsFile
{
    /// <summary>
    /// Gets or sets the dictionary of credential sections keyed by section name.
    /// </summary>
    [JsonPropertyName("sections")]
    public Dictionary<string, OxsCredentialsSection>? Sections { get; set; }
}