//
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//

namespace ManagedDoom.DefinitionPatches.DeHackEd
{
    /// <summary>
    /// Safe representation of classic DeHackEd Sound block fields.
    /// Nullable values mean that the active patch set did not override the
    /// corresponding field.
    ///
    /// Pointer-like DOS fields are retained only as numeric metadata and are
    /// never interpreted as managed object references.
    /// </summary>
    public sealed class DeHackEdSoundInfo
    {
        public int? Singularity { get; internal set; }
        public int? Priority { get; internal set; }
        public int? LegacyLink { get; internal set; }
        public int? Pitch { get; internal set; }
        public int? Volume { get; internal set; }
        public int? Usefulness { get; internal set; }
        public int? LumpNum { get; internal set; }

        internal void Reset()
        {
            Singularity = null;
            Priority = null;
            LegacyLink = null;
            Pitch = null;
            Volume = null;
            Usefulness = null;
            LumpNum = null;
        }
    }
}
