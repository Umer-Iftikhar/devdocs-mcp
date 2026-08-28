using DevDocsMcp.Configuration;
using DevDocsMcp.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DevDocsMcp.Services.Implementations
{
    public class LocalNotesService : ILocalNotesService
    {
        private const int ContextLinesBefore = 5;
        private const int ContextLinesAfter = 5;
        private const int FallbackLineCount = 20;

        private readonly LocalNotesOptions _options;
        private readonly ILogger<LocalNotesService> _logger;
        private readonly string _rootFullPath;

        #region Constructor
        public LocalNotesService(IOptions<LocalNotesOptions> options, ILogger<LocalNotesService> logger)
        {
            _options = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_options.RootPath))
            {
                throw new InvalidOperationException("LocalNotes:RootPath is not configured.");
            }

            _rootFullPath = Path.GetFullPath(_options.RootPath);
        }
        #endregion

        #region Search Notes
        public async Task<string> SearchNotesAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Query cannot be empty.";
            }

            if (!Directory.Exists(_rootFullPath))
            {
                _logger.LogError("Notes directory not found: {Root}", _rootFullPath);
                return "Notes directory is not available.";
            }

            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string? bestFile = null;
            string[]? bestLines = null;
            var bestLineIndex = -1;
            var bestScore = 0;

            foreach (var file in EnumerateNoteFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var lines = await TryReadLinesAsync(file, cancellationToken);
                if (lines is null)
                {
                    continue;
                }

                for (var i = 0; i < lines.Length; i++)
                {
                    var score = queryWords.Count(word => lines[i].Contains(word, StringComparison.OrdinalIgnoreCase));

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestFile = file;
                        bestLines = lines;
                        bestLineIndex = i;
                    }

                    if (bestScore == queryWords.Length)
                    {
                        break;
                    }
                }

                if (bestScore == queryWords.Length)
                {
                    break;
                }
            }

            if (bestLines is null || bestFile is null)
            {
                _logger.LogWarning("No note matched {Query}", query);
                return "No matching notes found.";
            }

            var relativePath = Path.GetRelativePath(_rootFullPath, bestFile);
            var start = Math.Max(0, bestLineIndex - ContextLinesBefore);
            var end = Math.Min(bestLines.Length, bestLineIndex + ContextLinesAfter + 1);

            var snippet = string.Join('\n', bestLines[start..end]);

            return $"{relativePath} (line {bestLineIndex + 1}):\n{snippet}";
        }
        #endregion

        #region File Enumeration and Reading

        private IEnumerable<string> EnumerateNoteFiles()
        {
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.Hidden
                                | FileAttributes.System
                                | FileAttributes.ReparsePoint
            };

            return Directory
                .EnumerateFiles(_rootFullPath, "*", enumerationOptions)
                .Where(path => _options.Extensions.Contains(
                    Path.GetExtension(path),
                    StringComparer.OrdinalIgnoreCase));
        }

        private async Task<string[]?> TryReadLinesAsync(string path, CancellationToken cancellationToken)
        {
            try
            {
                var info = new FileInfo(path);
                if (info.Length > _options.MaxFileSizeBytes)
                {
                    _logger.LogDebug("Skipping large file {Path}", path);
                    return null;
                }

                var text = await File.ReadAllTextAsync(path, cancellationToken);
                return text
                    .Split('\n')
                    .Select(line => line.TrimEnd('\r'))
                    .ToArray();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not read {Path}", path);
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied reading {Path}", path);
                return null;
            }
        }
        #endregion

        #region Get File
        public async Task<string> GetNoteAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Path cannot be empty.";

            var fullPath = Path.GetFullPath(Path.Combine(_rootFullPath, path));

            if (!fullPath.StartsWith(_rootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path escape attempt: {Path}", path);
                return "Path is outside the notes directory.";
            }

            if (!_options.Extensions.Contains(Path.GetExtension(fullPath), StringComparer.OrdinalIgnoreCase))
                return "Not a note file.";

            if (!File.Exists(fullPath))
                return "Note not found.";

            var lines = await TryReadLinesAsync(fullPath, cancellationToken);
            if (lines is null)
                return "Could not read the note.";

            return string.Join('\n', lines);
        }
        #endregion
    }
}
