using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using tSprite.Common;
using tSprite.Config;
using tSprite.Rendering;
using tSprite.Session;

namespace tSprite.Controls;

public class CanvasControl : Control
{
    private static readonly Cursor _crosshairCursor = CreateCrosshairCursor();
    private static readonly Cursor _move4WayCursor = CreateMove4WayCursor();

    private float _zoom = 1.0f;
    private Point _panOffset;
    private Point _lastMousePos;
    private Point _lastRawMousePos;
    private bool _isPanning;
    private bool _isDrawing;
    private bool _isSelecting;
    private bool _isMovingSelection;
    private Point _selectionStart;
    private Point _moveStartPixel;
    private SKRectI _initialSelection;
    private SKColor?[,]? _floatingPixels;
    private Dictionary<(int, int), SKColor?>? _cutOriginals;
    private SKColor? _currentDrawingColor;
    private bool _isInitialized;
    private int _gridWidth = 16;
    private int _gridHeight = 16;
    private Size _canvasSize = new(320, 320);
    private SKColor?[,] _pixels = new SKColor?[16, 16];
    private readonly SessionManager _sessionManager = new();
    private readonly SpritesheetConfig _spritesheet = new();
    private readonly Dictionary<(int X, int Y), (SKColor? OldColor, SKColor? NewColor)> _strokeChanges = new();
    private static SKColor?[,]? _internalClipboard;

    public event Action? StatusChanged;

    public int? HoveredX { get; private set; }
    public int? HoveredY { get; private set; }
    public float Zoom => _zoom;
    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;
    public SKRectI? Selection { get; private set; }
    public SpritesheetConfig Spritesheet => _spritesheet;

