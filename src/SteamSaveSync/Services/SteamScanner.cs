using System.Text.RegularExpressions;
using SteamSaveSync.Models;

namespace SteamSaveSync.Services;

public sealed class SteamScanner
{
    private static readonly string[] DefaultSteamRoots =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
    };

    public IReadOnlyList<SteamGame> ScanInstalledGames()
    {
        var roots = FindSteamRoots();
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            libraries.Add(root);
            var libraryFile = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraryFile))
            {
                foreach (Match match in Regex.Matches(File.ReadAllText(libraryFile), "\\\"path\\\"\\s+\\\"(?<path>[^\\\"]+)\\\""))
                {
                    var path = match.Groups["path"].Value.Replace("\\\\", "\\");
                    if (Directory.Exists(path)) libraries.Add(path);
                }
            }
        }

        var games = new List<SteamGame>();
        foreach (var library in libraries)
        {
            var steamApps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(steamApps)) continue;

            foreach (var manifest in Directory.EnumerateFiles(steamApps, "appmanifest_*.acf"))
            {
                var text = File.ReadAllText(manifest);
                var appId = GetValue(text, "appid");
                var name = GetValue(text, "name");
                var installDir = GetValue(text, "installdir");

                if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(name)) continue;

                games.Add(new SteamGame
                {
                    Name = name,
                    AppId = appId,
                    LibraryPath = library,
                    InstallDirectory = Path.Combine(steamApps, "common", installDir ?? string.Empty),
                    SaveStatus = "Not scanned"
                });
            }
        }

        return games
            .GroupBy(g => g.AppId)
            .Select(g => g.First())
            .ToList();
    }

    private static IEnumerable<string> FindSteamRoots()
    {
        foreach (var root in DefaultSteamRoots.Where(Directory.Exists))
            yield return root;

        var customRoot = Environment.GetEnvironmentVariable("STEAM_PATH");
        if (!string.IsNullOrWhiteSpace(customRoot) && Directory.Exists(customRoot))
            yield return customRoot;
    }

    private static string? GetValue(string content, string key)
    {
        var match = Regex.Match(content, $"\\\"{Regex.Escape(key)}\\\"\\s+\\\"(?<value>[^\\\"]*)\\\"", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["value"].Value : null;
    }
}
