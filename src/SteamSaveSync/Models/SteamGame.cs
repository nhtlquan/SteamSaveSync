namespace SteamSaveSync.Models;

public sealed class SteamGame
{
    public string Name { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string InstallDirectory { get; init; } = string.Empty;
    public string LibraryPath { get; init; } = string.Empty;
    public string SaveStatus { get; set; } = "Not scanned";
    public IReadOnlyList<string> SavePaths { get; init; } = Array.Empty<string>();
    public bool SyncEnabled { get; set; } = true;
    public string SyncStatus { get; set; } = "Ready";
    public DateTime? LastSaveTime { get; set; }
    public string LastSaveDisplay => LastSaveTime?.ToString("dd/MM/yyyy HH:mm") ?? "No save found";
    public string LogoUrl => string.IsNullOrWhiteSpace(AppId) ? string.Empty : $"https://cdn.cloudflare.steamstatic.com/steam/apps/{AppId}/header.jpg";
}
