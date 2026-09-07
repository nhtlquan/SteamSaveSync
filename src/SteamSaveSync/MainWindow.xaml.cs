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
    private readonly LanTransferClient _lanClient = new();
    private LanTransferServer? _lanServer;
    private readonly ObservableCollection<SteamGame> _games = new();
    private readonly ObservableCollection<RemoteDevice> _devices = new();

    public MainWindow()
    {
        InitializeComponent();
        GamesGrid.ItemsSource = _games;
        DevicesGrid.ItemsSource = _devices;
        Closed += (_, _) => _lanServer?.Dispose();
    }

    private async void ScanSteam_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Scanning Steam and save folders...";
        try { var games = await Task.Run(() => _scanner.ScanInstalledGames().Select(_detector.Detect).OrderBy(g => g.Name).ToList()); _games.Clear(); foreach (var game in games) _games.Add(game); StatusText.Text = $"Found {_games.Count} Steam games"; }
        catch (Exception ex) { StatusText.Text = "Scan failed: " + ex.Message; }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var result = await Task.Run(() => _manual.ExportToShared(_games.Where(g => g.SyncEnabled).ToList(), _manual.DefaultSharedPath));
        StatusText.Text = result.Message;
    }

    private void AddDevice_Click(object sender, RoutedEventArgs e)
    {
        var name = DeviceNameBox.Text.Trim(); var ip = DeviceIpBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ip)) { DeviceStatusText.Text = "Enter device name and LAN IP."; return; }
        _devices.Add(new RemoteDevice { Name = name, IpAddress = ip, Status = "Ready" });
        DeviceStatusText.Text = "Device added. LAN transfer uses HTTP port 5123.";
    }

    private void EnableLanSharing_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _lanServer ??= new LanTransferServer(_manual.DefaultSharedPath, 5123);
            _lanServer.Start();
            ShareStatusText.Text = "LAN server ONLINE on port 5123. Allow Windows Firewall if prompted.";
        }
        catch (Exception ex) { ShareStatusText.Text = "Cannot start LAN server: " + ex.Message; }
    }

    private async void GetDeviceData_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not RemoteDevice device) { DeviceStatusText.Text = "Please select a device."; return; }
        DeviceStatusText.Text = $"Connecting to http://{device.IpAddress}:5123...";
        var result = await _lanClient.PullAsync(device.IpAddress, _manual.DefaultSharedPath);
        device.Status = result.Success ? "Data received" : "Unavailable";
        DevicesGrid.Items.Refresh(); DeviceStatusText.Text = result.Message;
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var result = await Task.Run(() => _manual.RestoreFromShared(_games.Where(g => g.SyncEnabled).ToList(), _manual.DefaultSharedPath));
        DeviceStatusText.Text = result.Message;
    }
}
