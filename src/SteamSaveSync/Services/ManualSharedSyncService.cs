using SteamSaveSync.Models;

namespace SteamSaveSync.Services;

public sealed record ManualSyncResult(bool Success, string Message, int Files);

public sealed class ManualSharedSyncService
{
    public string DefaultSharedPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SteamSaveSync", "Shared");

    public ManualSyncResult ExportToShared(IEnumerable<SteamGame> games, string sharedRoot)
    {
        Directory.CreateDirectory(sharedRoot);
        var count = 0;
        foreach (var game in games.Where(g => g.SavePaths.Count > 0))
        {
            var gameRoot = Path.Combine(sharedRoot, game.AppId);
            Directory.CreateDirectory(gameRoot);
            File.WriteAllText(Path.Combine(gameRoot, "game.json"), System.Text.Json.JsonSerializer.Serialize(new { game.AppId, game.Name, ExportedAt = DateTimeOffset.UtcNow }));
            foreach (var source in game.SavePaths.Where(Directory.Exists))
            {
                var destination = Path.Combine(gameRoot, "data", Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
                if (Directory.Exists(destination)) Directory.Delete(destination, true);
                count += CopyDirectory(source, destination);
            }
        }
        return new(true, $"Exported {count} file(s) to shared folder.", count);
    }

    public ManualSyncResult RefreshSharedFolder(string sharedRoot)
    {
        if (!Directory.Exists(sharedRoot)) return new(false, "Shared folder does not exist.", 0);
        var files = Directory.EnumerateFiles(sharedRoot, "*", SearchOption.AllDirectories).Count();
        return new(true, $"Shared folder is ready. {files} file(s) available.", files);
    }

    public ManualSyncResult RestoreFromShared(IEnumerable<SteamGame> games, string sharedRoot)
    {
        var count = 0;
        foreach (var game in games.Where(g => g.SavePaths.Count > 0))
        {
            var sourceRoot = Path.Combine(sharedRoot, game.AppId, "data");
            if (!Directory.Exists(sourceRoot)) continue;
            foreach (var destination in game.SavePaths.Where(Directory.Exists))
            {
                var source = Path.Combine(sourceRoot, Path.GetFileName(destination.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
                if (!Directory.Exists(source)) continue;
                var backupRoot = Path.Combine(sharedRoot, "Backups", Environment.MachineName, game.AppId, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                CopyDirectory(destination, backupRoot);
                count += CopyDirectory(source, destination);
            }
        }
        return new(true, $"Restored {count} file(s) from shared folder.", count);
    }

    private static int CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        var count = 0;
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
            count++;
        }
        return count;
    }
}
