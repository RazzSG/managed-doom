using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf.Lines;

/// <summary>
/// PrBoom/MBF21 handling for linedef flag 0x0800. With the compatibility
/// option enabled, the reserved bit is a signal to discard every extended
/// linedef flag and keep only the original Doom flag range (0x0000-0x01ff).
/// With the option disabled, 0x0800 is intentionally inert.
/// </summary>
public static class MbfReservedLineFlagCompatibility
{
    public const int ReservedFlag = 0x0800;
    public const int DoomFlagsMask = 0x01ff;

    public static void Apply(
        LineDef[] lines,
        GameCompatibility compatibility,
        bool compReservedLineFlag)
    {
        if (lines == null ||
            !GameCompatibilityFeatures.SupportsMbfReservedLineFlagCompatibility(compatibility) ||
            !compReservedLineFlag)
        {
            return;
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null)
                continue;

            var flags = (int)line.Flags;
            if ((flags & ReservedFlag) != 0)
                line.Flags = (LineFlags)(flags & DoomFlagsMask);
        }
    }
}
