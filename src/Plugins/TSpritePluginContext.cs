using System;
using System.Collections.Generic;
using tSprite.API;

namespace tSprite.Plugins;

public class TSpritePluginContext : ITSpriteContext
{
    private ICanvas? _activeCanvas;
    public ICanvas? ActiveCanvas => _activeCanvas;

    public event Action<(string Category, string Name, Action Action)>? ActionRegistered;

    public TSpritePluginContext(ICanvas? initialCanvas)
    {
        _activeCanvas = initialCanvas;
    }

    public void SetCanvas(ICanvas? canvas)
    {
        _activeCanvas = canvas;
    }

    public void RegisterMenuAction(string category, string name, Action action)
    {
        ActionRegistered?.Invoke((category, name, action));
    }
}
