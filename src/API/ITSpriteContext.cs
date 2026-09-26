using System;

namespace tSprite.API;

public interface ITSpriteContext
{
    ICanvas? ActiveCanvas { get; }
    void RegisterMenuAction(string category, string name, Action action);
}
