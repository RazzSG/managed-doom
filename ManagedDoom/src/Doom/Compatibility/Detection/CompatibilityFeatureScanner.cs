using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Detection;

public static class CompatibilityFeatureScanner
{
    private const int DoomThingSize = 10;
    private const int DoomThingTypeOffset = 6;
    private const int DoomThingFlagsOffset = 8;

    private const int DoomLineDefSize = 14;
    private const int DoomLineFlagsOffset = 4;
    private const int DoomLineSpecialOffset = 6;

    private const int DoomSectorSize = 26;
    private const int DoomSectorSpecialOffset = 22;

    private const ushort BoomThingFlagsMask = 0x0060;
    private const ushort MbfThingFlagsMask = 0x0080;
    private const ushort BoomPassThruFlag = 0x0200;
    private const ushort Mbf21LineFlagsMask = 0x3000;
    private const ushort BoomSectorFlagsMask = 0x0FE0;
    private const ushort Mbf21SectorFlagsMask = 0x3000;

    private const ushort BoomRegularSpecialFirst = 142;
    private const ushort BoomRegularSpecialLast = 269;
    private const ushort BoomGeneralizedSpecialFirst = 0x2F80;
    private const ushort BoomGeneralizedSpecialLast = 0x7FFF;

    public static bool TryDetect(Wad wad, out GameCompatibility compatibility)
    {
        if (wad == null)
            throw new ArgumentNullException(nameof(wad));

        var hasBoomResource =
            wad.GetLumpNumber("SWITCHES") != -1 ||
            wad.GetLumpNumber("ANIMATED") != -1;
        compatibility = hasBoomResource ? GameCompatibility.Boom : GameCompatibility.Vanilla;
        var found = hasBoomResource;
        var scannedMapNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lumps = wad.LumpInfos;

        // Scan backwards so only the effective (last loaded) definition of each map is considered.
        for (var mapLump = lumps.Count - 1; mapLump >= 0; mapLump--)
        {
            if (!IsMapMarkerAt(lumps, mapLump) || !scannedMapNames.Add(lumps[mapLump].Name))
                continue;

            if (!IsDoomBinaryMapAt(lumps, mapLump))
                continue;

            var mapCompatibility = ScanMap(wad, mapLump);

            if (mapCompatibility == GameCompatibility.Mbf21)
            {
                compatibility = GameCompatibility.Mbf21;
                return true;
            }

            if (mapCompatibility == GameCompatibility.Mbf)
            {
                compatibility = GameCompatibility.Mbf;
                found = true;
            }
            else if (mapCompatibility == GameCompatibility.Boom && !found)
            {
                compatibility = GameCompatibility.Boom;
                found = true;
            }
        }

        return found;
    }

    private static GameCompatibility ScanMap(Wad wad, int mapLump)
    {
        var lineData = wad.ReadLump(mapLump + 2);
        var thingData = wad.ReadLump(mapLump + 1);
        var sectorData = HasLumpAt(wad.LumpInfos, mapLump + 8, "SECTORS", wad.LumpInfos[mapLump].Stream) ? wad.ReadLump(mapLump + 8) : Array.Empty<byte>();

        if (HasMbf21LineFeature(lineData) || HasMbf21SectorFeature(sectorData))
            return GameCompatibility.Mbf21;

        if (HasMbfLineFeature(lineData) || HasMbfThingFeature(thingData))
            return GameCompatibility.Mbf;

        if (HasBoomLineFeature(lineData) || HasBoomThingFeature(thingData) || HasBoomSectorFeature(sectorData))
            return GameCompatibility.Boom;

        return GameCompatibility.Vanilla;
    }

