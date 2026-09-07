//
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//

using ManagedDoom.DefinitionPatches.DeHackEd;

namespace ManagedDoom
{
    public static partial class DoomInfo
    {
        /// <summary>
        /// Compatibility view of DeHackEd Sound metadata.
        ///
        /// This is a property rather than a static field on DoomInfo so it
        /// cannot participate in DoomInfo's partial-type initialization order.
        /// </summary>
        public static DeHackEdSoundInfo[] DeHackEdSoundInfos => DeHackEdSoundTable.Entries;

        internal static void ResetDeHackEdSoundInfos()
        {
            DeHackEdSoundTable.Reset();
        }
    }
}
