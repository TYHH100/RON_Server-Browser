using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RON_Server_Browser.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private readonly Dictionary<string, string> _translations = new();
    private string _currentLanguage = "zh-CN";
    private const string LanguagesDirectory = "Resources/Languages";

    public static LocalizationService Instance { get; } = new();

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                LoadLanguage(value);
                OnPropertyChanged(nameof(CurrentLanguage));
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? LanguageChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationService()
    {
        LoadLanguage(_currentLanguage);
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void LoadLanguage(string languageCode)
    {
        _translations.Clear();

        string languagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LanguagesDirectory, $"{languageCode}.json");

        if (File.Exists(languagePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(languagePath);
                var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                if (translations != null)
                {
                    foreach (var pair in translations)
                    {
                        _translations[pair.Key] = pair.Value;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        if (_translations.Count == 0)
        {
            LoadFallbackLanguage();
        }
    }

    private void LoadFallbackLanguage()
    {
        string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LanguagesDirectory, "en-US.json");
        if (File.Exists(fallbackPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(fallbackPath);
                var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                if (translations != null)
                {
                    foreach (var pair in translations)
                    {
                        _translations[pair.Key] = pair.Value;
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }

    public string GetString(string key)
    {
        if (_translations.TryGetValue(key, out string? value))
        {
            return value;
        }
        return key;
    }

    public string GetString(string key, params object[] args)
    {
        string baseString = GetString(key);
        try
        {
            return string.Format(baseString, args);
        }
        catch (Exception)
        {
            return baseString;
        }
    }

    public List<string> GetAvailableLanguages()
    {
        string languagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LanguagesDirectory);
        if (!Directory.Exists(languagesDir))
            return new List<string>();

        return Directory.GetFiles(languagesDir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToList();
    }

    public string GetLanguageDisplayName(string languageCode)
    {
        return languageCode switch
        {
            "zh-CN" => "中文",
            "en-US" => "English",
            _ => languageCode
        };
    }
}