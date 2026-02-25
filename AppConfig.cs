using System;
using System.IO;
using System.Text.Json;

namespace FUXADesktop
{
    public class AppConfig
    {
        public string AppName { get; set; } = "FUXA";
        public string WindowTitle { get; set; } = "FUXA - Process Visualization";
        public string LogoPath { get; set; } = "fuxa-logo.ico";
        public string VersionText { get; set; } = "v1.3.0";
        public LoadingMessages LoadingMessages { get; set; } = new LoadingMessages();
        public WindowSettings WindowSettings { get; set; } = new WindowSettings();
        public Colors Colors { get; set; } = new Colors();
        public ServerSettings ServerSettings { get; set; } = new ServerSettings();

        // 获取格式化后的版本文本
        public string GetFormattedVersionText() => $"{AppName} {VersionText}".Trim();

        public static AppConfig Load(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return config ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
            }
            return new AppConfig();
        }

        public void Save(string configPath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }
    }

    public class LoadingMessages
    {
        public string StartingServer { get; set; } = "正在启动 {AppName} 服务器...";
        public string InitializingBrowser { get; set; } = "正在初始化浏览器...";
        public string LoadingApp { get; set; } = "正在加载 {AppName}...";
        public string ServerRestarting { get; set; } = "服务器连接丢失，正在重新启动...";
        public string ReloadingApp { get; set; } = "正在重新加载 {AppName}...";

        // 根据 AppName 格式化消息
        public string GetStartingServer(string appName) => StartingServer.Replace("{AppName}", appName);
        public string GetLoadingApp(string appName) => LoadingApp.Replace("{AppName}", appName);
        public string GetReloadingApp(string appName) => ReloadingApp.Replace("{AppName}", appName);
    }

    public class WindowSettings
    {
        public int Width { get; set; } = 1400;
        public int Height { get; set; } = 900;
        public int MinWidth { get; set; } = 800;
        public int MinHeight { get; set; } = 600;
    }

    public class Colors
    {
        public string BackgroundColor { get; set; } = "#F5F7FA";
        public string BackgroundColor2 { get; set; } = "#EBF0F5";
        public string TextColor { get; set; } = "#3C3C3C";
        public string VersionColor { get; set; } = "#969696";
        public string SpinnerColor { get; set; } = "#007BFF";
        public string SpinnerBackgroundColor { get; set; } = "#DCDCDC";
    }

    public class ServerSettings
    {
        public string NodePath { get; set; } = "nodejs/node.exe";
        public string ServerScript { get; set; } = "server/main.js";
        public int Port { get; set; } = 1881;
        public string Host { get; set; } = "localhost";
        public bool StopServerOnExit { get; set; } = true;
        public bool AskBeforeStopServer { get; set; } = false;
    }
}
