using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using SkiaSharp;
using tSprite.Controls;

namespace tSprite.Common;

public static class ClipboardService
{
    public static async Task CopyAsync(TopLevel? topLevel, CanvasControl canvas)
    {
        if (topLevel?.Clipboard == null) return;

        using var bitmap = canvas.GetEffectiveBitmap();
        using var stream = new MemoryStream();
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        var bytes = stream.ToArray();
        var base64 = "data:image/png;base64," + Convert.ToBase64String(bytes);

        var dataObject = new DataObject();
        dataObject.Set("PNG", bytes);
        dataObject.Set("image/png", bytes);
        dataObject.Set(DataFormats.Text, base64);
        await topLevel.Clipboard.SetDataObjectAsync(dataObject);
    }

    public static async Task PasteAsync(TopLevel? topLevel, CanvasControl canvas)
    {
        if (topLevel?.Clipboard == null) return;

        var text = await topLevel.Clipboard.GetTextAsync();
        if (!string.IsNullOrEmpty(text))
        {
            var raw = text.StartsWith("data:image/png;base64,") ? text["data:image/png;base64,".Length..] : text;
            try
            {
                var bytes = Convert.FromBase64String(raw);
                using var skBitmap = SKBitmap.Decode(bytes);
                if (skBitmap != null)
                {
                    canvas.PasteBitmap(skBitmap);
                    return;
                }
            }
            catch { }

            if (File.Exists(text))
            {
                using var skBitmap = SKBitmap.Decode(text);
                if (skBitmap != null)
                {
                    canvas.PasteBitmap(skBitmap);
                    return;
                }
            }
        }

        var pngData = await topLevel.Clipboard.GetDataAsync("PNG") ?? await topLevel.Clipboard.GetDataAsync("image/png");
        if (pngData is byte[] pngBytes)
        {
            using var skBitmap = SKBitmap.Decode(pngBytes);
            if (skBitmap != null)
            {
                canvas.PasteBitmap(skBitmap);
            }
        }
    }
}
