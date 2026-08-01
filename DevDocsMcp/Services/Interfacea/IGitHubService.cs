namespace DevDocsMcp.Services.Interfacea
{
    public interface IGitHubService
    {
        Task<string> SearchCodeAsync(string repo, string query);
    }
}
