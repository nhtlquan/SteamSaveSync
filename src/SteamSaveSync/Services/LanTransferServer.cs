using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace SteamSaveSync.Services;

public sealed class LanTransferServer : IDisposable
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private readonly string _root;
    public bool IsRunning => _listener?.IsListening == true;
    public int Port { get; }

    public LanTransferServer(string root, int port = 5123) { _root = root; Port = port; }

    public void Start()
    {
        if (IsRunning) return;
        Directory.CreateDirectory(_root);
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{Port}/");
        _listener.Start();
        _loop = Task.Run(() => LoopAsync(_cts.Token));
    }

    private async Task LoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener is not null && _listener.IsListening)
        {
            try { var context = await _listener.GetContextAsync(); _ = Task.Run(() => HandleAsync(context)); }
            catch when (token.IsCancellationRequested) { }
            catch { }
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            if (path.Equals("/status", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJson(ctx, new { online = true, machine = Environment.MachineName, port = Port });
                return;
            }
            if (path.Equals("/manifest", StringComparison.OrdinalIgnoreCase))
            {
                var files = Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\Backups\\", StringComparison.OrdinalIgnoreCase))
                    .Select(f => Path.GetRelativePath(_root, f).Replace('\\', '/')).ToList();
                await WriteJson(ctx, files);
                return;
            }
            if (path.StartsWith("/file/", StringComparison.OrdinalIgnoreCase))
            {
                var relative = Uri.UnescapeDataString(path[6..]).Replace('/', Path.DirectorySeparatorChar);
                var rootFull = Path.GetFullPath(_root) + Path.DirectorySeparatorChar;
                var full = Path.GetFullPath(Path.Combine(_root, relative));
                if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) || !File.Exists(full)) { ctx.Response.StatusCode = 404; ctx.Response.Close(); return; }
                ctx.Response.ContentType = "application/octet-stream";
                ctx.Response.ContentLength64 = new FileInfo(full).Length;
                await using var input = File.OpenRead(full); await input.CopyToAsync(ctx.Response.OutputStream); ctx.Response.Close();
                return;
            }
            ctx.Response.StatusCode = 404; ctx.Response.Close();
        }
        catch { try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { } }
    }

    private static async Task WriteJson(HttpListenerContext ctx, object value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        ctx.Response.ContentType = "application/json"; ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes); ctx.Response.Close();
    }

    public void Dispose() { _cts?.Cancel(); if (_listener?.IsListening == true) _listener.Stop(); _listener?.Close(); _cts?.Dispose(); }
}

public sealed class LanTransferClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    public async Task<(bool Success, string Message, int Files)> PullAsync(string ip, string localRoot, int port = 5123)
    {
        try
        {
            var baseUrl = $"http://{ip}:{port}";
            var status = await _http.GetAsync(baseUrl + "/status");
            if (!status.IsSuccessStatusCode) return (false, "Remote device is offline.", 0);
            var manifest = await _http.GetFromJsonAsync<List<string>>(baseUrl + "/manifest") ?? new();
            Directory.CreateDirectory(localRoot); var count = 0;
            foreach (var relative in manifest)
            {
                var safe = relative.Replace('/', Path.DirectorySeparatorChar);
                var target = Path.GetFullPath(Path.Combine(localRoot, safe));
                var root = Path.GetFullPath(localRoot) + Path.DirectorySeparatorChar;
                if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var url = baseUrl + "/file/" + Uri.EscapeDataString(relative);
                var bytes = await _http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(target, bytes); count++;
            }
            return (true, $"Downloaded {count} file(s) via LAN server.", count);
        }
        catch (Exception ex) { return (false, "LAN transfer failed: " + ex.Message, 0); }
    }
}
