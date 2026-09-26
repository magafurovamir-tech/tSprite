using Avalonia.Media.Imaging;

namespace tSprite.Assets;

public enum AssetCategory
{
    Items,
    Projectiles,
    Tiles
}

public class ItemEntry
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string InternalName { get; init; } = string.Empty;
    public AssetCategory Category { get; init; } = AssetCategory.Items;
    public string? FilePath { get; set; }
    public bool IsFavorite { get; set; }
    public Bitmap? Thumbnail { get; set; }
    public string Key => $"{Category}:{Id}";
    public string DisplaySubtitle => $"{Category} • #{Id} • {InternalName}";
}
