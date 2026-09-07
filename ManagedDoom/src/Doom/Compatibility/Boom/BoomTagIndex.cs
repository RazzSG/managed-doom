using System;
using System.Collections.Generic;

namespace ManagedDoom.Compatibility.Boom;

public sealed class BoomTagIndex
{
    private readonly Sector[] sectors;
    private readonly LineDef[] lines;
    private Dictionary<int, Sector[]> sectorsByTag;
    private Dictionary<int, LineDef[]> linesByTag;

    public BoomTagIndex(Sector[] sectors, LineDef[] lines)
    {
        this.sectors = sectors ?? throw new ArgumentNullException(nameof(sectors));
        this.lines = lines ?? throw new ArgumentNullException(nameof(lines));
        Rebuild();
    }

    public ReadOnlySpan<Sector> GetSectors(int tag)
    {
        if (sectorsByTag.TryGetValue(tag, out var result))
            return result;

        return ReadOnlySpan<Sector>.Empty;
    }

    public ReadOnlySpan<LineDef> GetLines(int tag)
    {
        if (linesByTag.TryGetValue(tag, out var result))
            return result;

        return ReadOnlySpan<LineDef>.Empty;
    }

    public int FindNextSectorNumber(int tag, int start)
    {
        var taggedSectors = GetSectors(tag);
        var low = 0;
        var high = taggedSectors.Length;

        while (low < high)
        {
            var middle = low + ((high - low) >> 1);

            if (taggedSectors[middle].Number <= start)
                low = middle + 1;
            else
                high = middle;
        }

        return low < taggedSectors.Length ? taggedSectors[low].Number : -1;
    }

    public void Rebuild()
    {
        sectorsByTag = BuildIndex(sectors, static sector => sector.Tag);
        linesByTag = BuildIndex(lines, static line => line.Tag);
    }

    private static Dictionary<int, T[]> BuildIndex<T>(T[] items, Func<T, int> getTag)
    {
        var buckets = new Dictionary<int, List<T>>();

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var tag = getTag(item);

            if (!buckets.TryGetValue(tag, out var bucket))
            {
                bucket = new List<T>();
                buckets.Add(tag, bucket);
            }

            bucket.Add(item);
        }

        var index = new Dictionary<int, T[]>(buckets.Count);

        foreach (var pair in buckets)
            index.Add(pair.Key, pair.Value.ToArray());

        return index;
    }
}
