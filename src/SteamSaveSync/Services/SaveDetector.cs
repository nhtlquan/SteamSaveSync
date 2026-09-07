using SteamSaveSync.Models;

namespace SteamSaveSync.Services;

public sealed class SaveDetector
{
    public SteamGame Detect(SteamGame game)
    {
        var roots = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        var tokens = BuildTokens(game.Name, game.AppId);
        var matches = new List<string>();
        foreach (var root in roots.Where(Directory.Exists))
        {
            try
            {
                foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
                    if (tokens.Any(t => Path.GetFileName(directory).Contains(t, StringComparison.OrdinalIgnoreCase))) matches.Add(directory);
            }
            catch { }
        }

        DateTime? lastSave = null;
        foreach (var path in matches)
        {
            try
            {
                var latest = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Select(File.GetLastWriteTime)
                    .DefaultIfEmpty()
                    .Max();
                if (latest != default && (!lastSave.HasValue || latest > lastSave.Value)) lastSave = latest;
            }
            catch { }
        }

        return new SteamGame
        {
            Name = game.Name,
            AppId = game.AppId,
            InstallDirectory = game.InstallDirectory,
            LibraryPath = game.LibraryPath,
            SaveStatus = matches.Count == 0 ? "Save not found" : matches.Count == 1 ? "Save found" : $"{matches.Count} save folders",
            SavePaths = matches,
            LastSaveTime = lastSave,
            SyncEnabled = matches.Count > 0
        };
    }

    private static IEnumerable<string> BuildTokens(string name, string appId)
    {
        yield return appId;
        var compact = new string(name.Where(char.IsLetterOrDigit).ToArray());
        if (compact.Length >= 3) yield return compact;
        foreach (var part in name.Split(new[] { ' ', '-', ':', '_', '.' }, StringSplitOptions.RemoveEmptyEntries))
            if (part.Length >= 4) yield return part;
    }
}
