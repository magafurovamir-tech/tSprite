using SkiaSharp;

namespace tSprite.Rendering;

public static class RenderPaints
{
    public static readonly SKPaint Background = new()
    {
        Color = new SKColor(30, 30, 30),
        Style = SKPaintStyle.Fill
    };

    public static readonly SKPaint GridColor1 = new()
    {
        Color = SKColor.Parse("#FFFFFF"),
        Style = SKPaintStyle.Fill
    };

    public static readonly SKPaint GridColor2 = new()
    {
        Color = SKColor.Parse("#E0E0E0"),
        Style = SKPaintStyle.Fill
    };

    public static readonly SKPaint Cell = new()
    {
        Style = SKPaintStyle.Fill
    };

    public static readonly SKPaint CanvasBorder = new()
    {
        Color = new SKColor(120, 120, 120),
        Style = SKPaintStyle.Stroke,
        IsAntialias = true
    };

    public static readonly SKPaint FrameHighlightFill = new()
    {
        Color = new SKColor(255, 215, 0, 16),
        Style = SKPaintStyle.Fill
    };

    public static readonly SKPaint FrameHighlightStroke = new()
    {
        Color = new SKColor(255, 215, 0, 190),
        Style = SKPaintStyle.Stroke
    };

    public static readonly SKPaint SelectionFill = new()
    {
        Color = new SKColor(0, 150, 255, 35),
        Style = SKPaintStyle.Fill
    };

    public static readonly SKPaint SelectionStroke = new()
    {
        Color = new SKColor(0, 200, 255, 230),
        Style = SKPaintStyle.Stroke
    };
}
