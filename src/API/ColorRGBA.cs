namespace tSprite.API;

public readonly record struct ColorRGBA(byte R, byte G, byte B, byte A)
{
    public static readonly ColorRGBA Transparent = new(0, 0, 0, 0);
}
