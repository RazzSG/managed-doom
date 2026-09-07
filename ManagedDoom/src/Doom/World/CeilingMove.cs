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
	public sealed class CeilingMove : Thinker
	{
		private World world;

		private CeilingMoveType type;
		private Sector sector;
		private Fixed bottomHeight;
		private Fixed topHeight;
		private Fixed speed;
		private Fixed oldSpeed;
		private bool crush;
		private SectorSpecial newSpecial;
		private int texture;

		// 1 = up, 0 = waiting, -1 = down.
		private int direction;

		// Corresponding sector tag.
		private int tag;

		private int oldDirection;

		public CeilingMove(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			SectorActionResult result;

			var sa = world.SectorAction;

			switch (direction)
			{
				case 0:
					// In statis.
					break;

				case 1:
					// Up.
					result = sa.MovePlane(
						sector,
						speed,
						topHeight,
						false,
						1,
						direction);

					if (((world.LevelTime + sector.Number) & 7) == 0)
					{
						switch (type)
						{
							case CeilingMoveType.SilentCrushAndRaise:
							case CeilingMoveType.GeneralizedSilentCrusher:
								break;

							default:
								world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
								break;
						}
					}

					if (result == SectorActionResult.PastDestination)
					{
						switch (type)
						{
							case CeilingMoveType.RaiseToHighest:
								sa.RemoveActiveCeiling(this);
								sector.DisableFrameInterpolationForOneFrame();
								break;

							case CeilingMoveType.Generalized:
							case CeilingMoveType.GeneralizedChangeTexture:
							case CeilingMoveType.GeneralizedChangeZero:
							case CeilingMoveType.GeneralizedChangeSpecial:
								FinishGeneralized(sa);
								break;

							case CeilingMoveType.GeneralizedCrusher:
							case CeilingMoveType.GeneralizedSilentCrusher:
								direction = -1;
								break;

							case CeilingMoveType.SilentCrushAndRaise:
							case CeilingMoveType.FastCrushAndRaise:
							case CeilingMoveType.CrushAndRaise:
								if (type == CeilingMoveType.SilentCrushAndRaise)
								{
									world.StartSound(sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
								}
								direction = -1;
								break;

							default:
								break;
						}

					}
					break;

				case -1:
					// Down.
					result = sa.MovePlane(
						sector,
						speed,
						bottomHeight,
						crush,
						1,
						direction);

					if (((world.LevelTime + sector.Number) & 7) == 0)
					{
						switch (type)
						{
							case CeilingMoveType.SilentCrushAndRaise:
							case CeilingMoveType.GeneralizedSilentCrusher:
								break;

							default:
								world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
								break;
						}
					}

					if (result == SectorActionResult.PastDestination)
					{
						switch (type)
						{
							case CeilingMoveType.GeneralizedCrusher:
							case CeilingMoveType.GeneralizedSilentCrusher:
								speed = oldSpeed;
								direction = 1;
								break;

							case CeilingMoveType.SilentCrushAndRaise:
							case CeilingMoveType.CrushAndRaise:
							case CeilingMoveType.FastCrushAndRaise:
								if (type == CeilingMoveType.SilentCrushAndRaise)
								{
									world.StartSound(sector.SoundOrigin, Sfx.PSTOP, SfxType.Misc);
									speed = SectorAction.CeilingSpeed;
								}
								if (type == CeilingMoveType.CrushAndRaise)
								{
									speed = SectorAction.CeilingSpeed;
								}
								direction = 1;
								break;

							case CeilingMoveType.LowerAndCrush:
							case CeilingMoveType.LowerToFloor:
								sa.RemoveActiveCeiling(this);
								sector.DisableFrameInterpolationForOneFrame();
								break;

							case CeilingMoveType.Generalized:
							case CeilingMoveType.GeneralizedChangeTexture:
							case CeilingMoveType.GeneralizedChangeZero:
							case CeilingMoveType.GeneralizedChangeSpecial:
								FinishGeneralized(sa);
								break;

							default:
								break;
						}
					}
					else
					{
						if (result == SectorActionResult.Crushed)
						{
							switch (type)
							{
								case CeilingMoveType.SilentCrushAndRaise:
								case CeilingMoveType.CrushAndRaise:
								case CeilingMoveType.LowerAndCrush:
									speed = SectorAction.CeilingSpeed / 8;
									break;

								case CeilingMoveType.GeneralizedCrusher:
								case CeilingMoveType.GeneralizedSilentCrusher:
									if (oldSpeed.Data <= SectorAction.CeilingSpeed.Data * 2)
										speed = oldSpeed / 8;
									break;

								default:
									break;
							}
						}
					}
					break;
			}
		}

		private void FinishGeneralized(SectorAction sectorAction)
		{
			switch (type)
			{
				case CeilingMoveType.GeneralizedChangeTexture:
					sector.CeilingFlat = texture;
					break;

				case CeilingMoveType.GeneralizedChangeZero:
					sector.CeilingFlat = texture;
					sector.Special = SectorSpecial.Normal;
					break;

				case CeilingMoveType.GeneralizedChangeSpecial:
					sector.CeilingFlat = texture;
					sector.Special = newSpecial;
					break;
			}

			sectorAction.RemoveActiveCeiling(this);
			sector.DisableFrameInterpolationForOneFrame();
		}

		public CeilingMoveType Type
		{
			get => type;
			set => type = value;
		}

		public Sector Sector
		{
			get => sector;
			set => sector = value;
		}

		public Fixed BottomHeight
		{
			get => bottomHeight;
			set => bottomHeight = value;
		}

		public Fixed TopHeight
		{
			get => topHeight;
			set => topHeight = value;
		}

		public Fixed Speed
		{
			get => speed;
			set => speed = value;
		}

		public Fixed OldSpeed
		{
			get => oldSpeed;
			set => oldSpeed = value;
		}

		public bool Crush
		{
			get => crush;
			set => crush = value;
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

		public int Direction
		{
			get => direction;
			set => direction = value;
		}

		public int Tag
		{
			get => tag;
			set => tag = value;
		}

		public int OldDirection
		{
			get => oldDirection;
			set => oldDirection = value;
		}
	}
}
