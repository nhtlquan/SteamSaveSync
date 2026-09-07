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
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify),
        };

        var tokens = BuildTokens(game.Name, game.AppId);
        var matches = new List<string>();

        foreach (var root in roots.Where(Directory.Exists))
        {
            try
            {
                foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
                {
                    var leaf = Path.GetFileName(directory);
                    if (tokens.Any(t => leaf.Contains(t, StringComparison.OrdinalIgnoreCase)))
                        matches.Add(directory);
                }
            }
            catch { }
        }

        var status = matches.Count switch
        {
            0 => "Unknown",
            1 => "Candidate found",
            _ => $"{matches.Count} candidates"
        };

        return new SteamGame
        {
            Name = game.Name,
            AppId = game.AppId,
            InstallDirectory = game.InstallDirectory,
            LibraryPath = game.LibraryPath,
            SaveStatus = status,
            SavePaths = matches
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
