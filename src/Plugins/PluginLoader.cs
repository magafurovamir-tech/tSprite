using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using tSprite.API;

namespace tSprite.Plugins;

public static class PluginLoader
{
    public static string GetPluginsDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "tSprite", "Plugins");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static void LoadAll(ITSpriteContext context)
    {
        var pluginsDir = GetPluginsDirectory();
        if (!Directory.Exists(pluginsDir)) return;

        foreach (var dllPath in Directory.EnumerateFiles(pluginsDir, "*.dll", SearchOption.AllDirectories))
        {
            var normPath = dllPath.Replace(Path.DirectorySeparatorChar, '/');
            if (normPath.Contains("/bin/") || normPath.Contains("/obj/") || normPath.Contains("/runtimes/"))
            {
                continue;
            }

            var fileName = Path.GetFileName(dllPath);
            if (fileName.Equals("tSprite.dll", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("SkiaSharp", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("lib", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("av_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(dllPath));
                var pluginTypes = asm.GetTypes().Where(t => typeof(ITSpritePlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in pluginTypes)
                {
                    if (Activator.CreateInstance(type) is ITSpritePlugin plugin)
                    {
                        plugin.OnLoad(context);
                        Console.WriteLine($"[PluginLoader] Loaded: {plugin.Name} by {plugin.Author}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginLoader] Error loading {fileName}: {ex.Message}");
            }
        }
    }
}
