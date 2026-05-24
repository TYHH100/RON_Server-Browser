using System;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;

namespace RON_Server_Browser.Services
{
    public class SteamManager
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public event Action<string>? OnStatusChanged;

        public event Action<string>? OnError;

        /// <summary>
        /// Steam 客户端是否已初始化。
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                try
                {
                    return SteamClient.IsValid;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 是否已登录 Steam。
        /// </summary>
        public bool IsLoggedOn
        {
            get
            {
                try
                {
                    return SteamClient.IsValid && SteamClient.IsLoggedOn;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 当前 Steam ID，未登录时为 null。
        /// </summary>
        public SteamId? SteamId
        {
            get
            {
                try
                {
                    return SteamClient.IsValid ? SteamClient.SteamId : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 当前玩家名称。
        /// </summary>
        public string PlayerName
        {
            get
            {
                try
                {
                    return SteamClient.IsValid ? SteamClient.Name : string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public bool Initialize()
        {
            OnStatusChanged?.Invoke(_localization.GetString("Steam.Status.Initializing"));

            try
            {
                SteamClient.Init(1144200, true);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(_localization.GetString("Steam.Error.InitException", ex.Message));
                return false;
            }

            if (!SteamClient.IsValid)
            {
                OnError?.Invoke(_localization.GetString("Steam.Error.InitFailed"));
                return false;
            }

            OnStatusChanged?.Invoke(_localization.GetString("Steam.Status.Connected", PlayerName));
            return true;
        }

        /// <summary>
        /// 关闭 Steam 客户端连接。
        /// </summary>
        public void Shutdown()
        {
            SteamClient.Shutdown();
        }

        /// <summary>
        /// 启动 Steam 回调轮询循环（每 20ms 轮询一次，与 RON 配置一致）。
        /// </summary>
        /// <param name="ct">取消令牌，用于停止轮询循环。</param>
        public async Task StartTickLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                SteamClient.RunCallbacks();
                await Task.Delay(20, ct);
            }
        }
    }
}