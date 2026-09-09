using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// MBF helper-dog metadata that is independent from the map-spawn policy.
/// The original port bundled DOGS graphics; ManagedDoom must verify that a
/// loaded resource set actually supplies every frame before spawning the actor.
/// </summary>
public static class MbfDogActor
{
    public const int DoomEdNum = 888;
    public const int RequiredSpriteFrameCount = 14;

    public static bool HasRenderableSprites(ISpriteLookup sprites)
    {
        if (sprites == null)
            return false;

        var frames = sprites[Sprite.DOGS]?.Frames;
        if (frames == null || frames.Length < RequiredSpriteFrameCount)
            return false;

        for (var i = 0; i < RequiredSpriteFrameCount; i++)
        {
            var frame = frames[i];
            if (frame?.Patches == null || frame.Patches.Length < 8)
                return false;

            for (var rotation = 0; rotation < 8; rotation++)
            {
                if (frame.Patches[rotation] == null)
                    return false;
            }
        }

        return true;
    }
}
