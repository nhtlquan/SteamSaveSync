namespace SteamSaveSync.Services;

public sealed class SyncConflictService
{
    private readonly string _backupRoot;

    public SyncConflictService(string root)
    {
        _backupRoot = Path.Combine(root, "Conflicts");
        Directory.CreateDirectory(_backupRoot);
    }

    public string BackupExisting(string appId, string file)
    {
        var relative = file.Replace(':', '_').Replace('\\', '_').Replace('/', '_');
        var folder = Path.Combine(_backupRoot, appId, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, relative);
        File.Copy(file, destination, true);
        return destination;
    }
}
