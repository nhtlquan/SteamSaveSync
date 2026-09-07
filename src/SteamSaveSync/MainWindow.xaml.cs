using System.Collections.ObjectModel;
using System.Windows;
using SteamSaveSync.Models;
using SteamSaveSync.Services;

namespace SteamSaveSync;

public partial class MainWindow : Window
{
    private readonly SteamScanner _scanner = new();
    private readonly SaveDetector _detector = new();
    private readonly ManualSharedSyncService _manual = new();
    private readonly ObservableCollection<SteamGame> _games = new();
    private readonly ObservableCollection<RemoteDevice> _devices = new();

    public MainWindow()
    {
        InitializeComponent();
        GamesGrid.ItemsSource = _games;
        DevicesGrid.ItemsSource = _devices;
    }

    private async void ScanSteam_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning Steam and save folders...";
        try
        {
            var games = await Task.Run(() => _scanner.ScanInstalledGames().Select(_detector.Detect).OrderBy(g => g.Name).ToList());
            _games.Clear(); foreach (var game in games) _games.Add(game);
            StatusText.Text = $"Found {_games.Count} Steam games";
            SummaryText.Text = "Last Save Time is based on the latest modified file found in the detected save folder.";
        }
        catch (Exception ex) { StatusText.Text = "Scan failed: " + ex.Message; }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var games = _games.Where(g => g.SyncEnabled).ToList();
        StatusText.Text = "Copying selected saves...";
        var result = await Task.Run(() => _manual.ExportToShared(games, _manual.DefaultSharedPath));
        StatusText.Text = result.Message;
        SummaryText.Text = $"Local storage: {_manual.DefaultSharedPath}";
    }

    private void AddDevice_Click(object sender, RoutedEventArgs e)
    {
        var name = DeviceNameBox.Text.Trim();
        var ip = DeviceIpBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ip))
        {
            DeviceStatusText.Text = "Enter device name and LAN IP."; return;
        }
        _devices.Add(new RemoteDevice { Name = name, IpAddress = ip, Status = "Ready" });
        DeviceNameBox.Clear(); DeviceIpBox.Clear();
        DeviceStatusText.Text = "Device added. Select it and click Get Data.";
    }

    private async void GetDeviceData_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not RemoteDevice device)
        {
            DeviceStatusText.Text = "Please select a device."; return;
        }
        DeviceStatusText.Text = $"Connecting to {device.IpAddress}...";
        var result = await Task.Run(() => _manual.PullFromDevice(device.RemoteSharePath, _manual.DefaultSharedPath));
        device.Status = result.Success ? "Data received" : "Unavailable";
        DevicesGrid.Items.Refresh();
        DeviceStatusText.Text = result.Message;
        SummaryText.Text = result.Success ? "Remote C:\\SteamSaveSync copied to this computer." : result.Message;
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var games = _games.Where(g => g.SyncEnabled).ToList();
        DeviceStatusText.Text = "Synchronizing shared data to game save folders...";
        var result = await Task.Run(() => _manual.RestoreFromShared(games, _manual.DefaultSharedPath));
        DeviceStatusText.Text = result.Message;
        SummaryText.Text = "Current game saves are backed up under C:\\SteamSaveSync\\Backups before overwrite.";
    }
}
