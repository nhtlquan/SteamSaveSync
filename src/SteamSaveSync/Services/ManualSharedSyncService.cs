using SteamSaveSync.Models;

namespace SteamSaveSync.Services;

public sealed record ManualSyncResult(bool Success, string Message, int Files);

public sealed class ManualSharedSyncService
{
    public string DefaultSharedPath => @"C:\SteamSaveSync";

    public ManualSyncResult ExportToShared(IEnumerable<SteamGame> games, string sharedRoot)
    {
        Directory.CreateDirectory(sharedRoot);
        var count = 0;
        foreach (var game in games.Where(g => g.SavePaths.Count > 0))
        {
            var gameRoot = Path.Combine(sharedRoot, game.AppId, "data");
            if (Directory.Exists(gameRoot)) Directory.Delete(gameRoot, true);
            Directory.CreateDirectory(gameRoot);
            File.WriteAllText(Path.Combine(sharedRoot, game.AppId, "game.json"), System.Text.Json.JsonSerializer.Serialize(new { game.AppId, game.Name, game.LastSaveTime, ExportedAt = DateTimeOffset.Now }));
            foreach (var source in game.SavePaths.Where(Directory.Exists))
                count += CopyDirectory(source, Path.Combine(gameRoot, Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))));
        }
        return new(true, $"Saved {count} file(s) to C:\\SteamSaveSync.", count);
    }

    public ManualSyncResult PullFromDevice(string remoteRoot, string localRoot)
    {
        if (!Directory.Exists(remoteRoot)) return new(false, $"Cannot access {remoteRoot}", 0);
        if (Directory.Exists(localRoot)) Directory.Delete(localRoot, true);
        var files = CopyDirectory(remoteRoot, localRoot);
        return new(true, $"Downloaded {files} file(s) from device.", files);
    }

    public ManualSyncResult RestoreFromShared(IEnumerable<SteamGame> games, string sharedRoot)
    {
        var count = 0;
        foreach (var game in games.Where(g => g.SyncEnabled && g.SavePaths.Count > 0))
        {
            var sourceRoot = Path.Combine(sharedRoot, game.AppId, "data");
            if (!Directory.Exists(sourceRoot)) continue;
            foreach (var destination in game.SavePaths.Where(Directory.Exists))
            {
                var source = Path.Combine(sourceRoot, Path.GetFileName(destination.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
                if (!Directory.Exists(source)) continue;
                var backupRoot = Path.Combine(sharedRoot, "Backups", game.AppId, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                CopyDirectory(destination, backupRoot);
                count += CopyDirectory(source, destination);
            }
        }
        return new(true, $"Synchronized {count} file(s) to game save folders.", count);
    }

    private static int CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        var count = 0;
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
            count++;
        }
        return count;
    }
}
