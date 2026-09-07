//
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//

using System;

using ManagedDoom;

namespace ManagedDoom.DefinitionPatches.DeHackEd
{
    /// <summary>
    /// Process-global DeHackEd sound metadata for the classic Doom sound table.
    ///
    /// This is intentionally a separate type from DoomInfo. Its size is based
    /// on the Sfx enum, not DoomInfo.SfxNames, so static initialization never
    /// depends on the ordering of partial DoomInfo source files.
    /// </summary>
    internal static class DeHackEdSoundTable
    {
        private static readonly DeHackEdSoundInfo[] entries = CreateEntries();

        public static DeHackEdSoundInfo[] Entries => entries;

        public static void Reset()
        {
            for (var i = 0; i < entries.Length; i++)
            {
                entries[i].Reset();
            }
        }

        private static DeHackEdSoundInfo[] CreateEntries()
        {
            var result = new DeHackEdSoundInfo[Enum.GetValues<Sfx>().Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new DeHackEdSoundInfo();
            }

            return result;
        }
    }
}
