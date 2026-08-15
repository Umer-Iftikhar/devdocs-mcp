using System.Text.Json.Serialization;

namespace DevDocsMcp.DTOs
{
    public class GitHubSearchResult
    {
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("repository")]
        public GitHubRepository Repository { get; set; } = new();
    }
}
