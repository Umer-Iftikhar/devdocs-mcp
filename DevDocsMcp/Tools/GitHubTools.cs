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

        [McpServerTool]
        [Description("Search for code in a specific GitHub repository and return the relevant code surrounding the match. Use this when the user asks how something was implemented or wants to find code in their repository. Send concise technical keywords rather than a natural-language question.")]
        public async Task<string> SearchCode(
            [Description("The GitHub repository to search, in owner/repository format, for example 'octocat/Hello-World'.")]
            string repo,
            [Description("Concise technical keywords describing the code to find, such as a class name, method name, feature name, or concept. Do not send a full natural-language question; for example, convert 'how did I implement jwt' into 'jwt'.")]
            string query)
        {
            return await _gitHubService.SearchCodeAsync(repo, query);
        }
    }
}
