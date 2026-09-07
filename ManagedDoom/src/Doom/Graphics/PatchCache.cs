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

namespace ManagedDoom
{
    public sealed class PatchCache
    {
        private Wad wad;
        private Dictionary<string, Patch> cache;
        private Dictionary<string, bool> availability;

        public PatchCache(Wad wad)
        {
            this.wad = wad;

            cache = new Dictionary<string, Patch>();
            availability = new Dictionary<string, bool>();
        }

        public Patch this[string name]
        {
            get
            {
                Patch patch;
                if (!cache.TryGetValue(name, out patch))
                {
                    patch = Patch.FromWad(wad, name);
                    cache.Add(name, patch);
                }
                return patch;
            }
        }

        public bool Contains(string name)
        {
            if (!availability.TryGetValue(name, out var present))
            {
                present = wad.GetLumpNumber(DoomString.Resolve(name)) != -1;
                availability.Add(name, present);
            }

            return present;
        }

        public int GetWidth(string name)
        {
            return this[name].Width;
        }

        public int GetHeight(string name)
        {
            return this[name].Height;
        }
    }
}
