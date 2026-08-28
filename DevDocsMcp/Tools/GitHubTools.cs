using DevDocsMcp.Services.Implementations;
using DevDocsMcp.Services.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DevDocsMcp.Tools
{
    [McpServerToolType]
    public class GitHubTools
    {
        private readonly IGitHubService _gitHubService;
        public GitHubTools(IGitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        [McpServerTool(Name = "search_github_code")]
        [Description("Searches a GitHub repository for code matching a query and returns the matching snippet. Use when you don't know which file contains the code.")]
        public Task<string> SearchGitHubCodeAsync(
            [Description("Repository in owner/name format, e.g. dotnet/runtime.")] string repo,
            [Description("Search terms to find in the repository's code.")] string query,
            CancellationToken cancellationToken = default)
        => _gitHubService.SearchCodeAsync(repo, query, cancellationToken);


        [McpServerTool(Name = "get_github_file")]
        [Description("Returns the full contents of one file from a GitHub repository. Use when you already know the file path, e.g. from a previous search result.")]
        public Task<string> GetGitHubFileAsync(
            [Description("Repository in owner/name format, e.g. dotnet/runtime.")] string repo,
            [Description("Path to the file within the repository, e.g. src/Program.cs.")] string path,
            CancellationToken cancellationToken = default)
        => _gitHubService.GetFileAsync(repo, path, cancellationToken);

    }
}
