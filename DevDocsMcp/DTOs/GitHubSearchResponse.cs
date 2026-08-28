using System.Text.Json.Serialization;

namespace DevDocsMcp.DTOs
{
    public class GitHubSearchResponse
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("incomplete_results")]
        public bool IncompleteResults { get; set; }
        public List<GitHubSearchResult> Items { get; set; } = new();
    }
}
