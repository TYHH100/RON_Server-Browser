using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using RON_Server_Browser.Services;

namespace RON_Server_Browser.Models;

public class ServerModel : INotifyPropertyChanged
{
    private static readonly LocalizationService _localization = LocalizationService.Instance;
    private string _name = "";
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    private string _map = "";
    public string Map
    {
        get => _map;
        set { _map = value; OnPropertyChanged(); }
    }

    private string _gameMode = "";
    public string GameMode
    {
        get => _gameMode;
        set { _gameMode = value; OnPropertyChanged(); }
    }

    private int _currentPlayers;
    public int CurrentPlayers
    {
        get => _currentPlayers;
        set { _currentPlayers = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayerDisplay)); }
    }

    private int _maxPlayers;
    public int MaxPlayers
    {
        get => _maxPlayers;
        set { _maxPlayers = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayerDisplay)); }
    }

    public string PlayerDisplay => $"{CurrentPlayers}/{MaxPlayers}";

    private int _ping;
    public int Ping
    {
        get => _ping;
        set { _ping = value; OnPropertyChanged(); }
    }

    private string _hostAddress = "";
    public string HostAddress
    {
        get => _hostAddress;
        set { _hostAddress = value; OnPropertyChanged(); }
    }

    private int _hostPort;
    public int HostPort
    {
        get => _hostPort;
        set { _hostPort = value; OnPropertyChanged(); OnPropertyChanged(nameof(HostDisplay)); }
    }

    public string HostDisplay => string.IsNullOrEmpty(HostAddress) ? "" : $"{HostAddress}:{HostPort}";

    private string _sessionId = "";
    public string SessionId
    {
        get => _sessionId;
        set { _sessionId = value; OnPropertyChanged(); }
    }

    private ulong _ownerId;
    public ulong OwnerId
    {
        get => _ownerId;
        set { _ownerId = value; OnPropertyChanged(); }
    }

    private string _ownerName = "";
    public string OwnerName
    {
        get => _ownerName;
        set { _ownerName = value; OnPropertyChanged(); }
    }

    private string _bucketId = "";
    public string BucketId
    {
        get => _bucketId;
        set { _bucketId = value; OnPropertyChanged(); }
    }

    private string _passwordRequired = "";
    public string PasswordRequired
    {
        get => _passwordRequired;
        set { _passwordRequired = value; OnPropertyChanged(); }
    }

    private int _pingPort;
    public int PingPort
    {
        get => _pingPort;
        set { _pingPort = value; OnPropertyChanged(); }
    }

    private string _version = "";
    public string Version
    {
        get => _version;
        set { _version = value; OnPropertyChanged(); }
    }

    private readonly Dictionary<string, string> _metadata = new();
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    public int MetadataCount => _metadata.Count;

    private string _metadataSummary = "";
    public string MetadataSummary
    {
        get => _metadataSummary;
        private set { _metadataSummary = value; OnPropertyChanged(); }
    }

    private string _metadataDetail = "";
    public string MetadataDetail
    {
        get => _metadataDetail;
        private set { _metadataDetail = value; OnPropertyChanged(); }
    }

    public void SetMetadata(IEnumerable<KeyValuePair<string, string>> entries)
    {
        _metadata.Clear();
        foreach (var (key, value) in entries)
            _metadata[key] = value;

        MetadataSummary = _metadata.Count == 0
            ? _localization.GetString("Metadata.None")
            : string.Join("; ", _metadata.Select(kv => $"{kv.Key}={kv.Value}"));

        MetadataDetail = _metadata.Count == 0
            ? _localization.GetString("Metadata.NoExtra")
            : string.Join(Environment.NewLine, _metadata.Select(kv => $"{kv.Key} = {kv.Value}"));

        OnPropertyChanged(nameof(Metadata));
        OnPropertyChanged(nameof(MetadataCount));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}