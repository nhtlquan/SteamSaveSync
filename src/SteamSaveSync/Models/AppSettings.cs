namespace SteamSaveSync.Models;

public sealed class AppSettings
{
    public string SyncthingApiUrl { get; set; } = "http://127.0.0.1:8384";
    public string SyncthingApiKey { get; set; } = string.Empty;
    public string SharedFolderId { get; set; } = "steamsavesync";
    public string SharedFolderPath { get; set; } = string.Empty;
    public string RemoteDeviceId { get; set; } = string.Empty;
    public string RemoteDeviceName { get; set; } = "MSI Claw";
}
