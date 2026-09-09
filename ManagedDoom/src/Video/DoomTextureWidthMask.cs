using System;

namespace ManagedDoom.Video;

/// <summary>
/// Classic Doom/PrBoom horizontal wall-texture width mask.
///
/// The original renderer does not use <c>width - 1</c> for arbitrary texture
/// widths. It selects the largest power of two not greater than the texture
/// width and uses that value minus one as the column mask. For example, a
/// 36-pixel texture uses mask 31, not 35.
/// </summary>
public static class DoomTextureWidthMask
{
    public static int Resolve(int width)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        var powerOfTwo = 1;
        while (powerOfTwo <= width / 2)
            powerOfTwo <<= 1;

        return powerOfTwo - 1;
    }
}
