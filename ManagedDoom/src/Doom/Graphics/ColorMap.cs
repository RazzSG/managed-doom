//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace ManagedDoom
{
    public sealed class ColorMap
    {
        public static readonly int Inverse = 32;

        private byte[][] data;
        private Dictionary<string, byte[][]> namedSets;

        public ColorMap(Wad wad)
        {
            try
            {
                Console.Write("Load color map: ");

                data = ParseSet(wad.ReadLump(DoomString.Resolve("COLORMAP")));
                namedSets = new Dictionary<string, byte[][]>(StringComparer.OrdinalIgnoreCase);

                LoadBoomColorMaps(wad);

                Console.WriteLine(namedSets.Count == 0
                    ? "OK"
                    : $"OK (+{namedSets.Count} Boom colormap{(namedSets.Count == 1 ? "" : "s")})");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private static byte[][] ParseSet(byte[] raw)
        {
            var count = raw.Length / 256;
            var result = new byte[count][];

            for (var i = 0; i < count; i++)
            {
                result[i] = new byte[256];
                Buffer.BlockCopy(raw, 256 * i, result[i], 0, 256);
            }

            return result;
        }

        private void LoadBoomColorMaps(Wad wad)
        {
            var inColorMapSection = false;
            var lumps = wad.LumpInfos;

            // Scan each WAD's C_START/C_END namespace in final load order. Reset at
            // file boundaries so a malformed unterminated namespace cannot leak into
            // the next PWAD; later valid definitions still override earlier ones.
            var activeStream = lumps.Count == 0 ? null : lumps[0].Stream;

            for (var i = 0; i < lumps.Count; i++)
            {
                if (!ReferenceEquals(activeStream, lumps[i].Stream))
                {
                    activeStream = lumps[i].Stream;
                    inColorMapSection = false;
                }

                var name = lumps[i].Name;

                if (string.Equals(name, "C_START", StringComparison.OrdinalIgnoreCase))
                {
                    inColorMapSection = true;
                    continue;
                }

                if (string.Equals(name, "C_END", StringComparison.OrdinalIgnoreCase))
                {
                    inColorMapSection = false;
                    continue;
                }

                if (inColorMapSection)
                {
                    TryAddNamedSet(wad, i, name);
                }
            }

            // Boom ships WATERMAP as a predefined colormap. A PWAD may supply or
            // replace it without placing it in a C_START/C_END namespace. Search
            // from the end so the last loaded WAD keeps normal override priority.
            for (var i = lumps.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(lumps[i].Name, "WATERMAP", StringComparison.OrdinalIgnoreCase))
                    continue;

                TryAddNamedSet(wad, i, "WATERMAP");
                break;
            }
        }

        private void TryAddNamedSet(Wad wad, int lumpNumber, string name)
        {
            if (string.IsNullOrEmpty(name) ||
                string.Equals(name, "COLORMAP", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var expectedSize = data.Length * 256;
            if (wad.GetLumpSize(lumpNumber) != expectedSize)
            {
                return;
            }

            namedSets[name] = ParseSet(wad.ReadLump(lumpNumber));
        }

        public byte[] this[int index] => data[index];

        public byte[] FullBright => data[0];

        public int Count => data.Length;

        public byte[][] DefaultSet => data;

        public IReadOnlyDictionary<string, byte[][]> NamedSets => namedSets;

        public bool TryGetSet(string name, out byte[][] set)
        {
            if (string.Equals(name, "COLORMAP", StringComparison.OrdinalIgnoreCase))
            {
                set = data;
                return true;
            }

            return namedSets.TryGetValue(name ?? string.Empty, out set);
        }
    }
}
