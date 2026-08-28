namespace DevDocsMcp.Configuration
{
    public class LocalNotesOptions
    {
        public string RootPath { get; set; } = string.Empty;
        public string[] Extensions { get; set; } = [".md", ".txt"];
        public int MaxFileSizeBytes { get; set; } = 1_000_000;
    }
}
