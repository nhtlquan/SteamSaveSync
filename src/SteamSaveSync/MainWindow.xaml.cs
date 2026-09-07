using System.Collections.ObjectModel;
using System.Windows;
using SteamSaveSync.Models;
using SteamSaveSync.Services;

namespace SteamSaveSync;

public partial class MainWindow : Window
{
    private readonly SteamScanner _scanner = new();
    private readonly SaveDetector _detector = new();
    private readonly ObservableCollection<SteamGame> _games = new();

    public MainWindow()
    {
        InitializeComponent();
        GamesGrid.ItemsSource = _games;
    }

    private async void ScanSteam_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning Steam and save folders...";
        SummaryText.Text = "Searching installed Steam games and save candidates...";

        try
        {
            var games = await Task.Run(() => _scanner.ScanInstalledGames().Select(_detector.Detect).OrderBy(g => g.Name).ToList());
            _games.Clear();
            foreach (var game in games) _games.Add(game);

            var found = _games.Count(g => g.SavePaths.Count > 0);
            StatusText.Text = "Scan complete";
            SummaryText.Text = $"Found {_games.Count} installed Steam game(s); {found} have save-folder candidates. Select a game to review paths.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Scan failed";
            SummaryText.Text = ex.Message;
        }
    }
}
