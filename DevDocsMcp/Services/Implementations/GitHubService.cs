
using DevDocsMcp.DTOs;
using DevDocsMcp.Services.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace DevDocsMcp.Services.Implementations
{
    public class GitHubService : IGitHubService
    {
        private const int MaxResultsToScore = 5;
        private const long MaxFileSizeBytes = 1_000_000;

        private const string TextMatchMediaType = "application/vnd.github.text-match+json";

        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubService> _logger;

        public GitHubService(HttpClient httpClient, ILogger<GitHubService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> SearchCodeAsync(string repo, string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repo) || !repo.Contains('/'))
            {
                return "Repository must be in owner/name format.";
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Query cannot be empty.";
            }
            var safeQuery = StripQualifiers(query);
            if (string.IsNullOrWhiteSpace(safeQuery))
            {
                return "Query contained no searchable terms.";
            }

            var requestUrl =
               $"search/code?q={Uri.EscapeDataString(safeQuery)}+repo:{Uri.EscapeDataString(repo)}" +
               $"&per_page={MaxResultsToScore}";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(TextMatchMediaType));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return DescribeFailure(response, repo);
            }

            var searchResult = await response.Content
                .ReadFromJsonAsync<GitHubSearchResponse>(cancellationToken);

            if (searchResult is null || searchResult.Items.Count == 0)
            {
                _logger.LogInformation("No results for {Query} in {Repo}", safeQuery, repo);
                return "No matching code found.";
            }
            if (searchResult.IncompleteResults)
            {
                _logger.LogWarning("GitHub returned incomplete results for {Query} in {Repo}", safeQuery, repo);
            }
            var queryWords = safeQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            GitHubSearchResult? bestResult = null;
            string? bestFragment = null;
            var bestScore = -1;

            foreach (var item in searchResult.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var textMatch in item.TextMatches)
                {
                    var score = queryWords.Count(word =>
                        textMatch.Fragment.Contains(word, StringComparison.OrdinalIgnoreCase));

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestResult = item;
                        bestFragment = textMatch.Fragment;
                    }
                }
            }

            if (bestResult is null || bestFragment is null)
            {
                var fallback = searchResult.Items[0];
                _logger.LogWarning("No text matches returned for {Query} in {Repo}", safeQuery, repo);
                return $"{fallback.Path} matched, but GitHub returned no snippet. "
                     + $"Use get_github_file to read it.";
            }

            var note = searchResult.IncompleteResults
               ? " (results may be incomplete)"
               : string.Empty;

            return $"{bestResult.Path}{note}:\n{bestFragment.TrimEnd()}";
        }

        public async Task<string> GetFileAsync(string repo, string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(repo) || !repo.Contains('/'))
            {
                return "Repository must be in owner/name format.";
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                return "Path cannot be empty.";
            }

            var escapedPath = string.Join('/', path
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));


            var contentUrl = $"repos/{Uri.EscapeDataString(repo.Split('/')[0])}/"
                           + $"{Uri.EscapeDataString(repo.Split('/')[1])}/contents/{escapedPath}";

            using var response = await _httpClient.GetAsync(contentUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return DescribeFailure(response, repo);
            }
            var file = await response.Content
                .ReadFromJsonAsync<GitHubFileContent>(cancellationToken);

            if (file is null)
            {
                _logger.LogWarning("Null file content for {Path} in {Repo}", path, repo);
                return "Could not read the file.";
            }

            if (!string.Equals(file.Encoding, "base64", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Unsupported encoding {Encoding} for {Path}", file.Encoding, path);
                return $"File is too large or not text ({file.Size} bytes).";
            }

            if (file.Size > MaxFileSizeBytes)
            {
                return $"File is too large to return ({file.Size} bytes).";
            }

            try
            {
                var bytes = Convert.FromBase64String(file.Content);
                var code = Encoding.UTF8.GetString(bytes);

                var lines = code
                   .Split('\n')
                   .Select(line => line.TrimEnd('\r'));

                return string.Join('\n', lines);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Bad base64 for {Path} in {Repo}", path, repo);
                return "Could not decode the file.";
            }
        }

        private static string StripQualifiers(string query)
        {
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !word.Contains(':'));

            return string.Join(' ', words);
        }

        private string DescribeFailure(HttpResponseMessage response, string repo)
        {
            _logger.LogWarning("GitHub returned {Status} for {Repo}", response.StatusCode, repo);

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => "Repository or file not found.",
                HttpStatusCode.UnprocessableEntity => "GitHub rejected the search query.",
                HttpStatusCode.Forbidden => "GitHub rate limit reached. Try again shortly.",
                HttpStatusCode.TooManyRequests => "GitHub rate limit reached. Try again shortly.",
                HttpStatusCode.Unauthorized => "GitHub token is invalid or expired.",
                _ => $"GitHub request failed ({(int)response.StatusCode})."
            };
        }
    }
}
