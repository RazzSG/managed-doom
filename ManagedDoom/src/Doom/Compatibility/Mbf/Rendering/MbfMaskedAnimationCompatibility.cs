using System;

namespace ManagedDoom.Compatibility.Mbf.Rendering;

/// <summary>
/// PrBoom compatibility selector for the old two-sided masked-middle-texture
/// animation bug. Doom v1.2 used the original sidedef texture number here
/// instead of the global texture translation table.
/// </summary>
public static class MbfMaskedAnimationCompatibility
{
    public static bool AnimatesTwoSidedMiddleTextures(
        GameCompatibility compatibility,
        bool compMaskedAnim)
    {
        if (!GameCompatibilityFeatures.SupportsMbfMaskedAnimationCompatibility(compatibility))
            return true;

        // MBF21 deoptionalizes comp_maskedanim and always keeps the fix enabled.
        if (GameCompatibilityFeatures.SupportsMbf21(compatibility))
            return true;

        return !compMaskedAnim;
    }

    public static int ResolveTwoSidedMiddleTexture(
        GameCompatibility compatibility,
        bool compMaskedAnim,
        int textureNumber,
        int[] textureTranslation)
    {
        if (textureTranslation == null)
            throw new ArgumentNullException(nameof(textureTranslation));

        if (!AnimatesTwoSidedMiddleTextures(compatibility, compMaskedAnim))
            return textureNumber;

        return textureTranslation[textureNumber];
    }
}
