using System.Diagnostics;
using System.IO;

namespace RON_Server_Browser.Services;

public static class SteamLauncher
{
    private const string SteamAppId = "1144200";

    public static bool IsSteamInstalled()
    {
        try
        {
            var steamPath = GetSteamPath();
            return !string.IsNullOrEmpty(steamPath) && File.Exists(steamPath);
        }
        catch
        {
            return false;
        }
    }

    public static string? GetSteamPath()
    {
        var paths = new[]
        {
            @"C:\Program Files (x86)\Steam\Steam.exe",
            @"C:\Program Files\Steam\Steam.exe",
            @"D:\Steam\Steam.exe",
            @"E:\Steam\Steam.exe",
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        try
        {
            var key = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Valve\Steam",
                "SteamExe", null);
            if (key is string steamPath && File.Exists(steamPath))
            {
                return steamPath;
            }
        }
        catch
        {
        }

        try
        {
            var key = Microsoft.Win32.Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\Software\Valve\Steam",
                "InstallPath", null);
            if (key is string installPath)
            {
                var steamExe = Path.Combine(installPath, "Steam.exe");
                if (File.Exists(steamExe))
                {
                    return steamExe;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    public static bool JoinServer(string ipAddress, int port, string? password = null)
    {
        try
        {
            var arguments = $"-applaunch {SteamAppId} +connect {ipAddress}:{port}";

            if (!string.IsNullOrEmpty(password))
            {
                arguments += $" +password {password}";
            }

            var steamPath = GetSteamPath();
            if (steamPath == null)
            {
                return false;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = steamPath,
                Arguments = arguments,
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool JoinServerBySteamProtocol(string ipAddress, int port, string? password = null)
    {
        try
        {
            var url = $"steam://connect/{ipAddress}:{port}";

            if (!string.IsNullOrEmpty(password))
            {
                url += $"/{password}";
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsGameRunning()
    {
        try
        {
            var p1 = Process.GetProcessesByName("ReadyOrNot");
            if (p1.Length > 0) return true;

            var p2 = Process.GetProcessesByName("ReadyOrNot-Win64-Shipping");
            if (p2.Length > 0) return true;
        }
        catch
        {
        }
        return false;
    }

    public static bool StartGame()
    {
        try
        {
            var url = $"steam://run/{SteamAppId}";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            try
            {
                var steamPath = GetSteamPath();
                if (steamPath != null)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = steamPath,
                        Arguments = $"-applaunch {SteamAppId}",
                        UseShellExecute = true
                    });
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }
    }
}