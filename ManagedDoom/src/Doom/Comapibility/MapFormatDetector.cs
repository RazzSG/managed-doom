using System;
using System.IO;

namespace ManagedDoom;

public enum MapContainerFormat
{
    DoomBinary,
    HexenBinary,
    Udmf
}

/// <summary>
/// Detects the outer map representation before individual map lumps are parsed.
/// Gameplay compatibility (Vanilla/Boom/MBF/MBF21) is intentionally separate:
/// those compatibility levels can all use the same Doom binary map container.
/// </summary>
public static class MapFormatDetector
{
    public static MapContainerFormat Detect(Wad wad, int mapLump)
    {
        if (wad == null)
            throw new ArgumentNullException(nameof(wad));

        var lumps = wad.LumpInfos;

        if ((uint)mapLump >= (uint)lumps.Count)
            throw new ArgumentOutOfRangeException(nameof(mapLump));

        if (HasLumpAt(lumps, mapLump + 1, "TEXTMAP"))
            return MapContainerFormat.Udmf;

        if (!HasLumpAt(lumps, mapLump + 1, "THINGS") ||
            !HasLumpAt(lumps, mapLump + 2, "LINEDEFS") ||
            !HasLumpAt(lumps, mapLump + 3, "SIDEDEFS") ||
            !HasLumpAt(lumps, mapLump + 4, "VERTEXES"))
        {
            throw new InvalidDataException($"Map '{lumps[mapLump].Name}' does not have a recognized Doom map-lump layout.");
        }

        // Hexen-format binary maps use the same leading lump names as Doom-format maps
        // but add BEHAVIOR after BLOCKMAP.
        return HasLumpAt(lumps, mapLump + 11, "BEHAVIOR") ? MapContainerFormat.HexenBinary : MapContainerFormat.DoomBinary;
    }

    public static void EnsureDoomBinary(Wad wad, int mapLump)
    {
        var format = Detect(wad, mapLump);

        if (format == MapContainerFormat.DoomBinary)
            return;

        var name = wad.LumpInfos[mapLump].Name;

        throw new InvalidDataException(format switch
            {
                MapContainerFormat.HexenBinary =>
                    $"Map '{name}' uses the Hexen binary map format. " +
                    "ManagedDoom's Doom/Boom/MBF/MBF21 path must not parse Hexen LINEDEFS/THINGS as Doom data.",
                MapContainerFormat.Udmf => $"Map '{name}' uses UDMF (TEXTMAP). Add a UDMF loader as a separate map-container backend.",
                _ => $"Map '{name}' uses unsupported map format '{format}'."
            });
    }

    private static bool HasLumpAt(System.Collections.Generic.IReadOnlyList<LumpInfo> lumps, int index, string name)
    {
        return (uint)index < (uint)lumps.Count && string.Equals(lumps[index].Name, name, StringComparison.OrdinalIgnoreCase);
    }
}
