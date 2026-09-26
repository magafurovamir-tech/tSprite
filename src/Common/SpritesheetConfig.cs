using System;
using System.Collections.Generic;
using SkiaSharp;

namespace tSprite.Common;

public enum SpritesheetMode
{
    Off,
    Grid,
    Islands
}

public class SpritesheetConfig
{
    public SpritesheetMode Mode { get; set; } = SpritesheetMode.Off;
    public int FrameWidth { get; set; } = 16;
    public int FrameHeight { get; set; } = 16;
    public bool IsAuto { get; set; } = true;
    public List<SKRectI> DetectedIslands { get; set; } = new();

    public bool Enabled => Mode != SpritesheetMode.Off;

    public static List<SKRectI> FindIslands(SKColor?[,] pixels, int width, int height)
    {
        var visited = new bool[width, height];
        var list = new List<SKRectI>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[x, y].HasValue && !visited[x, y])
                {
                    int minX = x, maxX = x;
                    int minY = y, maxY = y;

                    var queue = new Queue<(int X, int Y)>();
                    queue.Enqueue((x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();

                        if (cx < minX) minX = cx;
                        if (cx > maxX) maxX = cx;
                        if (cy < minY) minY = cy;
                        if (cy > maxY) maxY = cy;

                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = cx + dx;
                                int ny = cy + dy;

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny] && pixels[nx, ny].HasValue)
                                {
                                    visited[nx, ny] = true;
                                    queue.Enqueue((nx, ny));
                                }
                            }
                        }
                    }

                    list.Add(new SKRectI(minX, minY, maxX + 1, maxY + 1));
                }
            }
        }

        list.Sort((a, b) => a.Top != b.Top ? a.Top.CompareTo(b.Top) : a.Left.CompareTo(b.Left));
        return list;
    }

    public static bool CellHasPixels(SKColor?[,] pixels, int cellX, int cellY, int cellW, int cellH, int gridW, int gridH)
    {
        int startX = cellX * cellW;
        int startY = cellY * cellH;
        int endX = Math.Min(gridW, startX + cellW);
        int endY = Math.Min(gridH, startY + cellH);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                if (pixels[x, y].HasValue)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
