using System;
using System.IO;

namespace tSprite.Config;

public static class ConfigManager
{
    private static string _configPath = string.Empty;
    public static int WindowWidth { get; set; } = 800;
    public static int WindowHeight { get; set; } = 600;
    public static string RecentFile { get; set; } = string.Empty;
    public static string LastSavedFileName { get; set; } = "sprite.png";

    public static void Initialize()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".config", "tSprite");
        _configPath = Path.Combine(configDir, "config.hjson");

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        if (!File.Exists(_configPath))
        {
            Save();
        }
        else
        {
            foreach (var rawLine in File.ReadAllLines(_configPath))
            {
                var line = rawLine.Trim();
                if (line.StartsWith("WindowWidth"))
                {
                    var parts = line.Split(new[] { '=', ':' }, 2);
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var w))
                    {
                        WindowWidth = w;
                    }
                }
                else if (line.StartsWith("WindowHeight"))
                {
                    var parts = line.Split(new[] { '=', ':' }, 2);
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var h))
                    {
                        WindowHeight = h;
                    }
                }
                else if (line.StartsWith("RecentFile"))
                {
                    var parts = line.Split(new[] { '=', ':' }, 2);
                    if (parts.Length == 2)
                    {
                        RecentFile = parts[1].Trim().Trim('"', '\'');
                    }
                }
                else if (line.StartsWith("LastSavedFileName"))
                {
                    var parts = line.Split(new[] { '=', ':' }, 2);
                    if (parts.Length == 2)
                    {
                        LastSavedFileName = parts[1].Trim().Trim('"', '\'');
                    }
                }
            }
        }
    }

    public static void Save()
    {
        if (string.IsNullOrEmpty(_configPath))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var configDir = Path.Combine(userProfile, ".config", "tSprite");
            _configPath = Path.Combine(configDir, "config.hjson");
        }

        var lines = new[]
        {
            "{",
            $"  WindowWidth: {WindowWidth}",
            $"  WindowHeight: {WindowHeight}",
            $"  RecentFile: \"{RecentFile}\"",
            $"  LastSavedFileName: \"{LastSavedFileName}\"",
            "}"
        };
        File.WriteAllLines(_configPath, lines);
    }
}
