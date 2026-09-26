namespace tSprite.API;

public interface ITSpritePlugin
{
    string Name { get; }
    string Author { get; }
    string Version { get; }
    void OnLoad(ITSpriteContext context);
}
