using SteamSaveSync.Models;

namespace SteamSaveSync.Services;

public sealed class SaveWatchService : IDisposable
{
    private readonly SaveSyncService _sync;
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CancellationTokenSource> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public event Action<SteamGame, string>? SyncCompleted;

    public SaveWatchService(SaveSyncService sync) => _sync = sync;

    public void Start(IEnumerable<SteamGame> games)
    {
        Stop();
        foreach (var game in games.Where(g => g.SyncEnabled))
        {
            foreach (var path in game.SavePaths.Where(Directory.Exists))
            {
                var key = game.AppId + "|" + path;
                var watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true
                };
                FileSystemEventHandler changed = (_, _) => Schedule(game, key);
                RenamedEventHandler renamed = (_, _) => Schedule(game, key);
                watcher.Changed += changed;
                watcher.Created += changed;
                watcher.Deleted += changed;
                watcher.Renamed += renamed;
                _watchers[key] = watcher;
            }
        }
    }

    private void Schedule(SteamGame game, string key)
    {
        lock (_gate)
        {
            if (_pending.TryGetValue(key, out var old)) { old.Cancel(); old.Dispose(); }
            var cts = new CancellationTokenSource();
            _pending[key] = cts;
            _ = DebouncedSync(game, key, cts);
        }
    }

    private async Task DebouncedSync(SteamGame game, string key, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(4), cts.Token);
            var result = await Task.Run(() => _sync.Sync(game), cts.Token);
            SyncCompleted?.Invoke(game, result.Message);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { SyncCompleted?.Invoke(game, "Watch error: " + ex.Message); }
        finally
        {
            lock (_gate)
            {
                if (_pending.Remove(key, out var current)) current.Dispose();
            }
        }
    }

    public void Stop()
    {
        foreach (var watcher in _watchers.Values) watcher.Dispose();
        _watchers.Clear();
        foreach (var pending in _pending.Values) { pending.Cancel(); pending.Dispose(); }
        _pending.Clear();
    }

    public void Dispose() => Stop();
}
