using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        try { using var response = await _http.SendAsync(Request(HttpMethod.Get, "/rest/system/status", null, settings)); return response.IsSuccessStatusCode; }
        catch { return false; }
    }

    public async Task<(string Name, string Id)?> GetLocalDeviceAsync(AppSettings settings)
    {
        try
        {
            using var response = await _http.SendAsync(Request(HttpMethod.Get, "/rest/system/status", null, settings));
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var id = doc.RootElement.TryGetProperty("myID", out var value) ? value.GetString() : null;
            return string.IsNullOrWhiteSpace(id) ? null : (Environment.MachineName, id!);
        }
        catch { return null; }
    }
}
