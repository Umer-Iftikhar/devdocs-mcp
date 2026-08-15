using System.Text.Json.Serialization;

namespace DevDocsMcp.DTOs
{
    public class GitHubRepository
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;
    }
}
