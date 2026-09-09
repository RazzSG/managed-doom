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
using ManagedDoom.Compatibility.Boom.Sectors;

namespace ManagedDoom
{
    public sealed class Sector
    {
        private static readonly int dataSize = 26;

        private int number;
        private Fixed floorHeight;
        private Fixed ceilingHeight;
        private int floorFlat;
        private int ceilingFlat;
        private int lightLevel;
        private SectorSpecial special;
        private int tag;

        // 0 = untraversed, 1, 2 = sndlines - 1.
        private int soundTraversed;

        // Thing that made a sound (or null).
        private Mobj soundTarget;

        // Mapblock bounding box for height changes.
        private int[] blockBox;

        // Origin for any sounds played by the sector.
        private Mobj soundOrigin;

        // If == validcount, already checked.
        private int validCount;

        // List of mobjs whose origins are in this sector.
        private Mobj thingList;

        // Boom msecnode-style list of mobjs whose radius touches this sector.
        private BoomSectorTouchNode touchingThingList;

        // Plane movers. Boom allows floor and ceiling actions to run independently
        // in the same sector (for example PassThru multiple-switch actions).
        private Thinker floorData;
        private Thinker ceilingData;

        // Boom keeps lighting actions independent from floor/ceiling movers.
        private Thinker lightingData;

        // Boom property transfers can light floor and ceiling planes from independent control sectors.
        private Sector floorLightSector;
        private Sector ceilingLightSector;

        // Boom linedef 242 can draw this sector using heights from a control sector.
        // Keep the defining line as well; later fake-colormap support needs its sidedef textures.
        private Sector heightSector;
        private LineDef heightSectorLine;

        // MBF 271/272 sky transfer. Keep the defining line live rather than copying
        // its texture/offsets so Boom wall scrollers automatically animate the sky.
        private LineDef skyTransferLine;

        // Boom flat scrollers keep their accumulated texture-space displacement on the sector.
        private Fixed floorXOffset;
        private Fixed floorYOffset;
        private Fixed ceilingXOffset;
        private Fixed ceilingYOffset;

        // Boom variable friction is resolved once at map startup.
        private Fixed friction = new Fixed(0xe800);
        private Fixed moveFactor = new Fixed(2048);

        // Boom constant wind vectors are resolved once at map startup.
        // Ground values are stored separately because Boom halves integer magnitudes before
        // converting them back to fixed-point momentum.
        private Fixed windAboveX;
        private Fixed windAboveY;
        private Fixed windGroundX;
        private Fixed windGroundY;

        // Boom constant current vector is resolved once at map startup.
        private Fixed currentX;
        private Fixed currentY;

        // Boom generalized stair retrigger lockout.
        // -2 = this step is still moving, -1 = this step finished but the chain is locked, 0 = unlocked.
        private int stairLock;
        private int stairPreviousSector;
        private int stairNextSector;

        private LineDef[] lines;

        // For frame interpolation.
        private Fixed oldFloorHeight;
        private Fixed oldCeilingHeight;

        public Sector(
            int number,
            Fixed floorHeight,
            Fixed ceilingHeight,
            int floorFlat,
            int ceilingFlat,
            int lightLevel,
            SectorSpecial special,
            int tag)
        {
            this.number = number;
            this.floorHeight = floorHeight;
            this.ceilingHeight = ceilingHeight;
            this.floorFlat = floorFlat;
            this.ceilingFlat = ceilingFlat;
            this.lightLevel = lightLevel;
            this.special = special;
            this.tag = tag;

            oldFloorHeight = floorHeight;
            oldCeilingHeight = ceilingHeight;

            stairPreviousSector = -1;
            stairNextSector = -1;
        }

