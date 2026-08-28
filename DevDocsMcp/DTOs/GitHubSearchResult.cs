using System.Text.Json.Serialization;

namespace DevDocsMcp.DTOs
{
    public class GitHubSearchResult
    {
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("text_matches")]
        public List<GitHubTextMatch> TextMatches { get; set; } = new();
    }
}
