using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SteamSaveSync.Models;

namespace SteamSaveSync.Services;

public sealed class SyncthingApiClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    private HttpRequestMessage Request(HttpMethod method, string url, string? json, AppSettings settings)
    {
        var request = new HttpRequestMessage(method, settings.SyncthingApiUrl.TrimEnd('/') + url);
        if (!string.IsNullOrWhiteSpace(settings.SyncthingApiKey)) request.Headers.Add("X-API-Key", settings.SyncthingApiKey);
        if (json is not null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return request;
    }

    public async Task<bool> IsOnlineAsync(AppSettings settings)
    {
        try
        {
            using var response = await _http.SendAsync(Request(HttpMethod.Get, "/rest/system/status", null, settings));
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<(string Name, string Id)?> GetLocalDeviceAsync(AppSettings settings)
    {
        try
        {
            using var response = await _http.SendAsync(Request(HttpMethod.Get, "/rest/system/status", null, settings));
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var id = root.TryGetProperty("myID", out var myId) ? myId.GetString() : null;
            var name = root.TryGetProperty("guiAddressUsed", out var gui) ? gui.GetString() : Environment.MachineName;
            return string.IsNullOrWhiteSpace(id) ? null : (name ?? Environment.MachineName, id!);
        }
        catch { return null; }
    }

    public async Task<bool> AddRemoteDeviceAsync(AppSettings settings, string deviceId, string name)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return false;
        var config = await GetConfigAsync(settings);
        if (config is null) return false;
        if (!config.RootElement.GetProperty("devices").EnumerateArray().Any(d => d.GetProperty("deviceID").GetString()?.Equals(deviceId, StringComparison.OrdinalIgnoreCase) == true))
        {
            var node = JsonNode.Parse(config.RootElement.GetRawText())!.AsObject();
            var devices = node["devices"]!.AsArray();
            devices.Add(new JsonObject { ["deviceID"] = deviceId, ["name"] = name, ["addresses"] = new JsonArray("dynamic"), ["compression"] = "metadata", ["introducer"] = false, ["paused"] = false });
            return await PutConfigAsync(settings, node.ToJsonString());
        }
        return true;
    }

    public async Task<bool> EnsureSharedFolderAsync(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.RemoteDeviceId)) return false;
        Directory.CreateDirectory(settings.SharedFolderPath);
        var config = await GetConfigAsync(settings);
        if (config is null) return false;
        var node = JsonNode.Parse(config.RootElement.GetRawText())!.AsObject();
        var folders = node["folders"]!.AsArray();
        var existing = folders.FirstOrDefault(f => f? ["id"]?.GetValue<string>() == settings.SharedFolderId) as JsonObject;
        var devices = new JsonArray(new JsonObject { ["deviceID"] = settings.RemoteDeviceId, ["introducedBy"] = "" });
        if (existing is null)
        {
            folders.Add(new JsonObject { ["id"] = settings.SharedFolderId, ["label"] = "SteamSaveSync", ["path"] = settings.SharedFolderPath, ["type"] = "sendreceive", ["devices"] = devices, ["fsWatcherEnabled"] = true, ["rescanIntervalS"] = 3600 });
        }
        else
        {
            existing["path"] = settings.SharedFolderPath;
            existing["devices"] = devices;
            existing["paused"] = false;
        }
        return await PutConfigAsync(settings, node.ToJsonString());
    }

    public async Task<string> GetConnectionStatusAsync(AppSettings settings)
    {
        try
        {
            using var response = await _http.SendAsync(Request(HttpMethod.Get, "/rest/system/connections", null, settings));
            if (!response.IsSuccessStatusCode) return "Syncthing API error";
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (string.IsNullOrWhiteSpace(settings.RemoteDeviceId)) return "Remote device not configured";
            if (!doc.RootElement.TryGetProperty("connections", out var connections) || !connections.TryGetProperty(settings.RemoteDeviceId, out var remote)) return "Remote device offline";
            return remote.TryGetProperty("connected", out var connected) && connected.GetBoolean() ? "Remote device online" : "Remote device offline";
        }
        catch { return "Syncthing offline"; }
    }

    private async Task<JsonDocument?> GetConfigAsync(AppSettings settings)
    {
        try
        {
            using var response = await _http.SendAsync(Request(HttpMethod.Get, "/rest/config", null, settings));
            return response.IsSuccessStatusCode ? JsonDocument.Parse(await response.Content.ReadAsStringAsync()) : null;
        }
        catch { return null; }
    }

    private async Task<bool> PutConfigAsync(AppSettings settings, string json)
    {
        try
        {
            using var response = await _http.SendAsync(Request(HttpMethod.Put, "/rest/config", json, settings));
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
