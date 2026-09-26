using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Input;

namespace tSprite.Config;

public static class KeymapManager
{
    private static string _configPath = string.Empty;
    public static Dictionary<string, string> Bindings { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".config", "tSprite");
        _configPath = Path.Combine(configDir, "keyboard.hjson");

        SetDefaults();

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
                if (line.StartsWith("{") || line.StartsWith("}") || string.IsNullOrEmpty(line))
                {
                    continue;
                }

                var parts = line.Split(new[] { '=', ':' }, 2);
                if (parts.Length == 2)
                {
                    var action = parts[0].Trim();
                    var shortcut = parts[1].Trim().Trim('"', '\'');
                    Bindings[action] = shortcut;
                }
            }
        }
    }

    private static void SetDefaults()
    {
        Bindings["Brush"] = "B";
        Bindings["Eraser"] = "E";
        Bindings["Bucket"] = "G";
        Bindings["Selection"] = "M";
        Bindings["Delete"] = "Delete";
        Bindings["Escape"] = "Escape";
        Bindings["Undo"] = "Ctrl+Z";
        Bindings["Redo"] = "Ctrl+Y";
        Bindings["RedoAlt"] = "Ctrl+Shift+Z";
        Bindings["QuickSave"] = "Ctrl+S";
        Bindings["SaveAs"] = "Ctrl+Shift+S";
        Bindings["OpenFile"] = "Ctrl+O";
        Bindings["Copy"] = "Ctrl+C";
        Bindings["Paste"] = "Ctrl+V";
        Bindings["Duplicate"] = "Ctrl+D";
        Bindings["CopyClipboard"] = "Ctrl+Shift+C";
        Bindings["PasteClipboard"] = "Ctrl+Shift+V";
        Bindings["DuplicateClipboard"] = "Ctrl+Shift+D";
        Bindings["ClearSelection"] = "Escape";
        Bindings["ToggleTerminal"] = "OemTilde";
        Bindings["AssetBrowser"] = "Ctrl+Shift+A";
        Bindings["SwapColors"] = "X";
        Bindings["Recolor"] = "Ctrl+U";
        Bindings["RecolorAlt"] = "Shift+R";
    }

    public static void Save()
    {
        if (string.IsNullOrEmpty(_configPath))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var configDir = Path.Combine(userProfile, ".config", "tSprite");
            _configPath = Path.Combine(configDir, "keyboard.hjson");
        }

        var lines = new List<string> { "{" };
        foreach (var (k, v) in Bindings)
        {
            lines.Add($"  {k}: \"{v}\"");
        }
        lines.Add("}");

        File.WriteAllLines(_configPath, lines);
    }

    public static bool Matches(string actionName, KeyEventArgs e)
    {
        if (!Bindings.TryGetValue(actionName, out var shortcut) || string.IsNullOrWhiteSpace(shortcut))
        {
            return false;
        }

        var parts = shortcut.Split('+', StringSplitOptions.TrimEntries);
        bool reqCtrl = false;
        bool reqShift = false;
        bool reqAlt = false;
        string keyStr = string.Empty;

        foreach (var p in parts)
        {
            if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || p.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                reqCtrl = true;
            }
            else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                reqShift = true;
            }
            else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                reqAlt = true;
            }
            else
            {
                keyStr = p;
            }
        }

        bool hasCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool hasShift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool hasAlt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);

        if (reqCtrl != hasCtrl || reqShift != hasShift || reqAlt != hasAlt)
        {
            return false;
        }

        if (keyStr.Equals("OemTilde", StringComparison.OrdinalIgnoreCase) && (e.Key is Key.OemTilde or Key.Oem3))
        {
            return true;
        }

        if (Enum.TryParse<Key>(keyStr, true, out var key))
        {
            return e.Key == key;
        }

        return false;
    }
}
