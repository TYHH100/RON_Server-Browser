using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using RON_Server_Browser.Models;

namespace RON_Server_Browser.Services
{
    public class SteamLobbyService
    {
        private readonly RonConfiguration _config;
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ObservableCollection<ServerModel> Lobbies { get; } = new();

        public event Action<string>? OnStatusChanged;
        public event Action<string>? OnError;
        public event Action<bool>? OnRefreshingChanged;

        public SteamLobbyService(RonConfiguration config)
        {
            _config = config;
        }

        public async Task RefreshLobbyListAsync(int distanceFilter = 3)
        {
            try
            {
                if (!SteamClient.IsValid || !SteamClient.IsLoggedOn)
                    return;
            }
            catch
            {
                return;
            }

            OnRefreshingChanged?.Invoke(true);

            try
            {
                Lobbies.Clear();

                var query = SteamMatchmaking.LobbyList;
                
                switch (distanceFilter)
                {
                    case 0: // Close
                        query.FilterDistanceClose();
                        break;
                    case 1: // Default
                        // 不设置筛选，使用默认距离
                        break;
                    case 2: // Far
                        query.FilterDistanceFar();
                        break;
                    case 3: // Worldwide
                        query.FilterDistanceWorldwide();
                        break;
                }
                
                query.WithMaxResults(50);

                Lobby[] lobbies = await query.RequestAsync();

                if (lobbies != null)
                {
                    foreach (Lobby lobby in lobbies)
                    {
                        await RefreshLobbyDataAsync(lobby);
                        var model = CreateLobbyModel(lobby);
                        Lobbies.Add(model);
                    }
                }

                string distanceText = distanceFilter switch
                {
                    0 => _localization.GetString("ComboBox.Distance.Closest"),
                    1 => _localization.GetString("ComboBox.Distance.Default"),
                    2 => _localization.GetString("ComboBox.Distance.Far"),
                    3 => _localization.GetString("ComboBox.Distance.Worldwide"),
                    _ => _localization.GetString("ComboBox.Distance.Worldwide")
                };
                
                OnStatusChanged?.Invoke(_localization.GetString("Lobby.Status.Found", Lobbies.Count, distanceText));
            }
            catch (Exception ex)
            {
                OnError?.Invoke(_localization.GetString("Lobby.Error.RefreshFailed", ex.Message));
            }
            finally
            {
                OnRefreshingChanged?.Invoke(false);
            }
        }

        public async Task RefreshLobbyDataAsync(Lobby lobby)
        {
            try
            {
                lobby.Refresh();
            }
            catch
            {
                // 刷新失败时忽略，继续使用已有数据
            }

            await Task.CompletedTask;
        }

        public ServerModel CreateLobbyModel(Lobby lobby)
        {
            var metadata = lobby.Data.ToDictionary(kv => kv.Key, kv => kv.Value);

            var model = new ServerModel
            {
                CurrentPlayers = lobby.MemberCount,
                MaxPlayers = lobby.MaxMembers,
                SessionId = lobby.Id.ToString(),
                BucketId = lobby.Id.AccountId.ToString(),
                OwnerId = lobby.Owner.Id
            };

            model.SetMetadata(metadata);
            ApplyMetadataToModel(model, metadata);

            if (string.IsNullOrWhiteSpace(model.Name))
                model.Name = _localization.GetString("Lobby.DefaultName", lobby.Id);

            uint gameServerIp = 0;
            ushort gameServerPort = 0;
            SteamId gameServerId = default;
            if (lobby.GetGameServer(ref gameServerIp, ref gameServerPort, ref gameServerId))
            {
                if (gameServerIp != 0)
                    model.HostAddress = $"{gameServerIp >> 24 & 0xFF}.{gameServerIp >> 16 & 0xFF}.{gameServerIp >> 8 & 0xFF}.{gameServerIp & 0xFF}";
                if (gameServerPort != 0)
                    model.HostPort = gameServerPort;
            }

            return model;
        }

        private static void ApplyMetadataToModel(ServerModel model, Dictionary<string, string> metadata)
        {
            if (TryGetMetadata(metadata, out var name, "name", "servername", "hostname"))
                model.Name = name;
                
            if (TryGetMetadata(metadata, out var ownerName, "OWNINGNAME", "owningname", "ownername"))
                model.OwnerName = ownerName;
        }

        private static bool TryGetMetadata(
            Dictionary<string, string> metadata,
            out string value,
            params string[] keys)
        {
            foreach (var key in keys)
            {
                if (metadata.TryGetValue(key, out var found) && !string.IsNullOrWhiteSpace(found))
                {
                    value = found;
                    return true;
                }
            }

            value = "";
            return false;
        }

        public async Task JoinLobbyAsync(ServerModel lobby)
        {
            try
            {
                if (!SteamClient.IsValid)
                    return;
            }
            catch
            {
                return;
            }

            if (ulong.TryParse(lobby.SessionId, out var lobbyId))
            {
                var steamId = new SteamId { Value = lobbyId };
                var result = await SteamMatchmaking.JoinLobbyAsync(steamId);

                if (result == null)
                    throw new Exception(_localization.GetString("Lobby.Error.JoinFailed"));

                result.Value.Leave();

                JoinLobbyViaSteamProtocol(lobbyId);
            }
        }

        private void JoinLobbyViaSteamProtocol(ulong lobbyId)
        {
            string steamUri = $"steam://joinlobby/1144200/{lobbyId}";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = steamUri,
                    UseShellExecute = true
                });
                OnStatusChanged?.Invoke(_localization.GetString("Lobby.Status.Joining"));
            }
            catch (Exception ex)
            {
                OnError?.Invoke(_localization.GetString("Lobby.Error.SteamProtocolFailed", ex.Message));
            }
        }
    }
}