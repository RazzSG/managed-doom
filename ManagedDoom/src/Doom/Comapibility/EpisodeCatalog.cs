using System;
using System.Collections.Generic;

namespace ManagedDoom
{
    /// <summary>
    /// Discovers Doom-style episode maps from the merged WAD directory.
    /// Episode availability comes from actual E#M# map markers, not GameMode or a fixed episode count.
    /// </summary>
    public static class EpisodeCatalog
    {
        private const int DoomLineDefSize = 14;
        private const int DoomLineSpecialOffset = 6;

        public static IReadOnlyList<int> GetEpisodes(Wad wad)
        {
            if (wad == null)
                throw new ArgumentNullException(nameof(wad));

            var episodes = new SortedSet<int>();
            var lumps = wad.LumpInfos;

            for (var i = 0; i < lumps.Count; i++)
            {
                if (!TryParseMapName(lumps[i].Name, out var episode, out var map) || map != 1)
                    continue;

                if (IsMapMarkerAt(wad, i))
                    episodes.Add(episode);
            }

            return new List<int>(episodes);
        }

        public static bool HasMap(Wad wad, int episode, int map)
        {
            return FindMapLump(wad, episode, map) != -1;
        }

        public static int FindMapLump(Wad wad, int episode, int map)
        {
            if (wad == null)
                throw new ArgumentNullException(nameof(wad));

            if (episode <= 0 || map <= 0)
                return -1;

            var name = GetMapName(episode, map);
            var lumps = wad.LumpInfos;

            for (var i = lumps.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(lumps[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (IsMapMarkerAt(wad, i))
                    return i;
            }

            return -1;
        }

        public static int FindFirstMap(Wad wad, int episode)
        {
            if (wad == null)
                throw new ArgumentNullException(nameof(wad));

            var best = int.MaxValue;
            var lumps = wad.LumpInfos;

            for (var i = 0; i < lumps.Count; i++)
            {
                if (!TryParseMapName(lumps[i].Name, out var candidateEpisode, out var map) ||
                    candidateEpisode != episode ||
                    !IsMapMarkerAt(wad, i))
                {
                    continue;
                }

                if (map < best)
                    best = map;
            }

            return best == int.MaxValue ? -1 : best;
        }

        public static int FindNextMap(Wad wad, int episode, int currentMap)
        {
            if (wad == null)
                throw new ArgumentNullException(nameof(wad));

            var best = int.MaxValue;
            var lumps = wad.LumpInfos;

            for (var i = 0; i < lumps.Count; i++)
            {
                if (!TryParseMapName(lumps[i].Name, out var candidateEpisode, out var map) ||
                    candidateEpisode != episode ||
                    map <= currentMap ||
                    map == 9 ||
                    !IsMapMarkerAt(wad, i))
                {
                    continue;
                }

                if (map < best)
                    best = map;
            }

            return best == int.MaxValue ? -1 : best;
        }

        /// <summary>
        /// Determines the normal return map after a classic Doom secret map.
        /// The runtime path remembers the exact source map; this method is a save/warp fallback
        /// and discovers the source by looking for vanilla/Boom secret-exit linedefs.
        /// </summary>
        public static int FindSecretReturnMap(Wad wad, int episode)
        {
            if (wad == null)
                throw new ArgumentNullException(nameof(wad));

            var lumps = wad.LumpInfos;
            var sourceMap = -1;

            for (var i = 0; i < lumps.Count; i++)
            {
                if (!TryParseMapName(lumps[i].Name, out var candidateEpisode, out var map) ||
                    candidateEpisode != episode ||
                    map == 9 ||
                    !IsMapMarkerAt(wad, i))
                {
                    continue;
                }

                if (HasClassicSecretExit(wad, i))
                    sourceMap = Math.Max(sourceMap, map);
            }

            if (sourceMap == -1)
                return -1;

            var next = FindNextMap(wad, episode, sourceMap);
            return next != -1 ? next : sourceMap + 1;
        }

        public static string GetMapName(int episode, int map)
        {
            return $"E{episode}M{map}";
        }

        public static bool TryParseMapName(string name, out int episode, out int map)
        {
            episode = 0;
            map = 0;

            if (string.IsNullOrEmpty(name) || char.ToUpperInvariant(name[0]) != 'E')
                return false;

            var separator = name.IndexOf('M', 1);

            if (separator < 0)
                separator = name.IndexOf('m', 1);

            return separator >= 2 &&
                   separator < name.Length - 1 &&
                   int.TryParse(name.AsSpan(1, separator - 1), out episode) &&
                   int.TryParse(name.AsSpan(separator + 1), out map) &&
                   episode > 0 &&
                   map > 0;
        }

        private static bool IsMapMarkerAt(Wad wad, int lump)
        {
            var lumps = wad.LumpInfos;

            if ((uint)lump >= (uint)lumps.Count)
                return false;

            var stream = lumps[lump].Stream;

            if (lump + 1 < lumps.Count &&
                ReferenceEquals(lumps[lump + 1].Stream, stream) &&
                string.Equals(lumps[lump + 1].Name, "TEXTMAP", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (lump + 4 >= lumps.Count)
                return false;

            return ReferenceEquals(lumps[lump + 1].Stream, stream) &&
                   ReferenceEquals(lumps[lump + 2].Stream, stream) &&
                   ReferenceEquals(lumps[lump + 3].Stream, stream) &&
                   ReferenceEquals(lumps[lump + 4].Stream, stream) &&
                   string.Equals(lumps[lump + 1].Name, "THINGS", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(lumps[lump + 2].Name, "LINEDEFS", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(lumps[lump + 3].Name, "SIDEDEFS", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(lumps[lump + 4].Name, "VERTEXES", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasClassicSecretExit(Wad wad, int mapLump)
        {
            // UDMF/Hexen metadata should eventually provide explicit next/secretnext rules.
            if (mapLump + 2 >= wad.LumpInfos.Count ||
                !string.Equals(wad.LumpInfos[mapLump + 2].Name, "LINEDEFS", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var data = wad.ReadLump(mapLump + 2);

            if (data.Length == 0 || data.Length % DoomLineDefSize != 0)
                return false;

            for (var offset = 0; offset < data.Length; offset += DoomLineDefSize)
            {
                var special = BitConverter.ToUInt16(data, offset + DoomLineSpecialOffset);

                // 51 = S1 secret exit, 124 = W1 secret exit.
                if (special == 51 || special == 124)
                    return true;
            }

            return false;
        }
    }
}
