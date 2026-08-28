namespace DevDocsMcp.Services.Interfaces
{
    public interface ILocalNotesService
    {
        Task<string> SearchNotesAsync(string query, CancellationToken cancellationToken = default);
        Task<string> GetNoteAsync(string path, CancellationToken cancellationToken = default);
    }
}
