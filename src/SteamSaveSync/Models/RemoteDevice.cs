namespace SteamSaveSync.Models;

public sealed class RemoteDevice
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public string RemoteSharePath => $@"\\{IpAddress}\c$\SteamSaveSync";
}
