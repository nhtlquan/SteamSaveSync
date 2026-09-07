using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using SteamSaveSync.Models;
using SteamSaveSync.Services;

namespace SteamSaveSync;

public partial class MainWindow : Window
{
    private readonly SteamScanner _scanner = new();
    private readonly SaveDetector _detector = new();
    private readonly ManualSharedSyncService _manual = new();
    private readonly ObservableCollection<SteamGame> _games = new();

    public MainWindow()
    {
        InitializeComponent();
        GamesGrid.ItemsSource = _games;
        SharedPathBox.Text = _manual.DefaultSharedPath;
    }

    private async void ScanSteam_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning Steam and save folders...";
        try
        {
            var games = await Task.Run(() => _scanner.ScanInstalledGames().Select(_detector.Detect).OrderBy(g => g.Name).ToList());
            _games.Clear();
            foreach (var game in games) _games.Add(game);
            StatusText.Text = "Scan complete";
            SummaryText.Text = $"Found {_games.Count} game(s). Select the games to include.";
        }
        catch (Exception ex) { StatusText.Text = "Scan failed"; SummaryText.Text = ex.Message; }
    }

    private void EnableSelected_Click(object sender, RoutedEventArgs e)
    {
        foreach (SteamGame game in GamesGrid.SelectedItems)
            if (game.SavePaths.Count > 0) { game.SyncEnabled = true; game.SyncStatus = "Included"; }
        GamesGrid.Items.Refresh();
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var games = _games.Where(g => g.SyncEnabled).ToList();
        StatusText.Text = "Copying saves to shared folder...";
        var result = await Task.Run(() => _manual.ExportToShared(games, SharedPathBox.Text));
        foreach (var game in games) game.SyncStatus = result.Success ? "Exported to shared" : result.Message;
        GamesGrid.Items.Refresh(); StatusText.Text = result.Message;
    }

    private async void RefreshShared_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Checking shared folder...";
        var result = await Task.Run(() => _manual.RefreshSharedFolder(SharedPathBox.Text));
        StatusText.Text = result.Message;
        SummaryText.Text = "Data is available locally after Syncthing finishes updating the shared folder.";
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var games = _games.Where(g => g.SyncEnabled).ToList();
        StatusText.Text = "Copying shared data to game saves...";
        var result = await Task.Run(() => _manual.RestoreFromShared(games, SharedPathBox.Text));
        foreach (var game in games) game.SyncStatus = result.Success ? "Restored from shared" : result.Message;
        GamesGrid.Items.Refresh(); StatusText.Text = result.Message;
    }

    private void BrowseShared_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { InitialDirectory = Directory.Exists(SharedPathBox.Text) ? SharedPathBox.Text : null };
        if (dialog.ShowDialog() == true) SharedPathBox.Text = dialog.FolderName;
    }
}
