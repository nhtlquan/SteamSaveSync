using System.Security.Cryptography;
using SteamSaveSync.Models;

namespace SteamSaveSync.Services;

public sealed class SaveSyncService
{
    public string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamSaveSync", "SyncedSaves");

    public SyncResult Sync(SteamGame game)
    {
        if (!game.SyncEnabled || game.SavePaths.Count == 0)
            return SyncResult.Disabled();

        var gameRoot = Path.Combine(Root, Sanitize(game.AppId));
        Directory.CreateDirectory(gameRoot);

        var copied = 0;
        foreach (var source in game.SavePaths.Where(Directory.Exists))
        {
            var slot = HashPath(source);
            var target = Path.Combine(gameRoot, slot);
            Directory.CreateDirectory(target);
            copied += MirrorNewer(source, target);
            copied += MirrorNewer(target, source);
        }

        return new SyncResult(true, copied, $"Synced {copied} file change(s)");
    }

    private static int MirrorNewer(string source, string target)
    {
        var changes = 0;
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(target, relative));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destination = Path.Combine(target, relative);
            var sourceInfo = new FileInfo(file);
            var destinationInfo = new FileInfo(destination);

            if (!destinationInfo.Exists || sourceInfo.LastWriteTimeUtc > destinationInfo.LastWriteTimeUtc || sourceInfo.Length != destinationInfo.Length)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, true);
                File.SetLastWriteTimeUtc(destination, sourceInfo.LastWriteTimeUtc);
                changes++;
            }
        }
        return changes;
    }

    private static string HashPath(string value)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value.ToLowerInvariant()));
        return Convert.ToHexString(bytes)[..16];
    }

    private static string Sanitize(string value) => string.Concat(value.Where(char.IsLetterOrDigit));
}

public sealed record SyncResult(bool Success, int Changes, string Message)
{
    public static SyncResult Disabled() => new(false, 0, "Sync disabled or no save folder");
}
