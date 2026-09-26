using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia.Media.Imaging;

namespace tSprite.Assets;

public class ItemMetadataRaw
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public string internal_name { get; set; } = string.Empty;
}

public static class AssetManager
{
    private static readonly Dictionary<int, string> _itemFileMap = new();
    private static readonly Dictionary<int, string> _projectileFileMap = new();
    private static readonly Dictionary<int, string> _tileFileMap = new();
    private static readonly HashSet<string> _favorites = new();
    private static readonly Dictionary<string, Bitmap> _thumbnailCache = new();
    private static string _favoritesPath = string.Empty;

    public static List<ItemEntry> Items { get; } = new();
    public static bool IsLoaded { get; private set; }

    public static void Initialize()
    {
        if (IsLoaded) return;

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".config", "tSprite");
        Directory.CreateDirectory(configDir);
        _favoritesPath = Path.Combine(configDir, "favorites.json");

        LoadFavorites();
        IndexVanillaDump();
        LoadMetadata("items.json", AssetCategory.Items, _itemFileMap);
        LoadMetadata("projectiles.json", AssetCategory.Projectiles, _projectileFileMap);
        LoadMetadata("tiles.json", AssetCategory.Tiles, _tileFileMap);

        IsLoaded = true;
    }

    private static void LoadFavorites()
    {
        if (File.Exists(_favoritesPath))
        {
            try
            {
                var json = File.ReadAllText(_favoritesPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.String)
                        {
                            var s = el.GetString();
                            if (!string.IsNullOrEmpty(s)) _favorites.Add(s);
                        }
                        else if (el.ValueKind == JsonValueKind.Number)
                        {
                            _favorites.Add($"Items:{el.GetInt32()}");
                        }
                    }
                }
            }
            catch { }
        }
    }

    public static void ToggleFavorite(ItemEntry item)
    {
        if (_favorites.Contains(item.Key))
        {
            _favorites.Remove(item.Key);
            item.IsFavorite = false;
        }
        else
        {
            _favorites.Add(item.Key);
            item.IsFavorite = true;
        }
        SaveFavorites();
    }

    private static void SaveFavorites()
    {
        try
        {
            var json = JsonSerializer.Serialize(_favorites);
            File.WriteAllText(_favoritesPath, json);
        }
        catch { }
    }

    private static void IndexVanillaDump()
    {
        string[] searchPaths = {
            Path.Combine(Directory.GetCurrentDirectory(), "VanillaDump"),
            Path.Combine(AppContext.BaseDirectory, "VanillaDump"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "VanillaDump")
        };

        string? dumpDir = null;
        foreach (var path in searchPaths)
        {
            if (Directory.Exists(path))
            {
                dumpDir = path;
                break;
            }
        }

        if (dumpDir == null) return;

        foreach (var file in Directory.EnumerateFiles(dumpDir, "*.*", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            if (fileName.StartsWith("Items_", StringComparison.OrdinalIgnoreCase) && int.TryParse(fileName["Items_".Length..], out var id1))
            {
                _itemFileMap[id1] = file;
            }
            else if (fileName.StartsWith("Item_", StringComparison.OrdinalIgnoreCase) && int.TryParse(fileName["Item_".Length..], out var id2))
            {
                _itemFileMap[id2] = file;
            }
            else if (fileName.StartsWith("Projectiles_", StringComparison.OrdinalIgnoreCase) && int.TryParse(fileName["Projectiles_".Length..], out var pid1))
            {
                _projectileFileMap[pid1] = file;
            }
            else if (fileName.StartsWith("Projectile_", StringComparison.OrdinalIgnoreCase) && int.TryParse(fileName["Projectile_".Length..], out var pid2))
            {
                _projectileFileMap[pid2] = file;
            }
            else if (fileName.StartsWith("Tiles_", StringComparison.OrdinalIgnoreCase) && int.TryParse(fileName["Tiles_".Length..], out var tid1))
            {
                _tileFileMap[tid1] = file;
            }
            else if (fileName.StartsWith("Tile_", StringComparison.OrdinalIgnoreCase) && int.TryParse(fileName["Tile_".Length..], out var tid2))
            {
                _tileFileMap[tid2] = file;
            }
        }
    }

    private static void LoadMetadata(string fileName, AssetCategory category, Dictionary<int, string> fileMap)
    {
        string[] searchPaths = {
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName),
            Path.Combine(AppContext.BaseDirectory, "Assets", "Data", fileName),
            Path.Combine(AppContext.BaseDirectory, "Data", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), fileName)
        };

        string? jsonPath = null;
        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                jsonPath = path;
                break;
            }
        }

        if (jsonPath == null) return;

        try
        {
            var json = File.ReadAllText(jsonPath);
            var list = JsonSerializer.Deserialize<List<ItemMetadataRaw>>(json);
            if (list == null) return;

            foreach (var raw in list)
            {
                fileMap.TryGetValue(raw.id, out var filePath);
                var entry = new ItemEntry
                {
                    Id = raw.id,
                    Name = raw.name,
                    InternalName = raw.internal_name,
                    Category = category,
                    FilePath = filePath
                };
                entry.IsFavorite = _favorites.Contains(entry.Key);
                Items.Add(entry);
            }
        }
        catch { }
    }

    public static Bitmap? GetThumbnail(ItemEntry? item)
    {
        if (item == null) return null;
        if (item.Thumbnail != null) return item.Thumbnail;
        if (_thumbnailCache.TryGetValue(item.Key, out var cached))
        {
            item.Thumbnail = cached;
            return cached;
        }

        if (item.FilePath != null && File.Exists(item.FilePath))
        {
            try
            {
                if (_thumbnailCache.Count > 300)
                {
                    _thumbnailCache.Clear();
                }
                var bmp = new Bitmap(item.FilePath);
                _thumbnailCache[item.Key] = bmp;
                item.Thumbnail = bmp;
                return bmp;
            }
            catch { }
        }
        return null;
    }
}
