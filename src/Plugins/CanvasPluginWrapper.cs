using System.Collections.Generic;
using SkiaSharp;
using tSprite.API;
using tSprite.Common;
using tSprite.Controls;

namespace tSprite.Plugins;

public class CanvasPluginWrapper : ICanvas
{
    private readonly CanvasControl _canvas;
    private readonly List<PixelDiff> _diffs = new();

    public CanvasPluginWrapper(CanvasControl canvas)
    {
        _canvas = canvas;
    }

    public int Width => _canvas.GridWidth;
    public int Height => _canvas.GridHeight;

    public ColorRGBA GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return ColorRGBA.Transparent;
        var col = _canvas.GetPixelColor(x, y);
        if (!col.HasValue || col.Value.Alpha == 0) return ColorRGBA.Transparent;
        return new ColorRGBA(col.Value.Red, col.Value.Green, col.Value.Blue, col.Value.Alpha);
    }

    public void SetPixel(int x, int y, ColorRGBA color)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        var oldCol = _canvas.GetPixelColor(x, y);
        SKColor? newCol = color.A == 0 ? null : new SKColor(color.R, color.G, color.B, color.A);

        if (oldCol != newCol)
        {
            _diffs.Add(new PixelDiff(x, y, oldCol, newCol));
            _canvas.SetPixelDirect(x, y, newCol);
        }
    }

    public void CommitChanges()
    {
        if (_diffs.Count > 0)
        {
            _canvas.ApplyPluginDiffs(_diffs);
            _diffs.Clear();
        }
    }
}