        public static Sector FromData(byte[] data, int offset, int number, IFlatLookup flats)
        {
            var floorHeight = BitConverter.ToInt16(data, offset);
            var ceilingHeight = BitConverter.ToInt16(data, offset + 2);
            var floorFlatName = DoomInterop.ToString(data, offset + 4, 8);
            var ceilingFlatName = DoomInterop.ToString(data, offset + 12, 8);
            var lightLevel = BitConverter.ToInt16(data, offset + 20);
            var special = BitConverter.ToInt16(data, offset + 22);
            var tag = BitConverter.ToInt16(data, offset + 24);

            return new Sector(
                number,
                Fixed.FromInt(floorHeight),
                Fixed.FromInt(ceilingHeight),
                flats.GetNumber(floorFlatName),
                flats.GetNumber(ceilingFlatName),
                lightLevel,
                (SectorSpecial)special,
                tag);
        }

        public static Sector[] FromWad(Wad wad, int lump, IFlatLookup flats)
        {
            var length = wad.GetLumpSize(lump);
            if (length % dataSize != 0)
            {
                throw new Exception();
            }

            var data = wad.ReadLump(lump);
            var count = length / dataSize;
            var sectors = new Sector[count]; ;

            for (var i = 0; i < count; i++)
            {
                var offset = dataSize * i;
                sectors[i] = FromData(data, offset, i, flats);
            }

            return sectors;
        }

        public void UpdateFrameInterpolationInfo()
        {
            oldFloorHeight = floorHeight;
            oldCeilingHeight = ceilingHeight;
        }

        public Fixed GetInterpolatedFloorHeight(Fixed frameFrac)
        {
            return oldFloorHeight + frameFrac * (floorHeight - oldFloorHeight);
        }

        public Fixed GetInterpolatedCeilingHeight(Fixed frameFrac)
        {
            return oldCeilingHeight + frameFrac * (ceilingHeight - oldCeilingHeight);
        }

        public void DisableFrameInterpolationForOneFrame()
        {
            oldFloorHeight = floorHeight;
            oldCeilingHeight = ceilingHeight;
        }

        public ThingEnumerator GetEnumerator()
        {
            return new ThingEnumerator(this);
        }



        public struct ThingEnumerator : IEnumerator<Mobj>
        {
            private Sector sector;
            private Mobj thing;
            private Mobj current;

            public ThingEnumerator(Sector sector)
            {
                this.sector = sector;
                thing = sector.thingList;
                current = null;
            }

            public bool MoveNext()
            {
                if (thing != null)
                {
                    current = thing;
                    thing = thing.SectorNext;
                    return true;
                }
                else
                {
                    current = null;
                    return false;
                }
            }

            public void Reset()
            {
                thing = sector.thingList;
                current = null;
            }

            public void Dispose()
            {
            }

            public Mobj Current => current;

            object IEnumerator.Current => throw new NotImplementedException();
        }

        public int Number => number;

        public Fixed FloorHeight
        {
            get => floorHeight;
            set => floorHeight = value;
        }

        public Fixed CeilingHeight
        {
            get => ceilingHeight;
            set => ceilingHeight = value;
        }

        public int FloorFlat
        {
            get => floorFlat;
            set => floorFlat = value;
        }

        public int CeilingFlat
        {
            get => ceilingFlat;
            set => ceilingFlat = value;
        }

        public int LightLevel
        {
            get => lightLevel;
            set => lightLevel = value;
        }

        public Sector FloorLightSector
        {
            get => floorLightSector;
            set => floorLightSector = value;
        }

        public Sector CeilingLightSector
        {
            get => ceilingLightSector;
            set => ceilingLightSector = value;
        }

        public int FloorLightLevel => floorLightSector?.LightLevel ?? lightLevel;

        public int CeilingLightLevel => ceilingLightSector?.LightLevel ?? lightLevel;

        public Sector HeightSector
        {
            get => heightSector;
            set => heightSector = value;
        }

        public LineDef HeightSectorLine
        {
            get => heightSectorLine;
            set => heightSectorLine = value;
        }

        public LineDef SkyTransferLine
        {
            get => skyTransferLine;
            set => skyTransferLine = value;
        }