    private static bool HasMbf21LineFeature(byte[] data)
    {
        if (data.Length == 0 || data.Length % DoomLineDefSize != 0)
            return false;

        for (var offset = 0; offset < data.Length; offset += DoomLineDefSize)
        {
            var flags = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomLineFlagsOffset, 2));
            var special = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomLineSpecialOffset, 2));

            if ((flags & Mbf21LineFlagsMask) != 0 || special is >= 1024 and <= 1026)
                return true;
        }

        return false;
    }

    private static bool HasMbfLineFeature(byte[] data)
    {
        if (data.Length == 0 || data.Length % DoomLineDefSize != 0)
            return false;

        for (var offset = 0; offset < data.Length; offset += DoomLineDefSize)
        {
            var special = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomLineSpecialOffset, 2));

            if (special is 271 or 272)
                return true;
        }

        return false;
    }

    private static bool HasBoomLineFeature(byte[] data)
    {
        if (data.Length == 0 || data.Length % DoomLineDefSize != 0)
            return false;

        for (var offset = 0; offset < data.Length; offset += DoomLineDefSize)
        {
            var flags = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomLineFlagsOffset, 2));
            var special = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomLineSpecialOffset, 2));

            if ((flags & BoomPassThruFlag) != 0 ||
                IsBoomRegularSpecial(special) ||
                special is >= BoomGeneralizedSpecialFirst and <= BoomGeneralizedSpecialLast)
            {
                return true;
            }
        }

        return false;
    }


    private static bool IsBoomRegularSpecial(ushort special)
    {
        // Boom has two extended regular specials below the otherwise contiguous
        // 142-269 range: 78 (floor change) and 85 (scroll right).
        return special is 78 or 85 || special is >= BoomRegularSpecialFirst and <= BoomRegularSpecialLast;
    }

    private static bool HasMbfThingFeature(byte[] data)
    {
        if (data.Length == 0 || data.Length % DoomThingSize != 0)
            return false;

        for (var offset = 0; offset < data.Length; offset += DoomThingSize)
        {
            var type = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomThingTypeOffset, 2));
            var flags = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomThingFlagsOffset, 2));

            if ((flags & MbfThingFlagsMask) != 0 || type == 888)
                return true;
        }

        return false;
    }

    private static bool HasBoomThingFeature(byte[] data)
    {
        if (data.Length == 0 || data.Length % DoomThingSize != 0)
            return false;

        for (var offset = 0; offset < data.Length; offset += DoomThingSize)
        {
            var type = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomThingTypeOffset, 2));
            var flags = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomThingFlagsOffset, 2));

            if ((flags & BoomThingFlagsMask) != 0 || type is 5001 or 5002)
                return true;
        }

        return false;
    }

    private static bool HasMbf21SectorFeature(byte[] data)
    {
        if (data.Length == 0 || data.Length % DoomSectorSize != 0)
            return false;

        for (var offset = 0; offset < data.Length; offset += DoomSectorSize)
        {
            var special = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomSectorSpecialOffset, 2));

            if ((special & Mbf21SectorFlagsMask) != 0)
                return true;
        }

        return false;
    }

    private static bool HasBoomSectorFeature(byte[] data)
    {
        if (data.Length == 0 || data.Length % DoomSectorSize != 0)
            return false;

        for (var offset = 0; offset < data.Length; offset += DoomSectorSize)
        {
            var special = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + DoomSectorSpecialOffset, 2));

            if ((special & BoomSectorFlagsMask) != 0)
                return true;
        }

        return false;
    }

    private static bool IsMapMarkerAt(IReadOnlyList<LumpInfo> lumps, int mapLump)
    {
        if ((uint)mapLump >= (uint)lumps.Count)
            return false;

        var stream = lumps[mapLump].Stream;

        if (HasLumpAt(lumps, mapLump + 1, "TEXTMAP", stream))
            return true;

        return HasLumpAt(lumps, mapLump + 1, "THINGS", stream) &&
               HasLumpAt(lumps, mapLump + 2, "LINEDEFS", stream) &&
               HasLumpAt(lumps, mapLump + 3, "SIDEDEFS", stream) &&
               HasLumpAt(lumps, mapLump + 4, "VERTEXES", stream);
    }

    private static bool IsDoomBinaryMapAt(IReadOnlyList<LumpInfo> lumps, int mapLump)
    {
        var stream = lumps[mapLump].Stream;

        if (!HasLumpAt(lumps, mapLump + 1, "THINGS", stream) ||
            !HasLumpAt(lumps, mapLump + 2, "LINEDEFS", stream) ||
            !HasLumpAt(lumps, mapLump + 3, "SIDEDEFS", stream) ||
            !HasLumpAt(lumps, mapLump + 4, "VERTEXES", stream))
        {
            return false;
        }

        return !HasLumpAt(lumps, mapLump + 11, "BEHAVIOR", stream);
    }

    private static bool HasLumpAt(IReadOnlyList<LumpInfo> lumps, int index, string name, Stream stream)
    {
        return (uint)index < (uint)lumps.Count &&
               ReferenceEquals(lumps[index].Stream, stream) &&
               string.Equals(lumps[index].Name, name, StringComparison.OrdinalIgnoreCase);
    }
}
