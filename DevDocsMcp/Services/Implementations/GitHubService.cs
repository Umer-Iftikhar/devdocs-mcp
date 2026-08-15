
using DevDocsMcp.DTOs;
using DevDocsMcp.Services.Interfaces;

namespace DevDocsMcp.Services.Implementations
{
    public class GitHubService : IGitHubService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubService> _logger;
        public GitHubService(HttpClient httpClient, ILogger<GitHubService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<string> SearchCodeAsync(string repo, string query)
        {
            var requestUrl = $"search/code?q={Uri.EscapeDataString(query)}+repo:{Uri.EscapeDataString(repo)}";
            var searchResponse = await _httpClient.GetAsync(requestUrl);

            searchResponse.EnsureSuccessStatusCode();

            var searchResult = await searchResponse.Content.ReadFromJsonAsync<GitHubSearchResponse>();

            var result = searchResult?.Items?.FirstOrDefault();

            if (result == null)
            {
                _logger.LogWarning("No code result found for {Query} in {Repo}", query, repo);
                return "No matching code found.";
            }

            var contentUrl = $"repos/{result.Repository.FullName}/contents/{result.Path}";
            var contentResponse = await _httpClient.GetAsync(contentUrl);
            contentResponse.EnsureSuccessStatusCode();


            var file = await contentResponse.Content.ReadFromJsonAsync<GitHubFileContent>();

            if (file == null)
            {
                _logger.LogWarning("File content is null for {Repo} and {Query}", repo, query);
                return "Could not get file content.";
            }

            var content = file.Content.Replace("\n", "").Replace("\r", "");

            var bytes = Convert.FromBase64String(content);
            var code = System.Text.Encoding.UTF8.GetString(bytes);

            var lines = code.Split('\n');

            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var matchIndex = Array.FindIndex(lines, line =>
                queryWords.Any(word => line.Contains(word, StringComparison.OrdinalIgnoreCase)));

            if (matchIndex == -1)
            {
                _logger.LogWarning("No match found for {Query} in {Repo}", query, repo);
                return string.Join('\n', lines.Take(20));
            }

            var start = Math.Max(0, matchIndex - 5);
            var end = Math.Min(lines.Length, matchIndex + 6);

            return string.Join('\n', lines[start..end]);

        }
    }
}
