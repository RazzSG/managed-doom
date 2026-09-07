using System;

namespace ManagedDoom.Compatibility.Boom.Rendering;

/// <summary>
/// Builds Boom's fallback translucency lookup when no TRANMAP lump is present.
/// The table layout matches Boom: high byte is the existing framebuffer color,
/// low byte is the newly drawn (already lit) texture color.
/// </summary>
public static class BoomTranslucencyMap
{
    public const int DefaultForegroundPercent = 66;

    private const int WeightBits = 12;
    private const int WeightUnit = 1 << WeightBits;
    private const int ForegroundWeight = (DefaultForegroundPercent << WeightBits) / 100;
    private const int BackgroundWeight = WeightUnit - ForegroundWeight;

    public static byte[] BuildDefaultIndexed(uint[] palette)
    {
        if (palette == null || palette.Length < 256)
            throw new ArgumentException("A 256-color palette is required.", nameof(palette));

        var red = new int[256];
        var green = new int[256];
        var blue = new int[256];
        var total = new long[256];

        for (var i = 0; i < 256; i++)
        {
            var color = palette[i];
            var r = red[i] = (int)(color & 0xFF);
            var g = green[i] = (int)((color >> 8) & 0xFF);
            var b = blue[i] = (int)((color >> 16) & 0xFF);

            // Same rearranged squared-distance term used by Boom's
            // R_InitTranMap. Keeping the fixed-point weights avoids subtle
            // palette-boundary differences from percentage rounding.
            total[i] = ((long)r * r + (long)g * g + (long)b * b) << (WeightBits - 1);
        }

        var result = new byte[256 * 256];

        for (var background = 0; background < 256; background++)
        {
            var backgroundR = red[background] * BackgroundWeight;
            var backgroundG = green[background] * BackgroundWeight;
            var backgroundB = blue[background] * BackgroundWeight;

            for (var foreground = 0; foreground < 256; foreground++)
            {
                var r = red[foreground] * ForegroundWeight + backgroundR;
                var g = green[foreground] * ForegroundWeight + backgroundG;
                var b = blue[foreground] * ForegroundWeight + backgroundB;

                var best = long.MaxValue;
                var bestIndex = 0;

                // Boom scans 255 -> 0 and only replaces on a strictly lower
                // error, so equal-distance ties resolve to the higher index.
                for (var color = 255; color >= 0; color--)
                {
                    var error = total[color]
                        - (long)red[color] * r
                        - (long)green[color] * g
                        - (long)blue[color] * b;

                    if (error < best)
                    {
                        best = error;
                        bestIndex = color;
                    }
                }

                result[(background << 8) | foreground] = (byte)bestIndex;
            }
        }

        return result;
    }

    public static uint BlendDefault(uint foreground, uint background)
    {
        var r = BlendChannel((int)(foreground & 0xFF), (int)(background & 0xFF));
        var g = BlendChannel((int)((foreground >> 8) & 0xFF), (int)((background >> 8) & 0xFF));
        var b = BlendChannel((int)((foreground >> 16) & 0xFF), (int)((background >> 16) & 0xFF));

        return (uint)(r | (g << 8) | (b << 16) | (255 << 24));
    }

    private static int BlendChannel(int foreground, int background)
    {
        return (foreground * ForegroundWeight + background * BackgroundWeight + (WeightUnit >> 1))
            >> WeightBits;
    }
}
