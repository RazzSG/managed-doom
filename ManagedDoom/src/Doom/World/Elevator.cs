//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//

namespace ManagedDoom
{
    public sealed class Elevator : Thinker
    {
        private readonly World world;

        private Sector sector;
        private int direction;
        private Fixed floorDestHeight;
        private Fixed ceilingDestHeight;
        private Fixed speed;

        public Elevator(World world)
        {
            this.world = world;
        }

        public override void Run()
        {
            var sa = world.SectorAction;
            SectorActionResult result;

            if (direction < 0)
            {
                result = sa.MovePlane(sector, speed, ceilingDestHeight, false, 1, direction);
                if (result == SectorActionResult.OK || result == SectorActionResult.PastDestination)
                    sa.MovePlane(sector, speed, floorDestHeight, false, 0, direction);
            }
            else
            {
                result = sa.MovePlane(sector, speed, floorDestHeight, false, 0, direction);
                if (result == SectorActionResult.OK || result == SectorActionResult.PastDestination)
                    sa.MovePlane(sector, speed, ceilingDestHeight, false, 1, direction);
            }

            if ((world.LevelTime & 7) == 0)
                world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);

            if (result != SectorActionResult.PastDestination)
                return;

            sector.FloorData = null;
            sector.CeilingData = null;
            world.Thinkers.Remove(this);
            sector.DisableFrameInterpolationForOneFrame();
            world.StartSound(sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
        }

        public Sector Sector
        {
            get => sector;
            set => sector = value;
        }

        public int Direction
        {
            get => direction;
            set => direction = value;
        }

        public Fixed FloorDestHeight
        {
            get => floorDestHeight;
            set => floorDestHeight = value;
        }

        public Fixed CeilingDestHeight
        {
            get => ceilingDestHeight;
            set => ceilingDestHeight = value;
        }

        public Fixed Speed
        {
            get => speed;
            set => speed = value;
        }
    }
}