    public int ActiveGridFramesCount
    {
        get
        {
            if (_spritesheet.Mode != SpritesheetMode.Grid || _spritesheet.FrameWidth <= 0 || _spritesheet.FrameHeight <= 0)
            {
                return 0;
            }

            int cols = _gridWidth / _spritesheet.FrameWidth;
            int rows = _gridHeight / _spritesheet.FrameHeight;
            int count = 0;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (SpritesheetConfig.CellHasPixels(_pixels, c, r, _spritesheet.FrameWidth, _spritesheet.FrameHeight, _gridWidth, _gridHeight))
                    {
                        count++;
                    }
                }
            }
            return count;
        }
    }

    public int? GetActiveFrameIndex(int col, int row)
    {
        if (_spritesheet.Mode != SpritesheetMode.Grid || _spritesheet.FrameWidth <= 0 || _spritesheet.FrameHeight <= 0)
        {
            return null;
        }

        int cols = _gridWidth / _spritesheet.FrameWidth;
        int rows = _gridHeight / _spritesheet.FrameHeight;
        int index = 0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (SpritesheetConfig.CellHasPixels(_pixels, c, r, _spritesheet.FrameWidth, _spritesheet.FrameHeight, _gridWidth, _gridHeight))
                {
                    index++;
                    if (c == col && r == row)
                    {
                        return index;
                    }
                }
            }
        }
        return null;
    }

    public CanvasControl()
    {
        ClipToBounds = true;
        Focusable = true;

        var (w, h, pixels) = _sessionManager.LoadSession();
        _gridWidth = w;
        _gridHeight = h;
        _pixels = pixels;
        _canvasSize = new Size(_gridWidth * 20, _gridHeight * 20);

        AutoDetectSpritesheet();
    }

    public void RefreshIslands()
    {
        _spritesheet.DetectedIslands = SpritesheetConfig.FindIslands(_pixels, _gridWidth, _gridHeight);
    }

    public void AutoDetectSpritesheet()
    {
        RefreshIslands();
        var islands = _spritesheet.DetectedIslands;

        if (_gridWidth <= 80 && _gridHeight <= 80 && (_gridHeight < _gridWidth * 3 && _gridWidth < _gridHeight * 3))
        {
            _spritesheet.Mode = SpritesheetMode.Off;
            _spritesheet.IsAuto = true;
            InvalidateVisual();
            StatusChanged?.Invoke();
            return;
        }

        if (_gridHeight >= _gridWidth * 3 && _gridHeight % _gridWidth == 0 && _gridWidth >= 8)
        {
            _spritesheet.Mode = SpritesheetMode.Grid;
            _spritesheet.FrameWidth = _gridWidth;
            _spritesheet.FrameHeight = _gridWidth;
            _spritesheet.IsAuto = true;
            InvalidateVisual();
            StatusChanged?.Invoke();
            return;
        }

        if (_gridWidth >= _gridHeight * 3 && _gridWidth % _gridHeight == 0 && _gridHeight >= 8)
        {
            _spritesheet.Mode = SpritesheetMode.Grid;
            _spritesheet.FrameWidth = _gridHeight;
            _spritesheet.FrameHeight = _gridHeight;
            _spritesheet.IsAuto = true;
            InvalidateVisual();
            StatusChanged?.Invoke();
            return;
        }

        if (_gridWidth > 80 || _gridHeight > 80)
        {
            int[] standardSizes = { 32, 18, 16, 20, 24, 48, 64 };
            foreach (var sz in standardSizes)
            {
                if (_gridWidth >= sz * 2 && _gridHeight >= sz * 2 && _gridWidth % sz == 0 && _gridHeight % sz == 0)
                {
                    _spritesheet.Mode = SpritesheetMode.Grid;
                    _spritesheet.FrameWidth = sz;
                    _spritesheet.FrameHeight = sz;
                    _spritesheet.IsAuto = true;
                    InvalidateVisual();
                    StatusChanged?.Invoke();
                    return;
                }
            }

            if (islands.Count > 1)
            {
                _spritesheet.Mode = SpritesheetMode.Islands;
                _spritesheet.IsAuto = true;
                InvalidateVisual();
                StatusChanged?.Invoke();
                return;
            }
        }

        _spritesheet.Mode = SpritesheetMode.Off;
        _spritesheet.IsAuto = true;
        InvalidateVisual();
        StatusChanged?.Invoke();
    }

    public void SetGridMode(int fw, int fh)
    {
        _spritesheet.Mode = SpritesheetMode.Grid;
        _spritesheet.FrameWidth = Math.Clamp(fw, 1, _gridWidth);
        _spritesheet.FrameHeight = Math.Clamp(fh, 1, _gridHeight);
        _spritesheet.IsAuto = false;
        InvalidateVisual();
        StatusChanged?.Invoke();
    }

    public void SetIslandMode()
    {
        RefreshIslands();
        _spritesheet.Mode = SpritesheetMode.Islands;
        _spritesheet.IsAuto = false;
        InvalidateVisual();
        StatusChanged?.Invoke();
    }

    public void DisableSpritesheet()
    {
        _spritesheet.Mode = SpritesheetMode.Off;
        _spritesheet.IsAuto = false;
        InvalidateVisual();
        StatusChanged?.Invoke();
    }

    public SKRectI? GetHoveredFrameRect()
    {
        if (!_spritesheet.Enabled || !HoveredX.HasValue || !HoveredY.HasValue)
        {
            return null;
        }

        int hx = HoveredX.Value;
        int hy = HoveredY.Value;

        if (_spritesheet.Mode == SpritesheetMode.Islands)
        {
            foreach (var isl in _spritesheet.DetectedIslands)
            {
                if (isl.Contains(hx, hy))
                {
                    return isl;
                }
            }
        }
        else if (_spritesheet.Mode == SpritesheetMode.Grid && _spritesheet.FrameWidth > 0 && _spritesheet.FrameHeight > 0)
        {
            int col = hx / _spritesheet.FrameWidth;
            int row = hy / _spritesheet.FrameHeight;

            if (SpritesheetConfig.CellHasPixels(_pixels, col, row, _spritesheet.FrameWidth, _spritesheet.FrameHeight, _gridWidth, _gridHeight))
            {
                int fx = col * _spritesheet.FrameWidth;
                int fy = row * _spritesheet.FrameHeight;
                return new SKRectI(fx, fy, fx + _spritesheet.FrameWidth, fy + _spritesheet.FrameHeight);
            }
        }

        return null;
    }

    public SKRectI GetEffectiveExportRect()
    {
        if (Selection.HasValue)
        {
            return Selection.Value;
        }

        var hoveredFrame = GetHoveredFrameRect();
        if (hoveredFrame.HasValue)
        {
            return hoveredFrame.Value;
        }

        return new SKRectI(0, 0, _gridWidth, _gridHeight);
    }

    public void CopyInternal()
    {
        var targetRect = GetEffectiveExportRect();
        int w = Math.Max(1, targetRect.Width);
        int h = Math.Max(1, targetRect.Height);
        _internalClipboard = new SKColor?[w, h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int gx = targetRect.Left + x;
                int gy = targetRect.Top + y;

                if (gx >= 0 && gx < _gridWidth && gy >= 0 && gy < _gridHeight)
                {
                    _internalClipboard[x, y] = _pixels[gx, gy];
                }
            }
        }
    }

    public void PasteInternal()
    {
        if (_internalClipboard == null)
        {
            return;
        }

        int w = _internalClipboard.GetLength(0);
        int h = _internalClipboard.GetLength(1);

        int startX = HoveredX.HasValue ? HoveredX.Value : Math.Max(0, (_gridWidth - w) / 2);
        int startY = HoveredY.HasValue ? HoveredY.Value : Math.Max(0, (_gridHeight - h) / 2);

        _floatingPixels = new SKColor?[w, h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                _floatingPixels[x, y] = _internalClipboard[x, y];
            }
        }

        _initialSelection = new SKRectI(startX, startY, startX + w, startY + h);
        Selection = _initialSelection;
        _isMovingSelection = false;

        InvalidateVisual();
        StatusChanged?.Invoke();
    }

    public void DuplicateInternal()
    {
        CopyInternal();
        PasteInternal();
    }

    public SKBitmap GetEffectiveBitmap()
    {
        var rect = GetEffectiveExportRect();
        int w = Math.Max(1, rect.Width);
        int h = Math.Max(1, rect.Height);

        var bitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int gx = rect.Left + x;
                int gy = rect.Top + y;
                SKColor col;

                if (_floatingPixels != null && x < _floatingPixels.GetLength(0) && y < _floatingPixels.GetLength(1) && _floatingPixels[x, y].HasValue)
                {
                    col = _floatingPixels[x, y]!.Value;
                }
                else if (gx >= 0 && gx < _gridWidth && gy >= 0 && gy < _gridHeight)
                {
                    col = _pixels[gx, gy] ?? new SKColor(0, 0, 0, 0);
                }
                else
                {
                    col = new SKColor(0, 0, 0, 0);
                }

                bitmap.SetPixel(x, y, col);
            }
        }

        return bitmap;
    }

    public void PasteBitmap(SKBitmap bitmap)
    {
        int w = bitmap.Width;
        int h = bitmap.Height;

        int startX = HoveredX.HasValue ? HoveredX.Value : Math.Max(0, (_gridWidth - w) / 2);
        int startY = HoveredY.HasValue ? HoveredY.Value : Math.Max(0, (_gridHeight - h) / 2);

        _floatingPixels = new SKColor?[w, h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var c = bitmap.GetPixel(x, y);
                _floatingPixels[x, y] = c.Alpha == 0 ? null : c;
            }
        }

        _initialSelection = new SKRectI(startX, startY, startX + w, startY + h);
        Selection = _initialSelection;
        _isMovingSelection = false;

        InvalidateVisual();
        StatusChanged?.Invoke();
    }

    private static Cursor CreateCrosshairCursor()
    {
        try
        {
            const int size = 17;
            const int center = 8;
            var wb = new WriteableBitmap(
                new PixelSize(size, size),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            var buffer = new int[size * size];

            void Set(int x, int y, uint color)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    buffer[y * size + x] = unchecked((int)color);
                }
            }

            const uint white = 0xFFFFFFFF;
            const uint black = 0xFF000000;

            Set(center, center, white);
            Set(center - 1, center, black);
            Set(center + 1, center, black);
            Set(center, center - 1, black);
            Set(center, center + 1, black);

            for (int x = 2; x <= 5; x++)
            {
                Set(x, center, white);
                Set(x, center - 1, black);
                Set(x, center + 1, black);
            }
            Set(1, center, black);
            Set(6, center, black);

            for (int x = 11; x <= 14; x++)
            {
                Set(x, center, white);
                Set(x, center - 1, black);
                Set(x, center + 1, black);
            }
            Set(10, center, black);
            Set(15, center, black);

            for (int y = 2; y <= 5; y++)
            {
                Set(center, y, white);
                Set(center - 1, y, black);
                Set(center + 1, y, black);
            }
            Set(center, 1, black);
            Set(center, 6, black);

            for (int y = 11; y <= 14; y++)
            {
                Set(center, y, white);
                Set(center - 1, y, black);
                Set(center + 1, y, black);
            }
            Set(center, 10, black);
            Set(center, 15, black);

            using (var fb = wb.Lock())
            {
                Marshal.Copy(buffer, 0, fb.Address, buffer.Length);
            }

            return new Cursor(wb, new PixelPoint(center, center));
        }
        catch
        {
            return new Cursor(StandardCursorType.Cross);
        }
    }

    private static Cursor CreateMove4WayCursor()
    {
        try
        {
            const int size = 17;
            const int center = 8;
            var wb = new WriteableBitmap(
                new PixelSize(size, size),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            var isWhite = new bool[size, size];

            void Mark(int x, int y)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    isWhite[x, y] = true;
                }
            }

            for (int x = 3; x <= 13; x++) Mark(x, center);
            for (int y = 3; y <= 13; y++) Mark(center, y);

            Mark(2, center);
            Mark(3, center - 1);
            Mark(3, center + 1);

            Mark(14, center);
            Mark(13, center - 1);
            Mark(13, center + 1);

            Mark(center, 2);
            Mark(center - 1, 3);
            Mark(center + 1, 3);

            Mark(center, 14);
            Mark(center - 1, 13);
            Mark(center + 1, 13);

            var buffer = new int[size * size];
            const int white = unchecked((int)0xFFFFFFFF);
            const int black = unchecked((int)0xFF000000);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (isWhite[x, y])
                    {
                        buffer[y * size + x] = white;
                    }
                    else
                    {
                        bool hasWhiteNeighbor = false;
                        for (int dy = -1; dy <= 1 && !hasWhiteNeighbor; dy++)
                        {
                            for (int dx = -1; dx <= 1 && !hasWhiteNeighbor; dx++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx >= 0 && nx < size && ny >= 0 && ny < size && isWhite[nx, ny])
                                {
                                    hasWhiteNeighbor = true;
                                }
                            }
                        }

                        if (hasWhiteNeighbor)
                        {
                            buffer[y * size + x] = black;
                        }
                    }
                }
            }

            using (var fb = wb.Lock())
            {
                Marshal.Copy(buffer, 0, fb.Address, buffer.Length);
            }

            return new Cursor(wb, new PixelPoint(center, center));
        }
        catch
        {
            return new Cursor(StandardCursorType.SizeAll);
        }
    }

    public void UpdateKeyModifiers(KeyModifiers modifiers)
    {
        UpdateCursor(_lastRawMousePos, modifiers);
    }

    private void UpdateCursor(Point pos, KeyModifiers modifiers)
    {
        if (_isMovingSelection)
        {
            Cursor = _move4WayCursor;
            return;
        }

        if (_isSelecting)
        {
            Cursor = _crosshairCursor;
            return;
        }

        double localX = (pos.X - _panOffset.X) / _zoom;
        double localY = (pos.Y - _panOffset.Y) / _zoom;

        double cellWidth = _canvasSize.Width / _gridWidth;
        double cellHeight = _canvasSize.Height / _gridHeight;

        int px = (int)Math.Floor(localX / cellWidth);
        int py = (int)Math.Floor(localY / cellHeight);

        bool isInsideSelection = Selection.HasValue && Selection.Value.Contains(px, py);
        bool isMoveMode = SettingsManager.LastSelectedTool == "Selection" || modifiers.HasFlag(KeyModifiers.Control);

        if (isInsideSelection && isMoveMode)
        {
            Cursor = _move4WayCursor;
        }
        else
        {
            Cursor = Cursor.Default;
        }
    }

    public SKColor? GetPixelColor(int x, int y)
    {
        if (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight)
            return _pixels[x, y];
        return null;
    }

    public void SetPixelDirect(int x, int y, SKColor? color)
    {
        if (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight)
            _pixels[x, y] = color;
    }

    public void ApplyPluginDiffs(IReadOnlyList<PixelDiff> diffs)
    {
        if (diffs.Count > 0)
        {
            _sessionManager.RecordStep(diffs);
            RefreshIslands();
            InvalidateVisual();
            StatusChanged?.Invoke();
        }
    }

    public void ClearSelection()
    {
        if (Selection.HasValue)
        {
            Selection = null;
            InvalidateVisual();
            StatusChanged?.Invoke();
            UpdateCursor(_lastRawMousePos, KeyModifiers.None);
        }
    }

    public void DeleteSelection()
    {
        if (!Selection.HasValue) return;

        var sel = Selection.Value;
        var diffs = new List<PixelDiff>();

        for (int y = 0; y < sel.Height; y++)
        {
            for (int x = 0; x < sel.Width; x++)
            {
                int gx = sel.Left + x;
                int gy = sel.Top + y;

                if (gx >= 0 && gx < _gridWidth && gy >= 0 && gy < _gridHeight)
                {
                    var oldCol = _pixels[gx, gy];
                    if (oldCol.HasValue)
                    {
                        _pixels[gx, gy] = null;
                        diffs.Add(new PixelDiff(gx, gy, oldCol, null));
                    }
                }
            }
        }

        if (_floatingPixels != null)
        {
            _floatingPixels = null;
            _cutOriginals = null;
            _isMovingSelection = false;
        }

        if (diffs.Count > 0)
        {
            _sessionManager.RecordStep(diffs);
            RefreshIslands();
        }

        InvalidateVisual();
        StatusChanged?.Invoke();
    }

    public void LoadNewImage(SKBitmap bitmap)
    {
        _gridWidth = bitmap.Width;
        _gridHeight = bitmap.Height;
        _canvasSize = new Size(_gridWidth * 20, _gridHeight * 20);
        _pixels = new SKColor?[_gridWidth, _gridHeight];

        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                var c = bitmap.GetPixel(x, y);
                _pixels[x, y] = c.Alpha == 0 ? null : c;
            }
        }

        _panOffset = new Point(
            (Bounds.Width - _canvasSize.Width) / 2.0,
            (Bounds.Height - _canvasSize.Height) / 2.0
        );
        _zoom = 1.0f;

        HoveredX = null;
        HoveredY = null;
        Selection = null;
        _floatingPixels = null;
        _cutOriginals = null;

        AutoDetectSpritesheet();

        _sessionManager.ResetWithNewBase(_pixels, _gridWidth, _gridHeight);
        InvalidateVisual();
        StatusChanged?.Invoke();
    }

    public void ExportToPng(Stream stream, SKRectI? specificRect = null)
    {
        var targetRect = specificRect ?? GetEffectiveExportRect();

        int exportWidth = Math.Max(1, targetRect.Width);
        int exportHeight = Math.Max(1, targetRect.Height);

        using var bitmap = new SKBitmap(exportWidth, exportHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);

        for (int y = 0; y < exportHeight; y++)
        {
            for (int x = 0; x < exportWidth; x++)
            {
                int gx = targetRect.Left + x;
                int gy = targetRect.Top + y;
                SKColor color;

                if (_floatingPixels != null && x < _floatingPixels.GetLength(0) && y < _floatingPixels.GetLength(1) && _floatingPixels[x, y].HasValue)
                {
                    color = _floatingPixels[x, y]!.Value;
                }
                else if (gx >= 0 && gx < _gridWidth && gy >= 0 && gy < _gridHeight)
                {
                    color = _pixels[gx, gy] ?? new SKColor(0, 0, 0, 0);
                }
                else
                {
                    color = new SKColor(0, 0, 0, 0);
                }

                bitmap.SetPixel(x, y, color);
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
    }

    private SKColor?[,]? _preRecolorPixels;

    public float GetDominantTargetHue()
    {
        var targetRect = GetEffectiveExportRect();
        return RecolorService.DetectDominantHue(_pixels, targetRect.Left, targetRect.Top, targetRect.Width, targetRect.Height, _gridWidth, _gridHeight);
    }

    public void BeginRecolor()
    {
        _preRecolorPixels = (SKColor?[,])_pixels.Clone();
    }

    public void PreviewRecolor(float srcH, float tol, float tgtH, float minS, float minV, float satMult, bool shadowShift)
    {
        if (_preRecolorPixels == null) return;

        var targetRect = GetEffectiveExportRect();
        int endX = Math.Min(_gridWidth, targetRect.Left + targetRect.Width);
        int endY = Math.Min(_gridHeight, targetRect.Top + targetRect.Height);

        for (int y = targetRect.Top; y < endY; y++)
        {
            for (int x = targetRect.Left; x < endX; x++)
            {
                var orig = _preRecolorPixels[x, y];
                if (orig.HasValue)
                {
                    _pixels[x, y] = RecolorService.TransformPixel(orig.Value, srcH, tol, tgtH, minS, minV, satMult, shadowShift);
                }
                else
                {
                    _pixels[x, y] = null;
                }
            }
        }

        InvalidateVisual();
    }

    public void CommitRecolor(float srcH, float tol, float tgtH, float minS, float minV, float satMult, bool shadowShift)
    {
        if (_preRecolorPixels == null) return;

        PreviewRecolor(srcH, tol, tgtH, minS, minV, satMult, shadowShift);

        var targetRect = GetEffectiveExportRect();
        int endX = Math.Min(_gridWidth, targetRect.Left + targetRect.Width);
        int endY = Math.Min(_gridHeight, targetRect.Top + targetRect.Height);

        var diffs = new List<PixelDiff>();
        for (int y = targetRect.Top; y < endY; y++)
        {
            for (int x = targetRect.Left; x < endX; x++)
            {
                var oldCol = _preRecolorPixels[x, y];
                var newCol = _pixels[x, y];
                if (oldCol != newCol)
                {
                    diffs.Add(new PixelDiff(x, y, oldCol, newCol));
                }
            }
        }

        _preRecolorPixels = null;

        if (diffs.Count > 0)
        {
            _sessionManager.RecordStep(diffs);
            RefreshIslands();
        }

        InvalidateVisual();
        StatusChanged?.Invoke();
    }

    public void CancelRecolor()
    {
        if (_preRecolorPixels == null) return;

        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                _pixels[x, y] = _preRecolorPixels[x, y];
            }
        }

        _preRecolorPixels = null;
        InvalidateVisual();
        StatusChanged?.Invoke();
    }

    public void Undo()
    {
        if (_sessionManager.Undo(_pixels, _gridWidth, _gridHeight))
        {
            RefreshIslands();
            InvalidateVisual();
            StatusChanged?.Invoke();
        }
    }

    public void Redo()
    {
        if (_sessionManager.Redo(_pixels, _gridWidth, _gridHeight))
        {
            RefreshIslands();
            InvalidateVisual();
            StatusChanged?.Invoke();
        }
    }

    private void UpdateHoveredCoordinates(Point pos)
    {
        double localX = (pos.X - _panOffset.X) / _zoom;
        double localY = (pos.Y - _panOffset.Y) / _zoom;

        double cellWidth = _canvasSize.Width / _gridWidth;
        double cellHeight = _canvasSize.Height / _gridHeight;

        int px = (int)Math.Floor(localX / cellWidth);
        int py = (int)Math.Floor(localY / cellHeight);

        int? newX = (px >= 0 && px < _gridWidth && py >= 0 && py < _gridHeight) ? px : null;
        int? newY = (px >= 0 && px < _gridWidth && py >= 0 && py < _gridHeight) ? py : null;

        if (newX != HoveredX || newY != HoveredY)
        {
            HoveredX = newX;
            HoveredY = newY;
            InvalidateVisual();
            StatusChanged?.Invoke();
        }
    }

    private void PaintPixel(Point pos, SKColor? color)
    {
        double localX = (pos.X - _panOffset.X) / _zoom;
        double localY = (pos.Y - _panOffset.Y) / _zoom;

        double cellWidth = _canvasSize.Width / _gridWidth;
        double cellHeight = _canvasSize.Height / _gridHeight;

        int px = (int)Math.Floor(localX / cellWidth);
        int py = (int)Math.Floor(localY / cellHeight);

        if (px >= 0 && px < _gridWidth && py >= 0 && py < _gridHeight)
        {
            if (Selection.HasValue && !Selection.Value.Contains(px, py))
            {
                return;
            }

            if (_pixels[px, py] != color)
            {
                var oldColor = _pixels[px, py];
                _pixels[px, py] = color;

                if (!_strokeChanges.ContainsKey((px, py)))
                {
                    _strokeChanges[(px, py)] = (oldColor, color);
                }
                else
                {
                    var initialOld = _strokeChanges[(px, py)].OldColor;
                    _strokeChanges[(px, py)] = (initialOld, color);
                }

                InvalidateVisual();
            }
        }
    }

    private void ApplyBucket(Point pos, SKColor fillColor)
    {
        double localX = (pos.X - _panOffset.X) / _zoom;
        double localY = (pos.Y - _panOffset.Y) / _zoom;

        double cellWidth = _canvasSize.Width / _gridWidth;
        double cellHeight = _canvasSize.Height / _gridHeight;

        int startX = (int)Math.Floor(localX / cellWidth);
        int startY = (int)Math.Floor(localY / cellHeight);

        if (startX < 0 || startX >= _gridWidth || startY < 0 || startY >= _gridHeight)
        {
            return;
        }

        if (Selection.HasValue && !Selection.Value.Contains(startX, startY))
        {
            return;
        }

        var targetColor = _pixels[startX, startY];
        if (targetColor == fillColor)
        {
            return;
        }

        var diffs = new List<PixelDiff>();
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((startX, startY));
        _pixels[startX, startY] = fillColor;
        diffs.Add(new PixelDiff(startX, startY, targetColor, fillColor));

        (int dx, int dy)[] directions = { (0, 1), (0, -1), (1, 0), (-1, 0) };

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();

            foreach (var (dx, dy) in directions)
            {
                int nx = cx + dx;
                int ny = cy + dy;

                if (nx >= 0 && nx < _gridWidth && ny >= 0 && ny < _gridHeight && _pixels[nx, ny] == targetColor)
                {
                    if (Selection.HasValue && !Selection.Value.Contains(nx, ny))
                    {
                        continue;
                    }

                    _pixels[nx, ny] = fillColor;
                    diffs.Add(new PixelDiff(nx, ny, targetColor, fillColor));
                    queue.Enqueue((nx, ny));
                }
            }
        }

        if (diffs.Count > 0)
        {
            _sessionManager.RecordStep(diffs);
            RefreshIslands();
        }

        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Cursor = Cursor.Default;
        if (HoveredX != null || HoveredY != null)
        {
            HoveredX = null;
            HoveredY = null;
            InvalidateVisual();
            StatusChanged?.Invoke();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        var pos = e.GetPosition(this);
        _lastRawMousePos = pos;
        UpdateHoveredCoordinates(pos);

        double localX = (pos.X - _panOffset.X) / _zoom;
        double localY = (pos.Y - _panOffset.Y) / _zoom;

        double cellWidth = _canvasSize.Width / _gridWidth;
        double cellHeight = _canvasSize.Height / _gridHeight;

        int px = (int)Math.Floor(localX / cellWidth);
        int py = (int)Math.Floor(localY / cellHeight);

        bool isMoveMode = SettingsManager.LastSelectedTool == "Selection" || e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool isInsideSelection = Selection.HasValue && Selection.Value.Contains(px, py);

        if (point.Properties.IsLeftButtonPressed && isMoveMode && isInsideSelection)
        {
            _isMovingSelection = true;
            _moveStartPixel = new Point(px, py);
            _initialSelection = Selection!.Value;

            _floatingPixels = new SKColor?[_initialSelection.Width, _initialSelection.Height];
            _cutOriginals = new Dictionary<(int, int), SKColor?>();

            for (int y = 0; y < _initialSelection.Height; y++)
            {
                for (int x = 0; x < _initialSelection.Width; x++)
                {
                    int sx = _initialSelection.Left + x;
                    int sy = _initialSelection.Top + y;
                    if (sx >= 0 && sx < _gridWidth && sy >= 0 && sy < _gridHeight)
                    {
                        _floatingPixels[x, y] = _pixels[sx, sy];
                        _cutOriginals[(sx, sy)] = _pixels[sx, sy];
                        _pixels[sx, sy] = null;
                    }
                }
            }

            e.Pointer.Capture(this);
            InvalidateVisual();
            UpdateCursor(pos, e.KeyModifiers);
            return;
        }

        if (point.Properties.IsLeftButtonPressed && Selection.HasValue && !isInsideSelection)
        {
            ClearSelection();
        }

        if (SettingsManager.LastSelectedTool == "Selection")
        {
            if (point.Properties.IsLeftButtonPressed)
            {
                if (px >= 0 && px < _gridWidth && py >= 0 && py < _gridHeight)
                {
                    _isSelecting = true;
                    _selectionStart = new Point(px, py);
                    Selection = new SKRectI(px, py, px + 1, py + 1);
                    e.Pointer.Capture(this);
                    InvalidateVisual();
                    StatusChanged?.Invoke();
                }
                else
                {
                    ClearSelection();
                }
            }
            else if (point.Properties.IsMiddleButtonPressed || point.Properties.IsRightButtonPressed)
            {
                _isPanning = true;
                _lastMousePos = pos;
                e.Pointer.Capture(this);
            }
        }
        else if (SettingsManager.LastSelectedTool == "Brush")
        {
            if (point.Properties.IsLeftButtonPressed)
            {
                _isDrawing = true;
                _strokeChanges.Clear();
                _currentDrawingColor = SettingsManager.PrimaryColor;
                PaintPixel(pos, _currentDrawingColor);
                e.Pointer.Capture(this);
            }
            else if (point.Properties.IsRightButtonPressed)
            {
                _isDrawing = true;
                _strokeChanges.Clear();
                _currentDrawingColor = SettingsManager.SecondaryColor;
                PaintPixel(pos, _currentDrawingColor);
                e.Pointer.Capture(this);
            }
            else if (point.Properties.IsMiddleButtonPressed)
            {
                _isPanning = true;
                _lastMousePos = pos;
                e.Pointer.Capture(this);
            }
        }
        else if (SettingsManager.LastSelectedTool == "Eraser")
        {
            if (point.Properties.IsLeftButtonPressed)
            {
                _isDrawing = true;
                _strokeChanges.Clear();
                _currentDrawingColor = null;
                PaintPixel(pos, null);
                e.Pointer.Capture(this);
            }
            else if (point.Properties.IsMiddleButtonPressed || point.Properties.IsRightButtonPressed)
            {
                _isPanning = true;
                _lastMousePos = pos;
                e.Pointer.Capture(this);
            }
        }
        else if (SettingsManager.LastSelectedTool == "Bucket")
        {
            if (point.Properties.IsLeftButtonPressed)
            {
                ApplyBucket(pos, SettingsManager.PrimaryColor);
            }
            else if (point.Properties.IsRightButtonPressed)
            {
                ApplyBucket(pos, SettingsManager.SecondaryColor);
            }
            else if (point.Properties.IsMiddleButtonPressed)
            {
                _isPanning = true;
                _lastMousePos = pos;
                e.Pointer.Capture(this);
            }
        }

        UpdateCursor(pos, e.KeyModifiers);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var current = e.GetPosition(this);
        _lastRawMousePos = current;
        UpdateHoveredCoordinates(current);

        double localX = (current.X - _panOffset.X) / _zoom;
        double localY = (current.Y - _panOffset.Y) / _zoom;

        double cellWidth = _canvasSize.Width / _gridWidth;
        double cellHeight = _canvasSize.Height / _gridHeight;

        if (_isMovingSelection)
        {
            int px = (int)Math.Floor(localX / cellWidth);
            int py = (int)Math.Floor(localY / cellHeight);

            int dx = px - (int)_moveStartPixel.X;
            int dy = py - (int)_moveStartPixel.Y;

            Selection = new SKRectI(
                _initialSelection.Left + dx,
                _initialSelection.Top + dy,
                _initialSelection.Right + dx,
                _initialSelection.Bottom + dy
            );

            InvalidateVisual();
            StatusChanged?.Invoke();
        }
        else if (_isSelecting)
        {
            int px = Math.Clamp((int)Math.Floor(localX / cellWidth), 0, _gridWidth - 1);
            int py = Math.Clamp((int)Math.Floor(localY / cellHeight), 0, _gridHeight - 1);

            int startX = (int)_selectionStart.X;
            int startY = (int)_selectionStart.Y;

            int minX = Math.Min(startX, px);
            int maxX = Math.Max(startX, px);
            int minY = Math.Min(startY, py);
            int maxY = Math.Max(startY, py);

            Selection = new SKRectI(minX, minY, maxX + 1, maxY + 1);
            InvalidateVisual();
            StatusChanged?.Invoke();
        }
        else if (_isDrawing)
        {
            PaintPixel(current, _currentDrawingColor);
        }
        else if (_isPanning)
        {
            _panOffset = new Point(
                _panOffset.X + (current.X - _lastMousePos.X),
                _panOffset.Y + (current.Y - _lastMousePos.Y)
            );
            _lastMousePos = current;
            InvalidateVisual();
        }

        UpdateCursor(current, e.KeyModifiers);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isMovingSelection)
        {
            _isMovingSelection = false;
            e.Pointer.Capture(null);

            if (_floatingPixels != null && Selection.HasValue)
            {
                var curSel = Selection.Value;
                var diffs = new List<PixelDiff>();
                var newPlaced = new Dictionary<(int, int), SKColor?>();

                for (int y = 0; y < _floatingPixels.GetLength(1); y++)
                {
                    for (int x = 0; x < _floatingPixels.GetLength(0); x++)
                    {
                        int tx = curSel.Left + x;
                        int ty = curSel.Top + y;
                        var newCol = _floatingPixels[x, y];

                        if (tx >= 0 && tx < _gridWidth && ty >= 0 && ty < _gridHeight)
                        {
                            newPlaced[(tx, ty)] = newCol;
                        }
                    }
                }

                if (_cutOriginals != null)
                {
                    foreach (var (origCoord, origCol) in _cutOriginals)
                    {
                        var finalCol = newPlaced.TryGetValue(origCoord, out var c) ? c : null;
                        if (origCol != finalCol)
                        {
                            diffs.Add(new PixelDiff(origCoord.Item1, origCoord.Item2, origCol, finalCol));
                        }
                    }
                }

                foreach (var (newCoord, newCol) in newPlaced)
                {
                    if (_cutOriginals == null || !_cutOriginals.ContainsKey(newCoord))
                    {
                        var oldCanvasCol = _pixels[newCoord.Item1, newCoord.Item2];
                        if (oldCanvasCol != newCol)
                        {
                            diffs.Add(new PixelDiff(newCoord.Item1, newCoord.Item2, oldCanvasCol, newCol));
                        }
                    }
                    _pixels[newCoord.Item1, newCoord.Item2] = newCol;
                }

                _floatingPixels = null;
                _cutOriginals = null;

                if (diffs.Count > 0)
                {
                    _sessionManager.RecordStep(diffs);
                    RefreshIslands();
                }

                InvalidateVisual();
                StatusChanged?.Invoke();
            }
        }
        else if (_isSelecting)
        {
            _isSelecting = false;
            e.Pointer.Capture(null);
        }
        else if (_isDrawing)
        {
            _isDrawing = false;
            _currentDrawingColor = null;
            e.Pointer.Capture(null);

            if (_strokeChanges.Count > 0)
            {
                var diffs = new List<PixelDiff>(_strokeChanges.Count);
                foreach (var (coord, colors) in _strokeChanges)
                {
                    if (colors.OldColor != colors.NewColor)
                    {
                        diffs.Add(new PixelDiff(coord.X, coord.Y, colors.OldColor, colors.NewColor));
                    }
                }
                _strokeChanges.Clear();

                if (diffs.Count > 0)
                {
                    _sessionManager.RecordStep(diffs);
                    RefreshIslands();
                }
            }
        }

        _isPanning = false;
        e.Pointer.Capture(null);
        UpdateCursor(_lastRawMousePos, e.KeyModifiers);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var pos = e.GetPosition(this);
        float oldZoom = _zoom;
        float factor = e.Delta.Y > 0 ? 1.15f : 1.0f / 1.15f;
        float newZoom = Math.Clamp(_zoom * factor, 0.05f, 50.0f);

        _panOffset = new Point(
            pos.X - (pos.X - _panOffset.X) * (newZoom / oldZoom),
            pos.Y - (pos.Y - _panOffset.Y) * (newZoom / oldZoom)
        );
        _zoom = newZoom;

        UpdateHoveredCoordinates(pos);
        InvalidateVisual();
        StatusChanged?.Invoke();
        UpdateCursor(pos, e.KeyModifiers);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!_isInitialized && Bounds.Width > 0 && Bounds.Height > 0)
        {
            _panOffset = new Point(
                (Bounds.Width - _canvasSize.Width) / 2.0,
                (Bounds.Height - _canvasSize.Height) / 2.0
            );
            _isInitialized = true;
        }

        context.Custom(new CanvasDrawOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            _zoom,
            _panOffset,
            _canvasSize,
            _pixels,
            _gridWidth,
            _gridHeight,
            Selection,
            _floatingPixels,
            _spritesheet,
            HoveredX,
            HoveredY
        ));
    }
}
