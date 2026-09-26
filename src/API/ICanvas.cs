namespace tSprite.API;

public interface ICanvas
{
    int Width { get; }
    int Height { get; }
    ColorRGBA GetPixel(int x, int y);
    void SetPixel(int x, int y, ColorRGBA color);
    void CommitChanges();
}
