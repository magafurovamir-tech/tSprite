using System;
using SkiaSharp;

namespace tSprite.Common;

public static class RecolorService
{
    public static void RgbToHsv(SKColor color, out float h, out float s, out float v)
    {
        float r = color.Red / 255f;
        float g = color.Green / 255f;
        float b = color.Blue / 255f;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        v = max;
        s = max > 1e-5f ? delta / max : 0f;

        if (delta < 1e-5f)
        {
            h = 0f;
        }
        else
        {
            if (Math.Abs(r - max) < 1e-5f)
            {
                h = (g - b) / delta;
            }
            else if (Math.Abs(g - max) < 1e-5f)
            {
                h = 2f + (b - r) / delta;
            }
            else
            {
                h = 4f + (r - g) / delta;
            }

            h *= 60f;
            if (h < 0f) h += 360f;
        }
    }

    public static SKColor HsvToRgb(float h, float s, float v, byte alpha = 255)
    {
        h = (h % 360f + 360f) % 360f;
        s = Math.Clamp(s, 0f, 1f);
        v = Math.Clamp(v, 0f, 1f);

        if (s <= 1e-5f)
        {
            byte val = (byte)Math.Clamp((int)Math.Round(v * 255f), 0, 255);
            return new SKColor(val, val, val, alpha);
        }

        float sectorPos = h / 60f;
        int sectorNumber = (int)Math.Floor(sectorPos);
        float fractionalSector = sectorPos - sectorNumber;

        float p = v * (1f - s);
        float q = v * (1f - (s * fractionalSector));
        float t = v * (1f - (s * (1f - fractionalSector)));

        float r = 0, g = 0, b = 0;
        switch (sectorNumber % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new SKColor(
            (byte)Math.Clamp((int)Math.Round(r * 255f), 0, 255),
            (byte)Math.Clamp((int)Math.Round(g * 255f), 0, 255),
            (byte)Math.Clamp((int)Math.Round(b * 255f), 0, 255),
            alpha
        );
    }

    public static SKColor TransformPixel(
        SKColor original,
        float sourceHue,
        float hueTolerance,
        float targetHue,
        float minSat,
        float minVal,
        float satMult,
        bool shadowShift)
    {
        if (original.Alpha == 0) return original;

        RgbToHsv(original, out float h, out float s, out float v);

        if (s < minSat || v < minVal) return original;

        float diff = Math.Abs(h - sourceHue);
        if (diff > 180f) diff = 360f - diff;

        if (diff > hueTolerance) return original;

        float newHue = targetHue;
        if (shadowShift && v < 0.45f)
        {
            newHue = (newHue - 10f + 360f) % 360f;
        }

        float newSat = Math.Clamp(s * satMult, 0f, 1f);
        return HsvToRgb(newHue, newSat, v, original.Alpha);
    }

    public static float DetectDominantHue(SKColor?[,] pixels, int startX, int startY, int width, int height, int gridW, int gridH)
    {
        double sumSin = 0;
        double sumCos = 0;
        int count = 0;

        int endX = Math.Min(gridW, startX + width);
        int endY = Math.Min(gridH, startY + height);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                var col = pixels[x, y];
                if (col.HasValue && col.Value.Alpha > 0)
                {
                    RgbToHsv(col.Value, out float h, out float s, out float v);
                    if (s >= 0.20f && v >= 0.15f)
                    {
                        double rad = h * Math.PI / 180.0;
                        sumSin += Math.Sin(rad) * s;
                        sumCos += Math.Cos(rad) * s;
                        count++;
                    }
                }
            }
        }

        if (count == 0) return 0f;

        double avgRad = Math.Atan2(sumSin, sumCos);
        double avgDeg = avgRad * 180.0 / Math.PI;
        if (avgDeg < 0) avgDeg += 360.0;

        return (float)avgDeg;
    }
}
