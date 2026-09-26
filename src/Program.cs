using System;
using Avalonia;
using tSprite.Config;

namespace tSprite;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ConfigManager.Initialize();
        SettingsManager.Initialize();
        KeymapManager.Initialize();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
