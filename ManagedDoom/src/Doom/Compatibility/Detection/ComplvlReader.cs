using System;
using System.Text;

using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Detection;

public static class ComplvlReader
{
    public static bool TryRead(Wad wad, out GameCompatibility compatibility)
    {
        if (wad == null)
            throw new ArgumentNullException(nameof(wad));

        compatibility = GameCompatibility.Vanilla;

        var lumpNumber = wad.GetLumpNumber("COMPLVL");
        if (lumpNumber == -1)
            return false;

        var value = Encoding.UTF8.GetString(wad.ReadLump(lumpNumber)).Trim('\uFEFF', '\0', ' ', '\t', '\r', '\n');

        if (value.Equals("vanilla", StringComparison.OrdinalIgnoreCase))
            compatibility = GameCompatibility.Vanilla;
        else if (value.Equals("boom", StringComparison.OrdinalIgnoreCase))
            compatibility = GameCompatibility.Boom;
        else if (value.Equals("mbf", StringComparison.OrdinalIgnoreCase))
            compatibility = GameCompatibility.Mbf;
        else if (value.Equals("mbf21", StringComparison.OrdinalIgnoreCase))
            compatibility = GameCompatibility.Mbf21;
        else
            return false;

        return true;
    }
}
