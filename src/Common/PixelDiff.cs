using SkiaSharp;

namespace tSprite.Common;

public readonly record struct PixelDiff(int X, int Y, SKColor? OldColor, SKColor? NewColor);
