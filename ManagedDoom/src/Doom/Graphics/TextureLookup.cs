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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;

namespace ManagedDoom
{
    public sealed class TextureLookup : ITextureLookup
    {
        private List<Texture> textures;
        private Dictionary<string, Texture> nameToTexture;
        private Dictionary<string, int> nameToNumber;

        private int[] switchList;

        public TextureLookup(Wad wad) : this(wad, false)
        {
        }

        public TextureLookup(Wad wad, bool useDummy)
        {
            try
            {
                Console.Write("Load textures: ");

                InitLookup(wad);
                InitSwitchList();

                Console.WriteLine("OK (" + textures.Count + " textures)");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private void InitLookup(Wad wad)
        {
            textures = new List<Texture>();
            nameToTexture = new Dictionary<string, Texture>();
            nameToNumber = new Dictionary<string, int>();

            var patches = LoadPatches(wad);

            for (var n = 1; n <= 2; n++)
            {
                var lumpNumber = wad.GetLumpNumber(DoomString.Resolve("TEXTURE" + n));
                if (lumpNumber == -1)
                {
                    break;
                }

                var data = wad.ReadLump(lumpNumber);
                var count = BitConverter.ToInt32(data, 0);
                for (var i = 0; i < count; i++)
                {
                    var offset = BitConverter.ToInt32(data, 4 + 4 * i);
                    var texture = Texture.FromData(data, offset, patches);
                    nameToNumber.TryAdd(texture.Name, textures.Count);
                    textures.Add(texture);
                    nameToTexture.TryAdd(texture.Name, texture);
                }
            }
        }

        private void InitSwitchList()
        {
            var list = new List<int>();
            foreach (var tuple in DoomInfo.SwitchNames)
            {
                var texNum1 = GetNumber(tuple.Item1);
                var texNum2 = GetNumber(tuple.Item2);
                if (texNum1 != -1 && texNum2 != -1)
                {
                    list.Add(texNum1);
                    list.Add(texNum2);
                }
            }
            switchList = list.ToArray();
        }

        public int GetNumber(string name)
        {
            if (name[0] == '-')
            {
                return 0;
            }

            return nameToNumber.TryGetValue(name, out var number) ? number : -1;
        }

        private static Patch[] LoadPatches(Wad wad)
        {
            var patchNames = LoadPatchNames(wad);
            var patches = new Patch[patchNames.Length];
            for (var i = 0; i < patches.Length; i++)
            {
                var name = patchNames[i];
                patches[i] = FindAndLoadPatch(wad, name);
            }

            return patches;
        }

        private static Patch FindAndLoadPatch(Wad wad, string name)
        {
            var lumps = wad.LumpInfos;
            Exception lastPatchError = null;
            var foundSameName = false;

            // Preserve vanilla Doom's lookup order: search the complete lump directory
            // backwards. The important difference is that a same-name flat/sprite/etc.
            // is ignored if it is not actually a valid Doom patch. This fixes modern
            // WAD collisions such as a flat named BODIES without breaking Final Doom,
            // whose PNAMES lookup relies on the original global-name behavior.
            for (var i = lumps.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(lumps[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                foundSameName = true;

                var data = wad.ReadLump(i);

                if (!LooksLikeDoomPatchHeader(data))
                    continue;

                try
                {
                    return Patch.FromData(name, data);
                }
                catch (Exception e)
                {
                    // There may be an earlier lump with the same name that is the real
                    // texture patch. Remember the parse error in case none is usable.
                    lastPatchError = new InvalidDataException($"Invalid texture patch candidate '{name}' at lump {i}, size={data.Length}.", e);
                }
            }

            // Missing PNAMES entries can legitimately be unused in old IWADs, so keep
            // the original ManagedDoom behavior and leave those slots null.
            // If a texture actually references a missing slot, Texture.FromData will
            // expose it; a malformed patch candidate, however, is worth reporting.
            if (lastPatchError != null)
                throw lastPatchError;

            if (foundSameName)
            {
                // Same-name lumps existed, but none were Doom patches. Treat the PNAMES
                // entry as unresolved rather than accidentally parsing a flat/sprite.
                return null;
            }

            return null;
        }

        private static bool LooksLikeDoomPatchHeader(byte[] data)
        {
            if (data == null || data.Length < 8)
                return false;

            var width = BitConverter.ToInt16(data, 0);
            var height = BitConverter.ToInt16(data, 2);

            if (width <= 0 || height <= 0)
                return false;

            var tableEnd = 8L + 4L * width;
            if (tableEnd > data.Length)
                return false;

            for (var x = 0; x < width; x++)
            {
                var p = BitConverter.ToInt32(data, 8 + 4 * x);

                if (p < tableEnd || p >= data.Length)
                    return false;
            }

            return true;
        }

        private static string[] LoadPatchNames(Wad wad)
        {
            var data = wad.ReadLump(DoomString.Resolve("PNAMES"));
            var count = BitConverter.ToInt32(data, 0);
            var names = new string[count];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = DoomInterop.ToString(data, 4 + 8 * i, 8);
            }
            return names;
        }

        public IEnumerator<Texture> GetEnumerator()
        {
            return textures.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return textures.GetEnumerator();
        }

        public int Count => textures.Count;
        public Texture this[int num] => textures[num];
        public Texture this[string name] => nameToTexture[name];
        public int[] SwitchList => switchList;
    }
}
