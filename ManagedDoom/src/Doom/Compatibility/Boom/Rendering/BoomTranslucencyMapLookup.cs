using System;
using System.Collections.Generic;
using ManagedDoom.Compatibility.Boom.Rendering;

namespace ManagedDoom.Compatibility.Boom.Rendering;

/// <summary>
/// Loads Boom translucency resources from the final WAD namespace.
/// The default TRANMAP is resolved eagerly; named 64K maps are resolved
/// lazily and cached only when a translucent line actually selects them.
/// </summary>
public sealed class BoomTranslucencyMapLookup
{
    public const int TableSize = 256 * 256;

    private readonly Wad wad;
    private readonly byte[] defaultMap;
    private readonly Dictionary<string, byte[]> customMaps;

    public BoomTranslucencyMapLookup(Wad wad)
    {
        this.wad = wad ?? throw new ArgumentNullException(nameof(wad));
        customMaps = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        var lump = GetFinalLumpNumber(BoomTranslucentLineResolver.DefaultMapName);
        if (lump != -1 && wad.GetLumpSize(lump) == TableSize)
            defaultMap = wad.ReadLump(lump);
    }

    /// <summary>
    /// Resolves the filter selected by Boom linedef 260. Invalid or missing
    /// custom selectors fall back to the final TRANMAP. A null result means
    /// neither a valid WAD TRANMAP nor a valid custom map exists, so the
    /// renderer should use its generated Boom fallback table.
    /// </summary>
    public byte[] ResolveMap(string name)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            string.Equals(name, BoomTranslucentLineResolver.DefaultMapName, StringComparison.OrdinalIgnoreCase))
        {
            return defaultMap;
        }

        return TryGetCustomMap(name, out var map) ? map : defaultMap;
    }

    /// <summary>
    /// Returns a named custom translucency lump only when the final lump with
    /// that name is exactly 64K, matching Boom's W_CheckNumForName/length test.
    /// Invalid and missing names are cached as misses. TRANMAP is reserved for
    /// the default filter and is therefore not reported as a custom map.
    /// </summary>
    public bool TryGetCustomMap(string name, out byte[] map)
    {
        map = null;

        if (string.IsNullOrWhiteSpace(name) ||
            string.Equals(name, BoomTranslucentLineResolver.DefaultMapName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!customMaps.TryGetValue(name, out map))
        {
            var lump = GetFinalLumpNumber(name);
            map = lump != -1 && wad.GetLumpSize(lump) == TableSize
                ? wad.ReadLump(lump)
                : null;

            // Cache misses/invalid sizes as null as well. Repeated setup/render
            // work must never rescan the WAD directory for a bad selector.
            customMaps[name] = map;
        }

        return map != null;
    }

    private int GetFinalLumpNumber(string name)
    {
        var lumps = wad.LumpInfos;
        for (var i = lumps.Count - 1; i >= 0; i--)
        {
            if (string.Equals(lumps[i].Name, name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public bool HasDefaultMap => defaultMap != null;

    public byte[] DefaultMap => defaultMap;
}
