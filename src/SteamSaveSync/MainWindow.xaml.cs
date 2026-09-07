using System.Collections.ObjectModel;
using System.Windows;
using SteamSaveSync.Models;
using SteamSaveSync.Services;

namespace SteamSaveSync;

public partial class MainWindow : Window
{
    private readonly SteamScanner _scanner = new();
    private readonly SaveDetector _detector = new();
    private readonly SaveSyncService _sync = new();
    private readonly ObservableCollection<SteamGame> _games = new();

    public MainWindow()
    {
        InitializeComponent();
        GamesGrid.ItemsSource = _games;
    }

    private async void ScanSteam_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning Steam and save folders...";
        try
        {
            var games = await Task.Run(() => _scanner.ScanInstalledGames().Select(_detector.Detect).OrderBy(g => g.Name).ToList());
            _games.Clear();
            foreach (var game in games) _games.Add(game);
            var found = _games.Count(g => g.SavePaths.Count > 0);
            StatusText.Text = "Scan complete";
            SummaryText.Text = $"Found {_games.Count} games; {found} have save candidates.";
        }
        catch (Exception ex) { StatusText.Text = "Scan failed"; SummaryText.Text = ex.Message; }
    }

    private void EnableSelected_Click(object sender, RoutedEventArgs e)
    {
        foreach (SteamGame game in GamesGrid.SelectedItems) if (game.SavePaths.Count > 0) { game.SyncEnabled = true; game.SyncStatus = "Enabled"; }
        GamesGrid.Items.Refresh();
    }

    private async void SyncNow_Click(object sender, RoutedEventArgs e)
    {
        var enabled = _games.Where(g => g.SyncEnabled).ToList();
        StatusText.Text = $"Syncing {enabled.Count} game(s)...";
        var results = await Task.Run(() => enabled.Select(g => (Game:g, Result:_sync.Sync(g))).ToList());
        foreach (var item in results) item.Game.SyncStatus = item.Result.Message;
        GamesGrid.Items.Refresh();
        StatusText.Text = "Sync complete";
        SummaryText.Text = $"Sync staging folder: {_sync.Root}";
    }
}
