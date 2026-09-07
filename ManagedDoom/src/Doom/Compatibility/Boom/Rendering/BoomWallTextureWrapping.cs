using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Rendering;

/// <summary>
/// Vertical wall-texture wrapping rules. Vanilla Doom's renderer always used
/// a 128-row mask, which produces the classic tutti-frutti artifact on short
/// textures. Boom tiles wall textures using their actual height instead.
/// </summary>
public static class BoomWallTextureWrapping
{
    public const int VanillaWrapHeight = 128;

    public static int ResolveWrapHeight(
        int textureHeight,
        GameCompatibility compatibility)
    {
        if (textureHeight <= 0)
            throw new System.ArgumentOutOfRangeException(nameof(textureHeight));

        return GameCompatibilityFeatures.SupportsBoom(compatibility)
            ? textureHeight
            : VanillaWrapHeight;
    }

    /// <summary>
    /// Wraps a row for arbitrary non-power-of-two texture heights. Power-of-two
    /// heights stay on the renderer's faster bit-mask path.
    /// </summary>
    public static int WrapNonPowerOfTwoRow(int row, int wrapHeight)
    {
        if (wrapHeight <= 0)
            throw new System.ArgumentOutOfRangeException(nameof(wrapHeight));

        var wrapped = row % wrapHeight;
        return wrapped < 0 ? wrapped + wrapHeight : wrapped;
    }
}
