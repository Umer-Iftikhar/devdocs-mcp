namespace DevDocsMcp.Services.Interfaces
{
    public interface IGitHubService
    {
        Task<string> SearchCodeAsync(string repo, string query);
    }
}
