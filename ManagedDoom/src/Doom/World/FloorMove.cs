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

namespace ManagedDoom
{
	public sealed class FloorMove : Thinker
	{
		private World world;

		private FloorMoveType type;
		private bool crush;
		private Sector sector;
		private int direction;
		private SectorSpecial newSpecial;
		private int texture;
		private Fixed floorDestHeight;
		private Fixed speed;

		public FloorMove(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			SectorActionResult result;

			var sa = world.SectorAction;

			result = sa.MovePlane(
				sector,
				speed,
				floorDestHeight,
				crush,
				0,
				direction);

			if (((world.LevelTime + sector.Number) & 7) == 0)
			{
				world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
			}

			if (result == SectorActionResult.PastDestination)
			{
				sector.FloorData = null;

				switch (type)
				{
					case FloorMoveType.GeneralizedChangeTexture:
						sector.FloorFlat = texture;
						break;

					case FloorMoveType.GeneralizedChangeZero:
						sector.FloorFlat = texture;
						sector.Special = SectorSpecial.Normal;
						break;

					case FloorMoveType.GeneralizedChangeSpecial:
						sector.FloorFlat = texture;
						sector.Special = newSpecial;
						break;
				}

				if (direction == 1)
				{
					switch (type)
					{
						case FloorMoveType.DonutRaise:
							sector.Special = newSpecial;
							sector.FloorFlat = texture;
							break;
					}
				}
				else if (direction == -1)
				{
					switch (type)
					{
						case FloorMoveType.LowerAndChange:
							sector.Special = newSpecial;
							sector.FloorFlat = texture;
							break;
					}
				}

				world.Thinkers.Remove(this);

				if (type == FloorMoveType.GeneralizedStair)
				{
					UnlockGeneralizedStair();
				}

				sector.DisableFrameInterpolationForOneFrame();

				world.StartSound(sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
			}
		}


		private void UnlockGeneralizedStair()
		{
			if (sector.StairLock != -2)
				return;

			var sectors = world.Map.Sectors;
			var current = sector;
			current.StairLock = -1;

			while (current.StairPreviousSector != -1 &&
				sectors[current.StairPreviousSector].StairLock != -2)
			{
				current = sectors[current.StairPreviousSector];
			}

			if (current.StairPreviousSector != -1)
				return;

			current = sector;
			while (current.StairNextSector != -1 &&
				sectors[current.StairNextSector].StairLock != -2)
			{
				current = sectors[current.StairNextSector];
			}

			if (current.StairNextSector != -1)
				return;

			while (current.StairPreviousSector != -1)
			{
				current.StairLock = 0;
				current = sectors[current.StairPreviousSector];
			}

			current.StairLock = 0;
		}

		public FloorMoveType Type
		{
			get => type;
			set => type = value;
		}

		public bool Crush
		{
			get => crush;
			set => crush = value;
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

		public SectorSpecial NewSpecial
		{
			get => newSpecial;
			set => newSpecial = value;
		}

		public int Texture
		{
			get => texture;
			set => texture = value;
		}

		public Fixed FloorDestHeight
		{
			get => floorDestHeight;
			set => floorDestHeight = value;
		}

		public Fixed Speed
		{
			get => speed;
			set => speed = value;
		}
	}
}
