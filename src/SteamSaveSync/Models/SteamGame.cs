namespace SteamSaveSync.Models;

public sealed class SteamGame
{
    public string Name { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string InstallDirectory { get; init; } = string.Empty;
    public string LibraryPath { get; init; } = string.Empty;
    public string SaveStatus { get; set; } = "Not scanned";
    public IReadOnlyList<string> SavePaths { get; init; } = Array.Empty<string>();
    public bool SyncEnabled { get; set; }
    public string SyncStatus { get; set; } = "Disabled";
}
