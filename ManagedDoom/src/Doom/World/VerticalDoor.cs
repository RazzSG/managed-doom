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
using ManagedDoom.Compatibility.Boom.Doors;
using ManagedDoom.Compatibility.Mbf.Doors;

namespace ManagedDoom
{
	public class VerticalDoor : Thinker
	{
		private World world;

		private VerticalDoorType type;
		private Sector sector;
		private Fixed topHeight;
		private Fixed speed;

		// Boom: manual doors may use the activating line tag for synchronized lighting.
		private LineDef lightLine;
		private int lightTag;

		// 1 = up, 0 = waiting at top, -1 = down.
		private int direction;

		// Tics to wait at the top.
		private int topWait;

		// When it reaches 0, start going down
		// (keep in case a door going down is reset).
		private int topCountDown;

		public VerticalDoor(World world)
		{
			this.world = world;
		}

		public override void Run()
		{
			var sa = world.SectorAction;

			SectorActionResult result;

			switch (direction)
			{
				case 0:
					// Waiting.
					if (--topCountDown == 0)
					{
						switch (type)
						{
							case VerticalDoorType.BlazeRaise:
								// Time to go back down.
								direction = -1;
								world.StartSound(sector.SoundOrigin, Sfx.BDCLS, SfxType.Misc);
								break;

							case VerticalDoorType.Normal:
								// Time to go back down.
								direction = -1;
								world.StartSound(sector.SoundOrigin, Sfx.DORCLS, SfxType.Misc);
								break;

							case VerticalDoorType.GeneralizedRaise:
								direction = -1;
								StartGeneralizedCloseSound();
								break;

							case VerticalDoorType.Close30ThenOpen:
								direction = 1;
								world.StartSound(sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);
								break;

							case VerticalDoorType.GeneralizedCloseThenOpen:
								direction = 1;
								StartGeneralizedOpenSound();
								break;

							default:
								break;
						}
					}
					break;

				case 2:
					// Initial wait.
					if (--topCountDown == 0)
					{
						switch (type)
						{
							case VerticalDoorType.RaiseIn5Mins:
								direction = 1;
								type = VerticalDoorType.Normal;
								world.StartSound(sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);
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
						sector.FloorHeight,
						false, 1, direction);

					UpdateGradualDoorLighting(sa);

					if (result == SectorActionResult.PastDestination)
					{
						switch (type)
						{
							case VerticalDoorType.BlazeRaise:
							case VerticalDoorType.BlazeClose:
								sector.CeilingData = null;
								// Unlink and free.
								world.Thinkers.Remove(this);
								sector.DisableFrameInterpolationForOneFrame();
								if (!UsesFixedBlazingDoorSounds)
								{
									world.StartSound(sector.SoundOrigin, Sfx.BDCLS, SfxType.Misc);
								}
								break;

							case VerticalDoorType.Normal:
							case VerticalDoorType.Close:
							case VerticalDoorType.GeneralizedRaise:
							case VerticalDoorType.GeneralizedClose:
								sector.CeilingData = null;
								// Unlink and free.
								world.Thinkers.Remove(this);
								sector.DisableFrameInterpolationForOneFrame();
								if ((type == VerticalDoorType.GeneralizedRaise ||
									type == VerticalDoorType.GeneralizedClose) &&
									speed >= Fixed.FromInt(8) &&
									!UsesFixedBlazingDoorSounds)
								{
									world.StartSound(sector.SoundOrigin, Sfx.BDCLS, SfxType.Misc);
								}
								break;

							case VerticalDoorType.Close30ThenOpen:
								direction = 0;
								topCountDown = 35 * 30;
								break;

							case VerticalDoorType.GeneralizedCloseThenOpen:
								direction = 0;
								topCountDown = topWait;
								break;

							default:
								break;
						}

						UpdateBoomDoorLightingAtEndpoint(sa, Fixed.Zero);
					}
					else if (result == SectorActionResult.Crushed)
					{
						switch (type)
						{
							case VerticalDoorType.BlazeClose:
							case VerticalDoorType.Close:
							case VerticalDoorType.GeneralizedClose: // Do not go back up!
								break;

							case VerticalDoorType.BlazeRaise:
								direction = 1;
								world.StartSound(
									sector.SoundOrigin,
									UsesFixedBlazingDoorSounds ? Sfx.BDOPN : Sfx.DOROPN,
									SfxType.Misc);
								break;

							case VerticalDoorType.GeneralizedRaise:
								direction = 1;
								if (speed >= Fixed.FromInt(8) && !UsesFixedBlazingDoorSounds)
								{
									world.StartSound(sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);
								}
								else
								{
									StartGeneralizedOpenSound();
								}
								break;

							case VerticalDoorType.GeneralizedCloseThenOpen:
								direction = 1;
								StartGeneralizedOpenSound();
								break;

							default:
								direction = 1;
								world.StartSound(sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);
								break;
						}
					}
					break;

				case 1:
					// Up.
					result = sa.MovePlane(
						sector,
						speed,
						topHeight,
						false, 1, direction);

					UpdateGradualDoorLighting(sa);

					if (result == SectorActionResult.PastDestination)
					{
						switch (type)
						{
							case VerticalDoorType.BlazeRaise:
							case VerticalDoorType.Normal:
							case VerticalDoorType.GeneralizedRaise:
								// Wait at top.
								direction = 0;
								topCountDown = topWait;
								break;

							case VerticalDoorType.Close30ThenOpen:
							case VerticalDoorType.BlazeOpen:
							case VerticalDoorType.Open:
							case VerticalDoorType.GeneralizedOpen:
							case VerticalDoorType.GeneralizedCloseThenOpen:
								sector.CeilingData = null;
								// Unlink and free.
								world.Thinkers.Remove(this);
								sector.DisableFrameInterpolationForOneFrame();
								break;

							default:
								break;
						}

						UpdateBoomDoorLightingAtEndpoint(sa, Fixed.One);
					}
					break;
			}
		}

		private bool UsesFixedBlazingDoorSounds =>
			MbfBlazingDoorCompatibility.UsesFixedBlazingDoorSounds(
				world.Options.Compatibility,
				world.Options.MbfOptions.CompBlazing);

		private bool HasDoorLighting =>
			lightTag != 0 &&
			lightLine != null &&
			topHeight != sector.FloorHeight;

		private void UpdateGradualDoorLighting(SectorAction sectorAction)
		{
			if (!HasDoorLighting ||
				!MbfDoorLightingCompatibility.UsesGradualDoorLighting(
					world.Options.Compatibility,
					world.Options.MbfOptions.CompDoorLight))
			{
				return;
			}

			var level = (sector.CeilingHeight - sector.FloorHeight) /
				(topHeight - sector.FloorHeight);
			sectorAction.LightTurnOnPartway(lightLine, level);
		}

		private void UpdateBoomDoorLightingAtEndpoint(SectorAction sectorAction, Fixed level)
		{
			var compatibility = world.Options.Compatibility;
			if (!HasDoorLighting ||
				!MbfDoorLightingCompatibility.UsesTaggedManualDoorLighting(
					compatibility, world.Options.MbfOptions.CompDoorLight) ||
				MbfDoorLightingCompatibility.UsesGradualDoorLighting(
					compatibility, world.Options.MbfOptions.CompDoorLight))
			{
				return;
			}

			sectorAction.LightTurnOnPartway(lightLine, level);
		}

		private void StartGeneralizedOpenSound()
		{
			world.StartSound(sector.SoundOrigin, speed >= Fixed.FromInt(8) ? Sfx.BDOPN : Sfx.DOROPN, SfxType.Misc);
		}

		private void StartGeneralizedCloseSound()
		{
			world.StartSound(sector.SoundOrigin, speed >= Fixed.FromInt(8) ? Sfx.BDCLS : Sfx.DORCLS, SfxType.Misc);
		}

		public VerticalDoorType Type
		{
			get => type;
			set => type = value;
		}

		public Sector Sector
		{
			get => sector;
			set => sector = value;
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

		public LineDef LightLine
		{
			get => lightLine;
			set => lightLine = value;
		}

		public int LightTag
		{
			get => lightTag;
			set => lightTag = value;
		}

		public int Direction
		{
			get => direction;
			set => direction = value;
		}

		public int TopWait
		{
			get => topWait;
			set => topWait = value;
		}

		public int TopCountDown
		{
			get => topCountDown;
			set => topCountDown = value;
		}
	}
}
