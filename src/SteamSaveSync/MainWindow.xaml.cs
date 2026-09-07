using System.Collections.ObjectModel;
using SteamSaveSync.Models;
using SteamSaveSync.Services;

namespace SteamSaveSync;

public partial class MainWindow : Window
{
    private readonly SteamScanner _scanner = new();
    private readonly ObservableCollection<SteamGame> _games = new();

    public MainWindow()
    {
        InitializeComponent();
        GamesGrid.ItemsSource = _games;
    }

    private async void ScanSteam_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning Steam libraries...";
        SummaryText.Text = "Searching installed Steam games...";

        try
        {
            var games = await Task.Run(() => _scanner.ScanInstalledGames());
            _games.Clear();
            foreach (var game in games.OrderBy(g => g.Name))
                _games.Add(game);

            StatusText.Text = "Scan complete";
            SummaryText.Text = $"Found {_games.Count} installed Steam game(s). Save detection will be added next.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Scan failed";
            SummaryText.Text = ex.Message;
        }
    }
}
