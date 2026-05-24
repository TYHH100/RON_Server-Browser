using System;
using System.IO;
using Newtonsoft.Json;

namespace RON_Server_Browser.Models;

public class RonConfiguration
{
    private const string ConfigFileName = "config.json";

    public string GamePath { get; set; } = GetDefaultGamePath();
    public string Language { get; set; } = "zh-CN";

    private static string GetDefaultGamePath()
    {
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string defaultPath = Path.Combine(programFiles, "Steam", "steamapps", "common", "Ready Or Not", "ReadyOrNot.exe");
        
        if (File.Exists(defaultPath))
            return defaultPath;

        return string.Empty;
    }

    public static RonConfiguration Load()
    {
        string configPath = GetConfigPath();
        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<RonConfiguration>(json) ?? new RonConfiguration();
            }
            catch
            {
                return new RonConfiguration();
            }
        }
        return new RonConfiguration();
    }

    public void Save()
    {
        string configPath = GetConfigPath();
        try
        {
            string? directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(configPath, json);
        }
        catch
        {
        }
    }

    private static string GetConfigPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "RON-Server-Browser", ConfigFileName);
    }
}