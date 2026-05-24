using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RON_Server_Browser.Models;
using RON_Server_Browser.Services;
using Steamworks;

namespace RON_Server_Browser;

public partial class MainWindow : Window
{
    private SteamManager? _steamManager;
    private SteamLobbyService? _lobbyService;
    private readonly RonConfiguration _config;
    private bool _isConnecting;
    private DispatcherTimer? _gameMonitorTimer;
    private bool _gameWasStarted;
    private readonly LocalizationService _localizationService;

    public MainWindow()
    {
        InitializeComponent();
        _config = RonConfiguration.Load();
        _lobbyService = null;
        _gameWasStarted = false;
        _localizationService = LocalizationService.Instance;
        Loaded += MainWindow_Loaded;
    }

    private void CmbLanguage_Loaded(object sender, RoutedEventArgs e)
    {
        var languages = _localizationService.GetAvailableLanguages();
        foreach (var lang in languages)
        {
            var item = new ComboBoxItem
            {
                Tag = lang,
                Content = _localizationService.GetLanguageDisplayName(lang)
            };
            CmbLanguage.Items.Add(item);
        }

        foreach (ComboBoxItem item in CmbLanguage.Items)
        {
            if (item.Tag is string tag && tag == _config.Language)
            {
                CmbLanguage.SelectedItem = item;
                break;
            }
        }

        _localizationService.CurrentLanguage = _config.Language;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!SteamLauncher.IsGameRunning())
            {
                TxtStatus.Text = _localizationService.GetString("Status.DetectingGame");
                TxtStatus.Foreground = System.Windows.Media.Brushes.Black;

                if (SteamLauncher.StartGame())
                {
                    TxtStatus.Text = _localizationService.GetString("Status.LaunchingGame");
                    _gameWasStarted = true;
                }
                else
                {
                    TxtStatus.Text = _localizationService.GetString("Status.LaunchFailed");
                    TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                }

                await Task.Delay(2000);
            }
            else
            {
                TxtStatus.Text = _localizationService.GetString("Status.GameRunning");
                _gameWasStarted = true;
            }
        }
        catch (Exception ex)
        {
            TxtStatus.Text = _localizationService.GetString("Status.GameException", ex.Message);
            TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private async void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnecting)
            return;

        if (_steamManager?.IsLoggedOn == true)
        {
            Disconnect();
            return;
        }

        _isConnecting = true;
        BtnConnect.IsEnabled = false;
        BtnConnect.Content = _localizationService.GetString("Button.Connecting");

        try
        {
            await ConnectAsync();
        }
        finally
        {
            _isConnecting = false;
            BtnConnect.IsEnabled = true;
        }
    }

    private async Task ConnectAsync()
    {
        try
        {
            _steamManager?.Shutdown();
            _steamManager = new SteamManager();

            _steamManager.OnStatusChanged += status =>
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = status;
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Black;
                });

            _steamManager.OnError += error =>
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = error;
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                });

            if (!_steamManager.Initialize())
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        _localizationService.GetString("Message.Error.SteamConnect"),
                        _localizationService.GetString("Message.Error.SteamConnect"), 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return;
            }

            _lobbyService = new SteamLobbyService(_config);

            _lobbyService.OnStatusChanged += status =>
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = status;
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Black;
                });

            _lobbyService.OnError += error =>
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = error;
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                });

            _lobbyService.OnRefreshingChanged += refreshing =>
                Dispatcher.Invoke(() => UpdateLobbyList());

            Dispatcher.Invoke(() =>
            {
                BtnConnect.Content = _localizationService.GetString("Button.Disconnect");
                BtnRefresh.IsEnabled = true;
                BtnConnect.IsEnabled = true;

                ServerGrid.ItemsSource = _lobbyService.Lobbies;

                TxtStatus.Text = _localizationService.GetString("Status.Connected");
                TxtStatus.Foreground = System.Windows.Media.Brushes.Green;

                StartGameMonitor();
            });

            var steamInstalled = SteamLauncher.IsSteamInstalled();
            if (!steamInstalled)
            {
                MessageBox.Show(_localizationService.GetString("Message.Info.NoSteamClient"),
                    _localizationService.GetString("Message.Info.NoSteamClient"), 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = _localizationService.GetString("Message.Error.RefreshFailed", ex.Message);
                TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
            });
        }
    }

    private void Disconnect()
    {
        StopGameMonitor();

        _steamManager?.Shutdown();
        _steamManager = null;
        _lobbyService = null;

        BtnConnect.Content = _localizationService.GetString("Button.Connect");
        BtnRefresh.IsEnabled = false;
        BtnJoin.IsEnabled = false;

        ServerGrid.ItemsSource = null;

        BtnRefresh.Content = _localizationService.GetString("Button.Refresh");

        TxtStatus.Text = _localizationService.GetString("Status.Ready");
        TxtStatus.Foreground = System.Windows.Media.Brushes.Black;
        TxtServerCount.Text = _localizationService.GetString("StatusBar.ServerCount", 0);
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (_lobbyService == null)
        {
            MessageBox.Show(_localizationService.GetString("Message.Error.NotInitialized"),
                _localizationService.GetString("Message.Error.NotInitialized"), 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (_steamManager?.IsLoggedOn != true)
        {
            MessageBox.Show(_localizationService.GetString("Message.Error.NotConnected"),
                _localizationService.GetString("Message.Error.NotConnected"), 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        BtnRefresh.IsEnabled = false;
        BtnRefresh.Content = _localizationService.GetString("Button.Refreshing");

        try
        {
            int distanceFilter = CmbDistance.SelectedIndex;
            await _lobbyService.RefreshLobbyListAsync(distanceFilter);
        }
        catch (Exception ex)
        {
            MessageBox.Show(_localizationService.GetString("Message.Error.RefreshFailed", ex.Message),
                _localizationService.GetString("Message.Error.RefreshFailed"), 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnRefresh.IsEnabled = true;
            BtnRefresh.Content = _localizationService.GetString("Button.Refresh");
        }
    }

    private void ServerGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        BtnJoin.IsEnabled = ServerGrid.SelectedItem is ServerModel;

        if (ServerGrid.SelectedItem is ServerModel lobby)
        {
            TxtMetadataDetail.Text = string.IsNullOrEmpty(lobby.SessionId)
                ? lobby.MetadataDetail
                : $"{_localizationService.GetString("DataGrid.Header.SessionId")}: {lobby.SessionId}{Environment.NewLine}{Environment.NewLine}{lobby.MetadataDetail}";
        }
        else
        {
            TxtMetadataDetail.Text = _localizationService.GetString("TextBlock.Metadata.Empty");
        }
    }

    private void ServerGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }

    private void ServerGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ServerGrid.SelectedItem is ServerModel lobby)
        {
            JoinLobby(lobby);
        }
    }

    private async void BtnJoin_Click(object sender, RoutedEventArgs e)
    {
        if (ServerGrid.SelectedItem is ServerModel lobby)
        {
            await JoinLobbyAsync(lobby);
        }
    }

    private async Task JoinLobbyAsync(ServerModel lobby)
    {
        if (_lobbyService == null)
            return;

        BtnJoin.IsEnabled = false;
        BtnJoin.Content = _localizationService.GetString("Button.Joining");

        try
        {
            await _lobbyService.JoinLobbyAsync(lobby);
            MessageBox.Show(_localizationService.GetString("Message.Success.JoinLobby", lobby.Name, lobby.SessionId),
                _localizationService.GetString("Message.Success.JoinLobby"), 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(_localizationService.GetString("Message.Error.JoinFailed", ex.Message),
                _localizationService.GetString("Message.Error.JoinFailed"), 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnJoin.IsEnabled = true;
            BtnJoin.Content = _localizationService.GetString("Button.Join");
        }
    }

    private void JoinLobby(ServerModel lobby)
    {
        _ = JoinLobbyAsync(lobby);
    }

    private void UpdateLobbyList()
    {
        if (_lobbyService != null)
        {
            ServerGrid.ItemsSource = _lobbyService.Lobbies;
        }
        UpdateLobbyCount();
    }

    private void UpdateLobbyCount()
    {
        var count = (ServerGrid.ItemsSource as System.Collections.IEnumerable)?
            .Cast<object>().Count() ?? 0;
        TxtServerCount.Text = _localizationService.GetString("StatusBar.ServerCount", count);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        StopGameMonitor();
        _steamManager?.Shutdown();
        _steamManager = null;
    }

    private void StartGameMonitor()
    {
        if (_gameMonitorTimer == null)
        {
            _gameMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _gameMonitorTimer.Tick += GameMonitorTimer_Tick;
        }
        _gameMonitorTimer.Start();
    }

    private void StopGameMonitor()
    {
        if (_gameMonitorTimer != null)
        {
            _gameMonitorTimer.Stop();
            _gameMonitorTimer.Tick -= GameMonitorTimer_Tick;
            _gameMonitorTimer = null;
        }
    }

    private void GameMonitorTimer_Tick(object? sender, EventArgs e)
    {
        if (!_gameWasStarted || _steamManager?.IsLoggedOn != true)
            return;

        if (!SteamLauncher.IsGameRunning())
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = _localizationService.GetString("Status.GameClosed");
                TxtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                StopGameMonitor();
                Close();
            });
        }
    }

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_localizationService != null && CmbLanguage.SelectedItem is ComboBoxItem item && item.Tag is string languageCode)
        {
            _localizationService.CurrentLanguage = languageCode;
            _config.Language = languageCode;
            _config.Save();
        }
    }
}