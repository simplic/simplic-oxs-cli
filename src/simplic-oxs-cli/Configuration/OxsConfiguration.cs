using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace oxs.Configuration
{
    public class OxsConfiguration
    {
        public string? Api { get; set; }
        public string? Email { get; set; }
        public string? Organization { get; set; }
        public string? OrganizationId { get; set; }
        public string? Token { get; set; }
    }

    public class OxsConfigurationSection
    {
        [JsonPropertyName("api")]
        public string? Api { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("organization")]
        public string? Organization { get; set; }

        [JsonPropertyName("organization_id")]
        public string? OrganizationId { get; set; }
    }

    public class OxsCredentialsSection
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    public class OxsConfigFile
    {
        [JsonPropertyName("sections")]
        public Dictionary<string, OxsConfigurationSection>? Sections { get; set; }
    }

    public class OxsCredentialsFile
    {
        [JsonPropertyName("sections")]
        public Dictionary<string, OxsCredentialsSection>? Sections { get; set; }
    }
}