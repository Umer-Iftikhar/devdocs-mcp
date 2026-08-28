using DevDocsMcp.Services.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DevDocsMcp.Tools
{
    [McpServerToolType]
    public class FileSystemTools
    {
        private readonly ILocalNotesService _localNotesService;
        public FileSystemTools(ILocalNotesService localNotesService)
        {
            _localNotesService = localNotesService;
        }

        [McpServerTool(Name = "search_local_notes")]
        [Description("Searches the local notes directory for text matching a query and returns the matching lines with surrounding context.")]
        public Task<string> SearchLocalNotesAsync(
            [Description("Search terms to find in the notes.")] string query,
            CancellationToken cancellationToken = default)
            => _localNotesService.SearchNotesAsync(query, cancellationToken);

        [McpServerTool(Name = "get_note")]
        [Description("Returns the full contents of one note by its path relative to the notes root, e.g. 'auth.md' or 'archive/old.md'. Use when you already know the filename.")]
        public Task<string> GetNoteAsync(
            [Description("The path to the note, relative to the notes root.")] string path,
            CancellationToken cancellationToken = default)
            => _localNotesService.GetNoteAsync(path, cancellationToken);
    }
}
