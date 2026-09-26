using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using tSprite.Common;

namespace tSprite.Session;

public class SessionManager
{
    private const int MaxHistorySteps = 100;
    private readonly string _sessionDir;
    private readonly string _basePngPath;
    private readonly string _sessionHjsonPath;
    private readonly string _stepsDir;

    public int CurrentStepIndex { get; private set; }
    public int TotalSteps { get; private set; }

    public SessionManager()
    {
        string baseSessionsDir;
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            baseSessionsDir = Path.Combine(localAppData, "tSprite", "Sessions");
        }
        else
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            baseSessionsDir = Path.Combine(userProfile, ".cache", "tSprite", "sessions");
        }

        Directory.CreateDirectory(baseSessionsDir);

        var lastSessionFile = Path.Combine(baseSessionsDir, "last_session.txt");
        string sessionGuid;
        if (File.Exists(lastSessionFile))
        {
            sessionGuid = File.ReadAllText(lastSessionFile).Trim();
        }
        else
        {
            sessionGuid = Guid.NewGuid().ToString("N");
            File.WriteAllText(lastSessionFile, sessionGuid);
        }

        _sessionDir = Path.Combine(baseSessionsDir, sessionGuid);
        _stepsDir = Path.Combine(_sessionDir, "steps");
        _basePngPath = Path.Combine(_sessionDir, "base.png");
        _sessionHjsonPath = Path.Combine(_sessionDir, "session.hjson");

        Directory.CreateDirectory(_sessionDir);
        Directory.CreateDirectory(_stepsDir);
        CleanOldSessions(baseSessionsDir, sessionGuid);
    }

    private static void CleanOldSessions(string baseDir, string currentGuid)
    {
        try
        {
            foreach (var dir in Directory.GetDirectories(baseDir))
            {
                var dirName = Path.GetFileName(dir);
                if (!dirName.Equals(currentGuid, StringComparison.OrdinalIgnoreCase))
                {
                    var info = new DirectoryInfo(dir);
                    if (DateTime.UtcNow - info.LastWriteTimeUtc > TimeSpan.FromDays(3))
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
        }
        catch { }
    }

    public (int Width, int Height, SKColor?[,] Pixels) LoadSession()
    {
        if (!File.Exists(_sessionHjsonPath) || !File.Exists(_basePngPath))
        {
            var initialPixels = new SKColor?[16, 16];
            SaveBasePng(initialPixels, 16, 16);
            SaveSessionHjson();
            return (16, 16, initialPixels);
        }

        foreach (var rawLine in File.ReadAllLines(_sessionHjsonPath))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("currentStepIndex"))
            {
                var parts = line.Split(new[] { '=', ':' }, 2);
                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var step))
                {
                    CurrentStepIndex = step;
                }
            }
            else if (line.StartsWith("totalSteps"))
            {
                var parts = line.Split(new[] { '=', ':' }, 2);
                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var total))
                {
                    TotalSteps = total;
                }
            }
        }

        var (w, h, pixels) = LoadBasePng();

        for (int step = 1; step <= CurrentStepIndex; step++)
        {
            var stepPath = GetStepFilePath(step);
            if (File.Exists(stepPath))
            {
                var diffs = ReadDiffFile(stepPath);
                foreach (var diff in diffs)
                {
                    if (diff.X >= 0 && diff.X < w && diff.Y >= 0 && diff.Y < h)
                    {
                        pixels[diff.X, diff.Y] = diff.NewColor;
                    }
                }
            }
        }

        return (w, h, pixels);
    }

    public void ResetWithNewBase(SKColor?[,] pixels, int width, int height)
    {
        CurrentStepIndex = 0;
        TotalSteps = 0;

        if (Directory.Exists(_stepsDir))
        {
            foreach (var file in Directory.GetFiles(_stepsDir, "*.diff"))
            {
                File.Delete(file);
            }
        }

        SaveBasePng(pixels, width, height);
        SaveSessionHjson();
    }

    public void RecordStep(IReadOnlyList<PixelDiff> diffs)
    {
        if (diffs.Count == 0) return;

        for (int i = CurrentStepIndex + 1; i <= TotalSteps; i++)
        {
            var obsoleteFile = GetStepFilePath(i);
            if (File.Exists(obsoleteFile))
            {
                File.Delete(obsoleteFile);
            }
        }

        CurrentStepIndex++;
        TotalSteps = CurrentStepIndex;

        WriteDiffFile(GetStepFilePath(CurrentStepIndex), diffs);

        if (TotalSteps > MaxHistorySteps)
        {
            BakeOldestStep();
        }

        SaveSessionHjson();
    }

    public bool Undo(SKColor?[,] pixels, int width, int height)
    {
        if (CurrentStepIndex <= 0) return false;

        var stepPath = GetStepFilePath(CurrentStepIndex);
        if (File.Exists(stepPath))
        {
            var diffs = ReadDiffFile(stepPath);
            foreach (var diff in diffs)
            {
                if (diff.X >= 0 && diff.X < width && diff.Y >= 0 && diff.Y < height)
                {
                    pixels[diff.X, diff.Y] = diff.OldColor;
                }
            }
        }

        CurrentStepIndex--;
        SaveSessionHjson();
        return true;
    }

    public bool Redo(SKColor?[,] pixels, int width, int height)
    {
        if (CurrentStepIndex >= TotalSteps) return false;

        CurrentStepIndex++;
        var stepPath = GetStepFilePath(CurrentStepIndex);
        if (File.Exists(stepPath))
        {
            var diffs = ReadDiffFile(stepPath);
            foreach (var diff in diffs)
            {
                if (diff.X >= 0 && diff.X < width && diff.Y >= 0 && diff.Y < height)
                {
                    pixels[diff.X, diff.Y] = diff.NewColor;
                }
            }
        }

        SaveSessionHjson();
        return true;
    }

    private void BakeOldestStep()
    {
        var firstStepPath = GetStepFilePath(1);
        if (File.Exists(firstStepPath) && File.Exists(_basePngPath))
        {
            using var bitmap = SKBitmap.Decode(_basePngPath);
            if (bitmap != null)
            {
                var diffs = ReadDiffFile(firstStepPath);
                foreach (var diff in diffs)
                {
                    if (diff.X >= 0 && diff.X < bitmap.Width && diff.Y >= 0 && diff.Y < bitmap.Height)
                    {
                        bitmap.SetPixel(diff.X, diff.Y, diff.NewColor ?? new SKColor(0, 0, 0, 0));
                    }
                }

                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.Create(_basePngPath);
                data.SaveTo(stream);
            }

            File.Delete(firstStepPath);
        }

        for (int i = 2; i <= TotalSteps; i++)
        {
            var oldPath = GetStepFilePath(i);
            var newPath = GetStepFilePath(i - 1);
            if (File.Exists(oldPath))
            {
                File.Move(oldPath, newPath, true);
            }
        }

        CurrentStepIndex--;
        TotalSteps--;
    }

    private string GetStepFilePath(int step) => Path.Combine(_stepsDir, $"step_{step:D4}.diff");

    private void SaveBasePng(SKColor?[,] pixels, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, pixels[x, y] ?? new SKColor(0, 0, 0, 0));
            }
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(_basePngPath);
        data.SaveTo(stream);
    }

    private (int Width, int Height, SKColor?[,] Pixels) LoadBasePng()
    {
        using var bitmap = SKBitmap.Decode(_basePngPath);
        int w = bitmap?.Width ?? 16;
        int h = bitmap?.Height ?? 16;
        var pixels = new SKColor?[w, h];

        if (bitmap != null)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var c = bitmap.GetPixel(x, y);
                    pixels[x, y] = c.Alpha == 0 ? null : c;
                }
            }
        }

        return (w, h, pixels);
    }

    private void SaveSessionHjson()
    {
        var content = $$"""
        {
          baseImage: "base.png"
          currentStepIndex: {{CurrentStepIndex}}
          totalSteps: {{TotalSteps}}
        }
        """;
        File.WriteAllText(_sessionHjsonPath, content);
    }

    private static void WriteDiffFile(string path, IReadOnlyList<PixelDiff> diffs)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write(diffs.Count);
        foreach (var diff in diffs)
        {
            writer.Write((short)diff.X);
            writer.Write((short)diff.Y);
            writer.Write(ColorToUInt(diff.OldColor));
            writer.Write(ColorToUInt(diff.NewColor));
        }
    }

    private static List<PixelDiff> ReadDiffFile(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        int count = reader.ReadInt32();
        var diffs = new List<PixelDiff>(count);
        for (int i = 0; i < count; i++)
        {
            short x = reader.ReadInt16();
            short y = reader.ReadInt16();
            uint oldArgb = reader.ReadUInt32();
            uint newArgb = reader.ReadUInt32();
            diffs.Add(new PixelDiff(x, y, UIntToColor(oldArgb), UIntToColor(newArgb)));
        }
        return diffs;
    }

    private static uint ColorToUInt(SKColor? color)
        => color.HasValue ? ((uint)color.Value.Alpha << 24) | ((uint)color.Value.Red << 16) | ((uint)color.Value.Green << 8) | color.Value.Blue : 0;

    private static SKColor? UIntToColor(uint argb)
        => (argb >> 24) == 0 ? null : new SKColor((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >> 24));
}
