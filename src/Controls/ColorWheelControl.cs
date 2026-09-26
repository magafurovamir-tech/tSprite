using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace tSprite.Controls;

public class ColorWheelControl : Control
{
    private bool _isDragging;

    public static readonly StyledProperty<float> HueProperty =
        AvaloniaProperty.Register<ColorWheelControl, float>(nameof(Hue), 0f);

    public static readonly StyledProperty<float> SaturationProperty =
        AvaloniaProperty.Register<ColorWheelControl, float>(nameof(Saturation), 1f);

    public static readonly StyledProperty<float> ValueProperty =
        AvaloniaProperty.Register<ColorWheelControl, float>(nameof(Value), 1f);

    public float Hue
    {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public float Saturation
    {
        get => GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public float Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public event Action<float, float>? ColorPicked;

    static ColorWheelControl()
    {
        AffectsRender<ColorWheelControl>(HueProperty, SaturationProperty, ValueProperty);
    }

    public ColorWheelControl()
    {
        ClipToBounds = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isDragging = false;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);
        if (pt.Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            UpdateFromPoint(e.GetPosition(this));
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            UpdateFromPoint(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void UpdateFromPoint(Point pos)
    {
        float cx = (float)Bounds.Width / 2f;
        float cy = (float)Bounds.Height / 2f;
        float radius = MathF.Min(cx, cy) - 6f;
        if (radius <= 0f) return;

        float dx = (float)pos.X - cx;
        float dy = (float)pos.Y - cy;

        float dist = MathF.Sqrt(dx * dx + dy * dy);
        float sat = Math.Clamp(dist / radius, 0f, 1f);

        float angle = MathF.Atan2(dy, dx) * 180f / MathF.PI;
        if (angle < 0f) angle += 360f;

        Hue = angle;
        Saturation = sat;

        ColorPicked?.Invoke(Hue, Saturation);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.Custom(new ColorWheelDrawOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            Hue,
            Saturation,
            Value
        ));
    }
}

internal class ColorWheelDrawOperation : ICustomDrawOperation
{
    private readonly float _hue;
    private readonly float _saturation;
    private readonly float _value;

    public Rect Bounds { get; }

    public ColorWheelDrawOperation(Rect bounds, float hue, float saturation, float value)
    {
        Bounds = bounds;
        _hue = hue;
        _saturation = saturation;
        _value = value;
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

        float cx = (float)Bounds.Width / 2f;
        float cy = (float)Bounds.Height / 2f;
        float radius = MathF.Min(cx, cy) - 6f;
        if (radius <= 0f) return;

        canvas.Save();

        var sweepColors = new[]
        {
            new SKColor(255, 0, 0),
            new SKColor(255, 255, 0),
            new SKColor(0, 255, 0),
            new SKColor(0, 255, 255),
            new SKColor(0, 0, 255),
            new SKColor(255, 0, 255),
            new SKColor(255, 0, 0)
        };

        using var sweepShader = SKShader.CreateSweepGradient(new SKPoint(cx, cy), sweepColors);
        using var sweepPaint = new SKPaint
        {
            Shader = sweepShader,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(cx, cy, radius, sweepPaint);

        using var radialShader = SKShader.CreateRadialGradient(
            new SKPoint(cx, cy),
            radius,
            new[] { SKColors.White, SKColors.White.WithAlpha(0) },
            SKShaderTileMode.Clamp);

        using var satPaint = new SKPaint
        {
            Shader = radialShader,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(cx, cy, radius, satPaint);

        if (_value < 1.0f)
        {
            byte darkAlpha = (byte)Math.Clamp((int)Math.Round((1.0f - _value) * 255f), 0, 255);
            using var darkPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, darkAlpha),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawCircle(cx, cy, radius, darkPaint);
        }

        using var borderPaint = new SKPaint
        {
            Color = new SKColor(55, 55, 55),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true
        };
        canvas.DrawCircle(cx, cy, radius, borderPaint);

        float rad = _hue * MathF.PI / 180f;
        float dist = _saturation * radius;
        float hx = cx + dist * MathF.Cos(rad);
        float hy = cy + dist * MathF.Sin(rad);

        using var ringBlack = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2.5f,
            IsAntialias = true
        };
        using var ringWhite = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.2f,
            IsAntialias = true
        };

        canvas.DrawCircle(hx, hy, 5.5f, ringBlack);
        canvas.DrawCircle(hx, hy, 5.5f, ringWhite);

        canvas.Restore();
    }
}
