using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using iNKORE.UI.WPF.Modern.Controls;

namespace SimpleSSH.Helper;

public class ServerInfoConfigHelper
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "SimpleSSH", "ServerInfo.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    static ServerInfoConfigHelper()
    {
        LoadConfig();
    }

    // 初始化默认配置，确保非null
    public static ServerConfig CurrentConfig { get; private set; } = new();

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

            var loadedConfig = JsonSerializer.Deserialize<ServerConfig>(json, Options);
            if (loadedConfig != null) CurrentConfig = loadedConfig;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"服务器数据加载异常：{ex.Message}\n检查配置文件是否正确！");
            SaveConfig();
        }
    }

    public static void SaveConfig()
    {
        var json = JsonSerializer.Serialize(CurrentConfig, Options);
        File.WriteAllText(ConfigPath, json);
    }

    private static string GetMd5Hash(string input)
    {
        using (var md5Hash = MD5.Create())
        {
            var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();

            for (var i = 0; i < data.Length; i++) sBuilder.Append(data[i].ToString("x2"));

            return sBuilder.ToString();
        }
    }

    public class ServerConfig
    {
        public List<ServerInfo> Servers { get; set; } = new();
    }

    public class ServerInfo
    {
        public string ServerName { get; set; } = string.Empty;
        public string ServerUsername { get; set; } = string.Empty;
        public string ServerIp { get; set; } = string.Empty;
        public int ServerPort { get; set; } = 22; // 默认SSH端口
    }
}