        public Fixed FloorXOffset
        {
            get => floorXOffset;
            set => floorXOffset = value;
        }

        public Fixed FloorYOffset
        {
            get => floorYOffset;
            set => floorYOffset = value;
        }

        public Fixed CeilingXOffset
        {
            get => ceilingXOffset;
            set => ceilingXOffset = value;
        }

        public Fixed CeilingYOffset
        {
            get => ceilingYOffset;
            set => ceilingYOffset = value;
        }

        public Fixed Friction
        {
            get => friction;
            set => friction = value;
        }

        public Fixed MoveFactor
        {
            get => moveFactor;
            set => moveFactor = value;
        }

        public Fixed WindAboveX
        {
            get => windAboveX;
            set => windAboveX = value;
        }

        public Fixed WindAboveY
        {
            get => windAboveY;
            set => windAboveY = value;
        }

        public Fixed WindGroundX
        {
            get => windGroundX;
            set => windGroundX = value;
        }

        public Fixed WindGroundY
        {
            get => windGroundY;
            set => windGroundY = value;
        }

        public Fixed CurrentX
        {
            get => currentX;
            set => currentX = value;
        }

        public Fixed CurrentY
        {
            get => currentY;
            set => currentY = value;
        }

        public SectorSpecial Special
        {
            get => special;
            set => special = value;
        }

        public int Tag
        {
            get => tag;
            set => tag = value;
        }

        public int SoundTraversed
        {
            get => soundTraversed;
            set => soundTraversed = value;
        }

        public Mobj SoundTarget
        {
            get => soundTarget;
            set => soundTarget = value;
        }

        public int[] BlockBox
        {
            get => blockBox;
            set => blockBox = value;
        }

        public Mobj SoundOrigin
        {
            get => soundOrigin;
            set => soundOrigin = value;
        }

        public int ValidCount
        {
            get => validCount;
            set => validCount = value;
        }

        public Mobj ThingList
        {
            get => thingList;
            set => thingList = value;
        }

        public BoomSectorTouchNode TouchingThingList
        {
            get => touchingThingList;
            set => touchingThingList = value;
        }

        /// <summary>
        /// Legacy single-special facade. New plane-mover code should use FloorData
        /// or CeilingData explicitly. Keeping this property preserves compatibility
        /// with older callers and tests that expect one reversible-action slot.
        /// </summary>
        public Thinker SpecialData
        {
            get => floorData ?? ceilingData;
            set
            {
                // A write through the legacy facade retains the old replacement
                // semantics. Explicit FloorData/CeilingData writes are what allow
                // Boom to keep both movers alive simultaneously.
                floorData = null;
                ceilingData = null;

                if (value == null)
                {
                    return;
                }

                if (value is Elevator)
                {
                    floorData = value;
                    ceilingData = value;
                }
                else if (value is FloorMove || value is Platform)
                {
                    floorData = value;
                }
                else if (value is CeilingMove || value is VerticalDoor)
                {
                    ceilingData = value;
                }
                else
                {
                    // Unknown legacy blockers historically occupied the only
                    // action slot, so conservatively block both planes.
                    floorData = value;
                    ceilingData = value;
                }
            }
        }

        public Thinker FloorData
        {
            get => floorData;
            set => floorData = value;
        }

        public Thinker CeilingData
        {
            get => ceilingData;
            set => ceilingData = value;
        }

        public Thinker LightingData
        {
            get => lightingData;
            set => lightingData = value;
        }

        public int StairLock
        {
            get => stairLock;
            set => stairLock = value;
        }

        public int StairPreviousSector
        {
            get => stairPreviousSector;
            set => stairPreviousSector = value;
        }

        public int StairNextSector
        {
            get => stairNextSector;
            set => stairNextSector = value;
        }

        public LineDef[] Lines
        {
            get => lines;
            set => lines = value;
        }
    }
}
