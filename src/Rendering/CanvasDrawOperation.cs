using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using tSprite.Common;

namespace tSprite.Rendering;

public class CanvasDrawOperation : ICustomDrawOperation
{
    private readonly float _zoom;
    private readonly Point _panOffset;
    private readonly Size _canvasSize;
    private readonly SKColor?[,] _pixels;
    private readonly int _gridWidth;
    private readonly int _gridHeight;
    private readonly SKRectI? _selection;
    private readonly SKColor?[,]? _floatingPixels;
    private readonly SpritesheetConfig _spritesheet;
    private readonly int? _hoverX;
    private readonly int? _hoverY;

    public Rect Bounds { get; }

    public CanvasDrawOperation(
        Rect bounds,
        float zoom,
        Point panOffset,
        Size canvasSize,
        SKColor?[,] pixels,
        int gridWidth,
        int gridHeight,
        SKRectI? selection,
        SKColor?[,]? floatingPixels,
        SpritesheetConfig spritesheet,
        int? hoverX,
        int? hoverY)
    {
        Bounds = bounds;
        _zoom = zoom;
        _panOffset = panOffset;
        _canvasSize = canvasSize;
        _pixels = pixels;
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
        _selection = selection;
        _floatingPixels = floatingPixels;
        _spritesheet = spritesheet;
        _hoverX = hoverX;
        _hoverY = hoverY;
    }

    public void Dispose() { }

    public bool Equals(ICustomDrawOperation? other) => false;

    public bool HitTest(Point p) => Bounds.Contains(p);

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature == null) return;

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;

        canvas.Save();
        canvas.DrawRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height), RenderPaints.Background);

        canvas.Translate((float)_panOffset.X, (float)_panOffset.Y);
        canvas.Scale(_zoom, _zoom);

        float cellWidth = (float)_canvasSize.Width / _gridWidth;
        float cellHeight = (float)_canvasSize.Height / _gridHeight;

        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                var cellRect = new SKRect(
                    x * cellWidth,
                    y * cellHeight,
                    (x + 1) * cellWidth,
                    (y + 1) * cellHeight
                );

                var color = _pixels[x, y];
                if (color.HasValue)
                {
                    RenderPaints.Cell.Color = color.Value;
                    canvas.DrawRect(cellRect, RenderPaints.Cell);
                }
                else
                {
                    var paint = (x + y) % 2 == 0 ? RenderPaints.GridColor1 : RenderPaints.GridColor2;
                    canvas.DrawRect(cellRect, paint);
                }
            }
        }

        if (_floatingPixels != null && _selection.HasValue)
        {
            int selLeft = _selection.Value.Left;
            int selTop = _selection.Value.Top;
            int fh = _floatingPixels.GetLength(1);
            int fw = _floatingPixels.GetLength(0);

            for (int fy = 0; fy < fh; fy++)
            {
                for (int fx = 0; fx < fw; fx++)
                {
                    var col = _floatingPixels[fx, fy];
                    if (col.HasValue)
                    {
                        int gx = selLeft + fx;
                        int gy = selTop + fy;

                        var cellRect = new SKRect(
                            gx * cellWidth,
                            gy * cellHeight,
                            (gx + 1) * cellWidth,
                            (gy + 1) * cellHeight
                        );

                        RenderPaints.Cell.Color = col.Value;
                        canvas.DrawRect(cellRect, RenderPaints.Cell);
                    }
                }
            }
        }

        if (_spritesheet.Enabled && _hoverX.HasValue && _hoverY.HasValue)
        {
            if (_spritesheet.Mode == SpritesheetMode.Islands)
            {
                int hx = _hoverX.Value;
                int hy = _hoverY.Value;

                foreach (var isl in _spritesheet.DetectedIslands)
                {
                    if (isl.Contains(hx, hy))
                    {
                        var frameRect = new SKRect(
                            isl.Left * cellWidth,
                            isl.Top * cellHeight,
                            isl.Right * cellWidth,
                            isl.Bottom * cellHeight
                        );

                        RenderPaints.FrameHighlightStroke.StrokeWidth = 1.5f / _zoom;
                        using var effect = SKPathEffect.CreateDash(new[] { 4f / _zoom, 4f / _zoom }, 0);
                        RenderPaints.FrameHighlightStroke.PathEffect = effect;

                        canvas.DrawRect(frameRect, RenderPaints.FrameHighlightFill);
                        canvas.DrawRect(frameRect, RenderPaints.FrameHighlightStroke);
                        break;
                    }
                }
            }
            else if (_spritesheet.Mode == SpritesheetMode.Grid && _spritesheet.FrameWidth > 0 && _spritesheet.FrameHeight > 0)
            {
                int col = _hoverX.Value / _spritesheet.FrameWidth;
                int row = _hoverY.Value / _spritesheet.FrameHeight;
                int maxCols = _gridWidth / _spritesheet.FrameWidth;
                int maxRows = _gridHeight / _spritesheet.FrameHeight;

                if (col >= 0 && col < maxCols && row >= 0 && row < maxRows)
                {
                    if (SpritesheetConfig.CellHasPixels(_pixels, col, row, _spritesheet.FrameWidth, _spritesheet.FrameHeight, _gridWidth, _gridHeight))
                    {
                        float fx = col * _spritesheet.FrameWidth * cellWidth;
                        float fy = row * _spritesheet.FrameHeight * cellHeight;
                        float fw = _spritesheet.FrameWidth * cellWidth;
                        float fh = _spritesheet.FrameHeight * cellHeight;

                        var frameRect = new SKRect(fx, fy, fx + fw, fy + fh);

                        RenderPaints.FrameHighlightStroke.StrokeWidth = 1.5f / _zoom;
                        using var effect = SKPathEffect.CreateDash(new[] { 4f / _zoom, 4f / _zoom }, 0);
                        RenderPaints.FrameHighlightStroke.PathEffect = effect;

                        canvas.DrawRect(frameRect, RenderPaints.FrameHighlightFill);
                        canvas.DrawRect(frameRect, RenderPaints.FrameHighlightStroke);
                    }
                }
            }
        }

        if (_selection.HasValue)
        {
            var sel = _selection.Value;
            var selRect = new SKRect(
                sel.Left * cellWidth,
                sel.Top * cellHeight,
                sel.Right * cellWidth,
                sel.Bottom * cellHeight
            );

            RenderPaints.SelectionStroke.StrokeWidth = 1.5f / _zoom;
            using var effect = SKPathEffect.CreateDash(new[] { 4f / _zoom, 4f / _zoom }, 0);
            RenderPaints.SelectionStroke.PathEffect = effect;

            canvas.DrawRect(selRect, RenderPaints.SelectionFill);
            canvas.DrawRect(selRect, RenderPaints.SelectionStroke);
        }

        RenderPaints.CanvasBorder.StrokeWidth = 1f / _zoom;
        var rect = new SKRect(0, 0, (float)_canvasSize.Width, (float)_canvasSize.Height);
        canvas.DrawRect(rect, RenderPaints.CanvasBorder);

        canvas.Restore();
    }
}
