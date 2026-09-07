using System.Diagnostics;

namespace SteamSaveSync.Services;

public sealed class SyncthingService
{
    public string? FindExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Syncthing", "syncthing.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Syncthing", "syncthing.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Syncthing", "syncthing.exe")
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    public bool IsAvailable => FindExecutable() is not null;

    public bool Start()
    {
        var exe = FindExecutable();
        if (exe is null) return false;
        try
        {
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, CreateNoWindow = true });
            return true;
        }
        catch { return false; }
    }
}
