namespace DevDocsMcp.Services.Interfaces
{
    public interface IGitHubService
    {
        Task<string> SearchCodeAsync( string repo,string query, CancellationToken cancellationToken = default);
        Task<string> GetFileAsync( string repo,string path,CancellationToken cancellationToken = default);
    }
}
