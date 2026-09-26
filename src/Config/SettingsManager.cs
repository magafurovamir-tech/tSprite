using System;
using System.IO;
using SkiaSharp;

namespace tSprite.Config;

public static class SettingsManager
{
    private static string _configPath = string.Empty;
    public static string LastSelectedTool { get; set; } = "Brush";
    public static string PrimaryColorHex { get; set; } = "#000000";
    public static string SecondaryColorHex { get; set; } = "#FFFFFF";

    public static SKColor PrimaryColor => SKColor.TryParse(PrimaryColorHex, out var c) ? c : SKColors.Black;
    public static SKColor SecondaryColor => SKColor.TryParse(SecondaryColorHex, out var c) ? c : SKColors.White;

    public static void Initialize()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".config", "tSprite");
        _configPath = Path.Combine(configDir, "settings.hjson");

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
                if (line.StartsWith("LastSelectedTool"))
                {
                    var parts = line.Split(new[] { '=', ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var tool = parts[1].Trim().Trim('"', '\'');
                        if (tool is "Brush" or "Eraser" or "Bucket" or "Selection")
                        {
                            LastSelectedTool = tool;
                        }
                    }
                }
                else if (line.StartsWith("Primary"))
                {
                    var parts = line.Split(new[] { '=', ':' }, 2);
                    if (parts.Length == 2)
                    {
                        PrimaryColorHex = parts[1].Trim().Trim('"', '\'');
                    }
                }
                else if (line.StartsWith("Secondary"))
                {
                    var parts = line.Split(new[] { '=', ':' }, 2);
                    if (parts.Length == 2)
                    {
                        SecondaryColorHex = parts[1].Trim().Trim('"', '\'');
                    }
                }
            }
        }
    }

    public static void SwapColors()
    {
        (PrimaryColorHex, SecondaryColorHex) = (SecondaryColorHex, PrimaryColorHex);
        Save();
    }

    public static void SetTool(string tool)
    {
        if (tool is "Brush" or "Eraser" or "Bucket" or "Selection")
        {
            LastSelectedTool = tool;
            Save();
        }
    }

    public static void Save()
    {
        if (string.IsNullOrEmpty(_configPath))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var configDir = Path.Combine(userProfile, ".config", "tSprite");
            _configPath = Path.Combine(configDir, "settings.hjson");
        }

        var content = $$"""
        Settings: {
          LastSelectedTool: {{LastSelectedTool}}

          Colors: {
            Primary: "{{PrimaryColorHex}}"
            Secondary: "{{SecondaryColorHex}}"
          }
        }
        """;
        File.WriteAllText(_configPath, content);
    }
}
