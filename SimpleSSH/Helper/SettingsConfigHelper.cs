using System.IO;
using System.Text.Json;
using iNKORE.UI.WPF.Modern.Controls;

namespace SimpleSSH.Helper;

public static class SettingsConfigHelper
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "SimpleSSH", "AppConfig.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    static SettingsConfigHelper()
    {
        LoadConfig();
    }

    public static SettingsConfig CurrentConfig { get; private set; } = new();

    private static void LoadConfig()
    {
        try
        {
            var configDir = Path.GetDirectoryName(ConfigPath);

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir!);
                SaveConfig();
                return;
            }

            if (!File.Exists(ConfigPath) || new FileInfo(ConfigPath).Length == 0)
            {
                SaveConfig();
                return;
            }

            var json = File.ReadAllText(ConfigPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                SaveConfig();
                return;
            }

            var loadedConfig = JsonSerializer.Deserialize<SettingsConfig>(json, Options);
            if (loadedConfig != null) CurrentConfig = loadedConfig;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"配置加载异常：{ex.Message}\n已使用默认配置");
            SaveConfig();
        }
    }

    public static void SaveConfig()
    {
        var json = JsonSerializer.Serialize(CurrentConfig, Options);
        File.WriteAllText(ConfigPath, json);
    }

    public class SettingsConfig
    {
        public string AppTheme { get; set; } = string.Empty;
    }
}