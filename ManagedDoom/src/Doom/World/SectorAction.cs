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
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Doors;
using ManagedDoom.Compatibility.Boom.Lines;
using ManagedDoom.Compatibility.Boom.Movement;
using ManagedDoom.Compatibility.Boom.Sectors;

namespace ManagedDoom
{
	public sealed class SectorAction
	{
		//
		// SECTOR HEIGHT CHANGING
		// After modifying a sectors floor or ceiling height,
		// call this routine to adjust the positions
		// of all things that touch the sector.
		//
		// If anything doesn't fit anymore, true will be returned.
		// If crunch is true, they will take damage
		// as they are being crushed.
		// If Crunch is false, you should set the sector height back
		// the way it was and call P_ChangeSector again
		// to undo the changes.
		//

		private World world;

		public SectorAction(World world)
		{
			this.world = world;

			InitSectorChange();
		}

		private bool UsesIndependentPlaneMovers =>
			GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility);

		private bool IsFloorBusy(Sector sector)
		{
			return sector.FloorData != null ||
				(!UsesIndependentPlaneMovers && sector.CeilingData != null);
		}

		private bool IsCeilingBusy(Sector sector)
		{
			return sector.CeilingData != null ||
				(!UsesIndependentPlaneMovers && sector.FloorData != null);
		}

		private static bool IsEitherPlaneBusy(Sector sector)
		{
			return sector.FloorData != null || sector.CeilingData != null;
		}



		private bool crushChange;
		private bool noFit;
		private Func<Mobj, bool> crushThingFunc;

		private void InitSectorChange()
		{
			crushThingFunc = CrushThing;
		}

		private bool ThingHeightClip(Mobj thing)
		{
			var onFloor = (thing.Z == thing.FloorZ);

			var tm = world.ThingMovement;

			tm.CheckPosition(thing, thing.X, thing.Y);
			// What about stranding a monster partially off an edge?

			thing.FloorZ = tm.CurrentFloorZ;
			thing.CeilingZ = tm.CurrentCeilingZ;

			if (onFloor)
			{
				// Walking monsters rise and fall with the floor.
				thing.Z = thing.FloorZ;
			}
			else
			{
				// Don't adjust a floating monster unless forced to.
				if (thing.Z + thing.Height > thing.CeilingZ)
				{
					thing.Z = thing.CeilingZ - thing.Height;
				}
			}

			if (thing.CeilingZ - thing.FloorZ < thing.Height)
			{
				return false;
			}

			return true;
		}

		private bool CrushThing(Mobj thing)
		{
			if (ThingHeightClip(thing))
			{
				// Keep checking.
				return true;
			}

			// Crunch bodies to giblets.
			if (thing.Health <= 0)
			{
				thing.SetState(MobjState.Gibs);
				thing.Flags &= ~MobjFlags.Solid;
				thing.Height = Fixed.Zero;
				thing.Radius = Fixed.Zero;

				// Keep checking.
				return true;
			}

			// Crunch dropped items.
			if ((thing.Flags & MobjFlags.Dropped) != 0)
			{
				world.ThingAllocation.RemoveMobj(thing);

				// Keep checking.
				return true;
			}

			if ((thing.Flags & MobjFlags.Shootable) == 0)
			{
				// Assume it is bloody gibs or something.
				return true;
			}

			noFit = true;

			if (crushChange && (world.LevelTime & 3) == 0)
			{
				world.ThingInteraction.DamageMobj(thing, null, null, 10);

				// Spray blood in a random direction.
				var blood = world.ThingAllocation.SpawnMobj(
					thing.X,
					thing.Y,
					thing.Z + thing.Height / 2,
					MobjType.Blood);

				var random = world.Random;
				blood.MomX = new Fixed((random.Next() - random.Next()) << 12);
				blood.MomY = new Fixed((random.Next() - random.Next()) << 12);
			}

			// Keep checking (crush other things).	
			return true;
		}

		private bool ChangeSector(Sector sector, bool crunch)
		{
			noFit = false;
			crushChange = crunch;

			if (GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility))
			{
				// Boom P_CheckSector examines only mobjs whose radius actually touches
				// the moving sector. The touching list is maintained by SetThingPosition.
				world.ThingMovement.VisitThingsTouchingSector(sector, crushThingFunc);
				return noFit;
			}

			var bm = world.Map.BlockMap;
			var blockBox = sector.BlockBox;

			// Vanilla P_ChangeSector re-checks every thing in the sector blockbox.
			for (var x = blockBox.Left(); x <= blockBox.Right(); x++)
			{
				for (var y = blockBox.Bottom(); y <= blockBox.Top(); y++)
				{
					bm.IterateThings(x, y, crushThingFunc);
				}
			}

			return noFit;
		}

		/// <summary>
		/// Move a plane (floor or ceiling) and check for crushing.
		/// </summary>
		public SectorActionResult MovePlane(
			Sector sector,
			Fixed speed,
			Fixed dest,
			bool crush,
			int floorOrCeiling,
			int direction)
		{
			switch (floorOrCeiling)
			{
				case 0:
					// Floor.
					switch (direction)
					{
						case -1:
							// Down.
							if (sector.FloorHeight - speed < dest)
							{
								var lastPos = sector.FloorHeight;
								sector.FloorHeight = dest;
								if (ChangeSector(sector, crush))
								{
									sector.FloorHeight = lastPos;
									ChangeSector(sector, crush);
								}

								return SectorActionResult.PastDestination;
							}
							else
							{
								var lastPos = sector.FloorHeight;
								sector.FloorHeight -= speed;
								if (ChangeSector(sector, crush) &&
									BoomSectorMovementQuirks.ShouldRestoreIntermediateLoweringFloorAfterNoFit(
										world.Options.Compatibility))
								{
									sector.FloorHeight = lastPos;
									ChangeSector(sector, crush);

									return SectorActionResult.Crushed;
								}
							}

							break;

						case 1:
							// Up. Boom keeps a rising floor from passing through the
							// current ceiling; vanilla Doom keeps the original behavior.
							var floorDestination = GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility) &&
								dest > sector.CeilingHeight
								? sector.CeilingHeight
								: dest;

							if (sector.FloorHeight + speed > floorDestination)
							{
								var lastPos = sector.FloorHeight;
								sector.FloorHeight = floorDestination;
								if (ChangeSector(sector, crush))
								{
									sector.FloorHeight = lastPos;
									ChangeSector(sector, crush);
								}

								return SectorActionResult.PastDestination;
							}
							else
							{
								// Could get crushed.
								var lastPos = sector.FloorHeight;
								sector.FloorHeight += speed;
								if (ChangeSector(sector, crush))
								{
									if (!BoomSectorMovementQuirks.ShouldRestoreIntermediateRaisingFloorAfterNoFit(
										world.Options.Compatibility,
										crush))
									{
										return SectorActionResult.Crushed;
									}

									sector.FloorHeight = lastPos;
									ChangeSector(sector, crush);

									return SectorActionResult.Crushed;
								}
							}

							break;
					}
					break;

				case 1:
					// Ceiling.
					switch (direction)
					{
						case -1:
							// Down. Boom keeps a lowering ceiling from passing through the
							// current floor; vanilla Doom keeps the original behavior.
							var ceilingDestination = GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility) &&
								dest < sector.FloorHeight
								? sector.FloorHeight
								: dest;

							if (sector.CeilingHeight - speed < ceilingDestination)
							{
								var lastPos = sector.CeilingHeight;
								sector.CeilingHeight = ceilingDestination;
								if (ChangeSector(sector, crush))
								{
									sector.CeilingHeight = lastPos;
									ChangeSector(sector, crush);
								}

								return SectorActionResult.PastDestination;
							}
							else
							{
								// Could get crushed.
								var lastPos = sector.CeilingHeight;
								sector.CeilingHeight -= speed;
								if (ChangeSector(sector, crush))
								{
									if (crush)
									{
										return SectorActionResult.Crushed;
									}
									sector.CeilingHeight = lastPos;
									ChangeSector(sector, crush);

									return SectorActionResult.Crushed;
								}
							}

							break;

						case 1:
							// UP
							if (sector.CeilingHeight + speed > dest)
							{
								var lastPos = sector.CeilingHeight;
								sector.CeilingHeight = dest;
								if (ChangeSector(sector, crush))
								{
									sector.CeilingHeight = lastPos;
									ChangeSector(sector, crush);
								}

								return SectorActionResult.PastDestination;
							}
							else
							{
								sector.CeilingHeight += speed;
								ChangeSector(sector, crush);
							}

							break;
					}

					break;
			}

			return SectorActionResult.OK;
		}

		private Sector GetNextSector(LineDef line, Sector sector)
		{
			return BoomSectorModelCompatibility.GetNextSector(
				line, sector, world.Options.Compatibility);
		}

		private Fixed FindLowestFloorSurrounding(Sector sector)
		{
			var floor = sector.FloorHeight;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var check = sector.Lines[i];

				var other = GetNextSector(check, sector);
				if (other == null)
				{
					continue;
				}

				if (other.FloorHeight < floor)
				{
					floor = other.FloorHeight;
				}
			}

			return floor;
		}

		private Fixed FindHighestFloorSurrounding(Sector sector)
		{
			var floor = BoomSectorModelCompatibility.HighestFloorInitial(world.Options.Compatibility);

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var check = sector.Lines[i];

				var other = GetNextSector(check, sector);
				if (other == null)
				{
					continue;
				}

				if (other.FloorHeight > floor)
				{
					floor = other.FloorHeight;
				}
			}

			return floor;
		}

		private Fixed FindLowestCeilingSurrounding(Sector sector)
		{
			var height = BoomSectorModelCompatibility.LowestCeilingInitial(world.Options.Compatibility);

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var check = sector.Lines[i];

				var other = GetNextSector(check, sector);
				if (other == null)
				{
					continue;
				}

				if (other.CeilingHeight < height)
				{
					height = other.CeilingHeight;
				}
			}

			return height;
		}

		private Fixed FindHighestCeilingSurrounding(Sector sector)
		{
			var height = BoomSectorModelCompatibility.HighestCeilingInitial(world.Options.Compatibility);

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var check = sector.Lines[i];

				var other = GetNextSector(check, sector);
				if (other == null)
				{
					continue;
				}

				if (other.CeilingHeight > height)
				{
					height = other.CeilingHeight;
				}
			}

			return height;
		}

		private int FindSectorFromLineTag(LineDef line, int start)
		{
			return world.Map.BoomTags.FindNextSectorNumber(line.Tag, start);
		}



		////////////////////////////////////////////////////////////
		// Door
		////////////////////////////////////////////////////////////

		private static readonly Fixed doorSpeed = Fixed.FromInt(2);
		private static readonly int doorWait = 150;

		/// <summary>
		/// Open a door manually, no tag value.
		/// </summary>
		public void DoLocalDoor(LineDef line, Mobj thing)
		{
			//	Check for locks.
			var player = thing.Player;

			switch ((int)line.Special)
			{
				// Blue Lock.
				case 26:
				case 32:
					if (player == null)
					{
						return;
					}

					if (!player.Cards[(int)CardType.BlueCard] &&
						!player.Cards[(int)CardType.BlueSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_BLUEK);
						world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);
						return;
					}
					break;

				// Yellow Lock.
				case 27:
				case 34:
					if (player == null)
					{
						return;
					}

					if (!player.Cards[(int)CardType.YellowCard] &&
						!player.Cards[(int)CardType.YellowSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_YELLOWK);
						world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);
						return;
					}
					break;

				// Red Lock.
				case 28:
				case 33:
					if (player == null)
					{
						return;
					}

					if (!player.Cards[(int)CardType.RedCard] &&
						!player.Cards[(int)CardType.RedSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_REDK);
						world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);
						return;
					}
					break;
			}

			var sector = line.BackSide.Sector;

			// If the ceiling already has a door, use it. Other ceiling movers
			// block the door; in Vanilla an active floor mover blocks it too.
			if (IsCeilingBusy(sector))
			{
				var door = sector.CeilingData as VerticalDoor;
				if (door == null)
					return;
				switch ((int)line.Special)
				{
					// Only for "raise" doors, not "open"s.
					case 1:
					case 26:
					case 27:
					case 28:
					case 117:
						if (door.Direction == -1)
						{
							// Go back up.
							door.Direction = 1;
						}
						else
						{
							if (thing.Player == null)
							{
								// Bad guys never close doors.
								return;
							}

							// Start going down immediately.
							door.Direction = -1;
						}
						return;
				}
			}

			// For proper sound.
			switch ((int)line.Special)
			{
				// Blazing door raise.
				case 117:

				// Blazing door open.
				case 118:
					world.StartSound(sector.SoundOrigin, Sfx.BDOPN, SfxType.Misc);
					break;

				// Normal door sound.
				case 1:
				case 31:
					world.StartSound(sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);
					break;

				// Locked door sound.
				default:
					world.StartSound(sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);
					break;
			}

			// New door thinker.
			var newDoor = new VerticalDoor(world);
			world.Thinkers.Add(newDoor);
			sector.CeilingData = newDoor;
			newDoor.Sector = sector;
			newDoor.Direction = 1;
			newDoor.Speed = doorSpeed;
			newDoor.TopWait = doorWait;
			newDoor.LightLine = line;
			newDoor.LightTag = BoomDoorCompatibility.UsesTaggedManualDoorLighting(world.Options.Compatibility)
				? line.Tag
				: 0;

			switch ((int)line.Special)
			{
				case 1:
				case 26:
				case 27:
				case 28:
					newDoor.Type = VerticalDoorType.Normal;
					break;

				case 31:
				case 32:
				case 33:
				case 34:
					newDoor.Type = VerticalDoorType.Open;
					line.Special = 0;
					break;

				// Blazing door raise.
				case 117:
					newDoor.Type = VerticalDoorType.BlazeRaise;
					newDoor.Speed = doorSpeed * 4;
					break;

				// Blazing door open.
				case 118:
					newDoor.Type = VerticalDoorType.BlazeOpen;
					line.Special = 0;
					newDoor.Speed = doorSpeed * 4;
					break;
			}

			// Find the top and bottom of the movement range.
			newDoor.TopHeight = FindLowestCeilingSurrounding(sector);
			newDoor.TopHeight -= Fixed.FromInt(4);
		}


		public bool DoBoomLockedDoor(LineDef line, BoomLockedDoorSpecial specification)
		{
			var kind = specification.Kind == BoomLockedDoorKind.OpenStay
				? BoomDoorKind.OpenStay
				: BoomDoorKind.OpenWaitClose;

			var door = new BoomDoorSpecial(specification.Common, kind, doorWait);
			return DoBoomDoor(line, door);
		}


		public bool DoBoomDoor(LineDef line, BoomDoorSpecial specification)
		{
			if (!specification.UsesTagForTargeting)
			{
				var sector = line.BackSector;
				return sector != null && !IsCeilingBusy(sector) && StartBoomDoor(sector, line, specification);
			}

			var result = false;
			var sectors = world.Map.BoomTags.GetSectors(line.Tag);

			for (var i = 0; i < sectors.Length; i++)
			{
				var sector = sectors[i];
				if (IsCeilingBusy(sector))
					continue;

				if (StartBoomDoor(sector, line, specification))
					result = true;
			}

			return result;
		}

		private bool StartBoomDoor(Sector sector, LineDef line, BoomDoorSpecial specification)
		{
			var door = new VerticalDoor(world)
			{
				Sector = sector,
				Speed = doorSpeed * (1 << (int)specification.Speed),
				TopWait = specification.WaitTics,
				LightLine = line,
				LightTag = BoomDoorCompatibility.GetGeneralizedDoorLightTag(
					world.Options.Compatibility, specification.Trigger, line.Tag)
			};

			switch (specification.Kind)
			{
				case BoomDoorKind.OpenWaitClose:
					door.Type = VerticalDoorType.GeneralizedRaise;
					door.Direction = 1;
					door.TopHeight = FindLowestCeilingSurrounding(sector) - Fixed.FromInt(4);
					StartBoomDoorSound(sector, door.Speed, true);
					break;

				case BoomDoorKind.OpenStay:
					door.Type = VerticalDoorType.GeneralizedOpen;
					door.Direction = 1;
					door.TopHeight = FindLowestCeilingSurrounding(sector) - Fixed.FromInt(4);
					StartBoomDoorSound(sector, door.Speed, true);
					break;

				case BoomDoorKind.CloseWaitOpen:
					door.Type = VerticalDoorType.GeneralizedCloseThenOpen;
					door.Direction = -1;
					door.TopHeight = sector.CeilingHeight;
					StartBoomDoorSound(sector, door.Speed, false);
					break;

				case BoomDoorKind.CloseStay:
					door.Type = VerticalDoorType.GeneralizedClose;
					door.Direction = -1;
					door.TopHeight = FindLowestCeilingSurrounding(sector) - Fixed.FromInt(4);
					StartBoomDoorSound(sector, door.Speed, false);
					break;

				default:
					return false;
			}

			world.Thinkers.Add(door);
			sector.CeilingData = door;
			return true;
		}

		private void StartBoomDoorSound(Sector sector, Fixed speed, bool opening)
		{
			var blazing = speed >= doorSpeed * 4;
			var sound = opening
				? (blazing ? Sfx.BDOPN : Sfx.DOROPN)
				: (blazing ? Sfx.BDCLS : Sfx.DORCLS);
			world.StartSound(sector.SoundOrigin, sound, SfxType.Misc);
		}

		public bool DoDoor(LineDef line, VerticalDoorType type)
		{
			var sectors = world.Map.Sectors;
			var setcorNumber = -1;
			var result = false;

			while ((setcorNumber = FindSectorFromLineTag(line, setcorNumber)) >= 0)
			{
				var sector = sectors[setcorNumber];
				if (IsCeilingBusy(sector))
				{
					continue;
				}

				result = true;

				// New door thinker.
				var door = new VerticalDoor(world);
				world.Thinkers.Add(door);
				sector.CeilingData = door;
				door.Sector = sector;
				door.Type = type;
				door.TopWait = doorWait;
				door.Speed = doorSpeed;

				switch (type)
				{
					case VerticalDoorType.BlazeClose:
						door.TopHeight = FindLowestCeilingSurrounding(sector);
						door.TopHeight -= Fixed.FromInt(4);
						door.Direction = -1;
						door.Speed = doorSpeed * 4;
						world.StartSound(door.Sector.SoundOrigin, Sfx.BDCLS, SfxType.Misc);
						break;

					case VerticalDoorType.Close:
						door.TopHeight = FindLowestCeilingSurrounding(sector);
						door.TopHeight -= Fixed.FromInt(4);
						door.Direction = -1;
						world.StartSound(door.Sector.SoundOrigin, Sfx.DORCLS, SfxType.Misc);
						break;

					case VerticalDoorType.Close30ThenOpen:
						door.TopHeight = sector.CeilingHeight;
						door.Direction = -1;
						world.StartSound(door.Sector.SoundOrigin, Sfx.DORCLS, SfxType.Misc);
						break;

					case VerticalDoorType.BlazeRaise:
					case VerticalDoorType.BlazeOpen:
						door.Direction = 1;
						door.TopHeight = FindLowestCeilingSurrounding(sector);
						door.TopHeight -= Fixed.FromInt(4);
						door.Speed = doorSpeed * 4;
						if (door.TopHeight != sector.CeilingHeight)
						{
							world.StartSound(door.Sector.SoundOrigin, Sfx.BDOPN, SfxType.Misc);
						}
						break;

					case VerticalDoorType.Normal:
					case VerticalDoorType.Open:
						door.Direction = 1;
						door.TopHeight = FindLowestCeilingSurrounding(sector);
						door.TopHeight -= Fixed.FromInt(4);
						if (door.TopHeight != sector.CeilingHeight)
						{
							world.StartSound(door.Sector.SoundOrigin, Sfx.DOROPN, SfxType.Misc);
						}
						break;

					default:
						break;
				}

			}

			return result;
		}

		public bool DoLockedDoor(LineDef line, VerticalDoorType type, Mobj thing)
		{
			var player = thing.Player;
			if (player == null)
			{
				return false;
			}

			switch ((int)line.Special)
			{
				// Blue Lock.
				case 99:
				case 133:
					if (player == null)
					{
						return false;
					}
					if (!player.Cards[(int)CardType.BlueCard] &&
						!player.Cards[(int)CardType.BlueSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_BLUEO);
						world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);
						return false;
					}
					break;

				// Red Lock.
				case 134:
				case 135:
					if (player == null)
					{
						return false;
					}
					if (!player.Cards[(int)CardType.RedCard] &&
						!player.Cards[(int)CardType.RedSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_REDO);
						world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);
						return false;
					}
					break;

				// Yellow Lock.
				case 136:
				case 137:
					if (player == null)
					{
						return false;
					}
					if (!player.Cards[(int)CardType.YellowCard] &&
						!player.Cards[(int)CardType.YellowSkull])
					{
						player.SendMessage(DoomInfo.Strings.PD_YELLOWO);
						world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);
						return false;
					}
					break;
			}

			return DoDoor(line, type);
		}



		////////////////////////////////////////////////////////////
		// Boom elevator
		////////////////////////////////////////////////////////////

		private static readonly Fixed elevatorSpeed = Fixed.FromInt(4);

		public bool DoBoomElevator(LineDef line, BoomElevatorSpecial specification)
		{
			if (line.Tag == 0)
				return false;

			var result = false;
			var sectors = world.Map.BoomTags.GetSectors(line.Tag);

			for (var i = 0; i < sectors.Length; i++)
			{
				var sector = sectors[i];
				if (IsEitherPlaneBusy(sector))
					continue;

				var floorDestination = specification.Target switch
				{
					BoomElevatorTarget.Up => FindBoomNextFloor(sector, BoomPlaneDirection.Up),
					BoomElevatorTarget.Down => FindBoomNextFloor(sector, BoomPlaneDirection.Down),
					BoomElevatorTarget.Current => line.FrontSector.FloorHeight,
					_ => sector.FloorHeight
				};

				var elevator = new Elevator(world)
				{
					Sector = sector,
					Direction = floorDestination > sector.FloorHeight ? 1 : -1,
					FloorDestHeight = floorDestination,
					CeilingDestHeight = floorDestination + sector.CeilingHeight - sector.FloorHeight,
					Speed = elevatorSpeed
				};

				world.Thinkers.Add(elevator);
				sector.FloorData = elevator;
				sector.CeilingData = elevator;
				result = true;
			}

			return result;
		}


		////////////////////////////////////////////////////////////
		// Platform
		////////////////////////////////////////////////////////////

		// In plutonia MAP23, number of adjoining sectors can be 44.
		private static readonly int maxAdjoiningSectorCount = 64;
		private Fixed[] heightList = new Fixed[maxAdjoiningSectorCount];

		private Fixed FindNextHighestFloor(Sector sector, Fixed currentHeight)
		{
			if (BoomSectorModelCompatibility.UsesFixedModelSemantics(world.Options.Compatibility))
			{
				var result = currentHeight;
				var found = false;

				for (var i = 0; i < sector.Lines.Length; i++)
				{
					var other = GetNextSector(sector.Lines[i], sector);
					if (other == null || other.FloorHeight <= currentHeight)
						continue;

					if (!found || other.FloorHeight < result)
					{
						result = other.FloorHeight;
						found = true;
					}
				}

				return result;
			}

			var height = currentHeight;
			var h = 0;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var check = sector.Lines[i];

				var other = GetNextSector(check, sector);
				if (other == null)
				{
					continue;
				}

				if (other.FloorHeight > height)
				{
					heightList[h++] = other.FloorHeight;
				}

				// Check for overflow.
				if (h >= heightList.Length)
				{
					// Exit.
					throw new Exception("Too many adjoining sectors!");
				}
			}

			// Find lowest height in list.
			if (h == 0)
			{
				return currentHeight;
			}

			var min = heightList[0];

			// Range checking? 
			for (var i = 1; i < h; i++)
			{
				if (heightList[i] < min)
				{
					min = heightList[i];
				}
			}

			return min;
		}


		private static readonly int platformWait = 3;
		private static readonly Fixed platformSpeed = Fixed.One;

		public bool DoPlatform(LineDef line, PlatformType type, int amount)
		{
			//	Activate all <type> plats that are in stasis.
			switch (type)
			{
				case PlatformType.PerpetualRaise:
					ActivateInStasis(line.Tag);
					break;

				default:
					break;
			}

			var sectors = world.Map.Sectors;
			var sectorNumber = -1;
			var result = false;

			while ((sectorNumber = FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var sector = sectors[sectorNumber];
				if (IsFloorBusy(sector))
				{
					continue;
				}

				result = true;

				// Find lowest and highest floors around sector.
				var plat = new Platform(world);
				world.Thinkers.Add(plat);
				plat.Type = type;
				plat.Sector = sector;
				plat.Sector.FloorData = plat;
				plat.Crush = false;
				plat.Tag = line.Tag;

				switch (type)
				{
					case PlatformType.RaiseToNearestAndChange:
						plat.Speed = platformSpeed / 2;
						sector.FloorFlat = line.FrontSide.Sector.FloorFlat;
						plat.High = FindNextHighestFloor(sector, sector.FloorHeight);
						plat.Wait = 0;
						plat.Status = PlatformState.Up;
						// No more damage, if applicable.
						sector.Special = 0;
						world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
						break;

					case PlatformType.RaiseAndChange:
						plat.Speed = platformSpeed / 2;
						sector.FloorFlat = line.FrontSide.Sector.FloorFlat;
						plat.High = sector.FloorHeight + amount * Fixed.One;
						plat.Wait = 0;
						plat.Status = PlatformState.Up;
						world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
						break;

					case PlatformType.DownWaitUpStay:
						plat.Speed = platformSpeed * 4;
						plat.Low = FindLowestFloorSurrounding(sector);
						if (plat.Low > sector.FloorHeight)
						{
							plat.Low = sector.FloorHeight;
						}
						plat.High = sector.FloorHeight;
						plat.Wait = 35 * platformWait;
						plat.Status = PlatformState.Down;
						world.StartSound(sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);
						break;

					case PlatformType.BlazeDwus:
						plat.Speed = platformSpeed * 8;
						plat.Low = FindLowestFloorSurrounding(sector);
						if (plat.Low > sector.FloorHeight)
						{
							plat.Low = sector.FloorHeight;
						}
						plat.High = sector.FloorHeight;
						plat.Wait = 35 * platformWait;
						plat.Status = PlatformState.Down;
						world.StartSound(sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);
						break;

					case PlatformType.PerpetualRaise:
						plat.Speed = platformSpeed;
						plat.Low = FindLowestFloorSurrounding(sector);
						if (plat.Low > sector.FloorHeight)
						{
							plat.Low = sector.FloorHeight;
						}
						plat.High = FindHighestFloorSurrounding(sector);
						if (plat.High < sector.FloorHeight)
						{
							plat.High = sector.FloorHeight;
						}
						plat.Wait = 35 * platformWait;
						plat.Status = (PlatformState)(world.Random.Next() & 1);
						world.StartSound(sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);
						break;
				}

				AddActivePlatform(plat);
			}

			return result;
		}


		public bool DoBoomPlatform(LineDef line, BoomPlatformSpecial specification)
		{
			if (specification.Action == BoomPlatformAction.Stop)
			{
				StopPlatform(line);
				return true;
			}

			var result = specification.Action == BoomPlatformAction.Toggle;

			if (specification.Action == BoomPlatformAction.Perpetual ||
				specification.Action == BoomPlatformAction.Toggle)
			{
				ActivateInStasis(line.Tag);
			}

			var sectors = world.Map.BoomTags.GetSectors(line.Tag);
			for (var i = 0; i < sectors.Length; i++)
			{
				var sector = sectors[i];
				if (IsFloorBusy(sector))
					continue;

				var plat = new Platform(world)
				{
					Sector = sector,
					Crush = false,
					Tag = line.Tag,
					Low = sector.FloorHeight
				};

				switch (specification.Action)
				{
					case BoomPlatformAction.RaiseAndChange:
						plat.Type = PlatformType.RaiseAndChange;
						plat.Speed = platformSpeed / 2;
						plat.High = sector.FloorHeight + specification.Amount * Fixed.One;
						plat.Wait = 0;
						plat.Status = PlatformState.Up;
						sector.FloorFlat = line.FrontSide.Sector.FloorFlat;
						world.StartSound(sector.SoundOrigin, Sfx.STNMOV, SfxType.Misc);
						break;

					case BoomPlatformAction.Perpetual:
						plat.Type = PlatformType.PerpetualRaise;
						plat.Speed = platformSpeed;
						plat.Low = FindLowestFloorSurrounding(sector);
						if (plat.Low > sector.FloorHeight)
							plat.Low = sector.FloorHeight;
						plat.High = FindHighestFloorSurrounding(sector);
						if (plat.High < sector.FloorHeight)
							plat.High = sector.FloorHeight;
						plat.Wait = 35 * platformWait;
						plat.Status = (PlatformState)(world.Random.Next() & 1);
						world.StartSound(sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);
						break;

					case BoomPlatformAction.Toggle:
						plat.Type = PlatformType.ToggleUpDown;
						plat.Speed = platformSpeed;
						plat.Low = sector.CeilingHeight;
						plat.High = sector.FloorHeight;
						plat.Wait = 35 * platformWait;
						plat.Crush = true;
						plat.Status = PlatformState.Down;
						break;

					default:
						continue;
				}

				world.Thinkers.Add(plat);
				sector.FloorData = plat;
				AddActivePlatform(plat);
				result = true;
			}

			return result;
		}


		public bool DoBoomLift(LineDef line, BoomLiftSpecial specification)
		{
			if (specification.IsPerpetual)
			{
				ActivateInStasis(line.Tag);
			}

			if (!specification.UsesTagForTargeting)
			{
				var sector = line.BackSector;
				return sector != null && !IsFloorBusy(sector) && StartBoomLift(line, sector, specification);
			}

			var result = false;
			var sectors = world.Map.BoomTags.GetSectors(line.Tag);

			for (var i = 0; i < sectors.Length; i++)
			{
				var sector = sectors[i];
				if (IsFloorBusy(sector))
					continue;

				if (StartBoomLift(line, sector, specification))
					result = true;
			}

			return result;
		}

		private bool StartBoomLift(LineDef line, Sector sector, BoomLiftSpecial specification)
		{
			var plat = new Platform(world)
			{
				Type = PlatformType.GeneralizedLift,
				Sector = sector,
				Crush = false,
				Tag = line.Tag,
				High = sector.FloorHeight,
				Status = PlatformState.Down,
				Speed = platformSpeed * (2 << (int)specification.Speed),
				Wait = specification.WaitTics
			};

			switch (specification.Target)
			{
				case BoomLiftTarget.LowestNeighborFloor:
					plat.Low = FindLowestFloorSurrounding(sector);
					if (plat.Low > sector.FloorHeight)
						plat.Low = sector.FloorHeight;
					break;

				case BoomLiftTarget.NextLowestNeighborFloor:
					plat.Low = FindBoomNextFloor(sector, BoomPlaneDirection.Down);
					break;

				case BoomLiftTarget.LowestNeighborCeiling:
					plat.Low = FindLowestCeilingSurrounding(sector);
					if (plat.Low > sector.FloorHeight)
						plat.Low = sector.FloorHeight;
					break;

				case BoomLiftTarget.LowestHighestFloorPerpetual:
					plat.Type = PlatformType.GeneralizedPerpetual;
					plat.Low = FindLowestFloorSurrounding(sector);
					if (plat.Low > sector.FloorHeight)
						plat.Low = sector.FloorHeight;

					plat.High = FindBoomHighestFloorSurrounding(sector);
					if (plat.High < sector.FloorHeight)
						plat.High = sector.FloorHeight;

					plat.Status = (PlatformState)(world.Random.Next() & 1);
					break;
			}

			world.Thinkers.Add(plat);
			sector.FloorData = plat;
			world.StartSound(sector.SoundOrigin, Sfx.PSTART, SfxType.Misc);
			AddActivePlatform(plat);
			return true;
		}


		private readonly List<Platform> activePlatforms = new List<Platform>();

		public void ActivateInStasis(int tag)
		{
			for (var i = 0; i < activePlatforms.Count; i++)
			{
				var platform = activePlatforms[i];

				if (platform.Tag == tag &&
					platform.Status == PlatformState.InStasis)
				{
					if (platform.Type == PlatformType.ToggleUpDown)
					{
						platform.Status = platform.OldStatus == PlatformState.Up
							? PlatformState.Down
							: PlatformState.Up;
					}
					else
					{
						platform.Status = platform.OldStatus;
					}

					platform.ThinkerState = ThinkerState.Active;
				}
			}
		}

		public void StopPlatform(LineDef line)
		{
			for (var i = 0; i < activePlatforms.Count; i++)
			{
				var platform = activePlatforms[i];

				if (platform.Status != PlatformState.InStasis &&
					platform.Tag == line.Tag)
				{
					platform.OldStatus = platform.Status;
					platform.Status = PlatformState.InStasis;
					platform.ThinkerState = ThinkerState.InStasis;
				}
			}
		}

		public void AddActivePlatform(Platform platform)
		{
			activePlatforms.Add(platform);
		}

		public void RemoveActivePlatform(Platform platform)
		{
			var index = activePlatforms.IndexOf(platform);

			if (index == -1)
				throw new Exception("The platform was not found!");

			platform.Sector.FloorData = null;
			world.Thinkers.Remove(platform);
			activePlatforms.RemoveAt(index);
		}



		////////////////////////////////////////////////////////////
		// Floor
		////////////////////////////////////////////////////////////

		private static readonly Fixed floorSpeed = Fixed.One;
		private static readonly Fixed boomMinPlaneHeight = Fixed.FromInt(-32000);
		private static readonly Fixed boomMaxPlaneHeight = Fixed.FromInt(32000);

		public bool DoBoomFloor(LineDef line, BoomFloorSpecial specification)
		{
			if (specification.Target == BoomFloorTarget.None)
				return DoBoomFloorChange(line, specification);

			if (!specification.UsesTagForTargeting)
			{
				var sector = line.BackSector;
				return sector != null && !IsFloorBusy(sector) && StartBoomFloor(line, sector, specification);
			}

			var result = false;
			var sectors = world.Map.BoomTags.GetSectors(line.Tag);

			for (var i = 0; i < sectors.Length; i++)
			{
				var sector = sectors[i];
				if (IsFloorBusy(sector))
					continue;

				if (StartBoomFloor(line, sector, specification))
					result = true;
			}

			return result;
		}

		private bool DoBoomFloorChange(LineDef line, BoomFloorSpecial specification)
		{
			var result = false;
			var sectors = world.Map.BoomTags.GetSectors(line.Tag);

			for (var i = 0; i < sectors.Length; i++)
			{
				var sector = sectors[i];
				var model = specification.Model == BoomModelType.Trigger
					? line.FrontSector
					: FindBoomModelFloorSector(sector, sector.FloorHeight);

				if (model == null)
					continue;

				sector.FloorFlat = model.FloorFlat;
				sector.Special = model.Special;
				result = true;
			}

			return result;
		}

		private bool StartBoomFloor(LineDef line, Sector sector, BoomFloorSpecial specification)
		{
			var floor = new FloorMove(world)
			{
				Type = FloorMoveType.Generalized,
				Crush = specification.Crush,
				Sector = sector,
				Direction = (int)specification.Direction,
				Speed = floorSpeed * (1 << (int)specification.Speed),
				FloorDestHeight = FindBoomFloorDestination(sector, specification),
				Texture = sector.FloorFlat,
				NewSpecial = sector.Special
			};

			if (specification.Change != BoomChangeType.None)
			{
				var model = specification.Model == BoomModelType.Trigger
					? line.FrontSector
					: FindBoomModelFloorSector(sector, floor.FloorDestHeight);

				if (model != null)
				{
					floor.Texture = model.FloorFlat;
					floor.NewSpecial = model.Special;
					floor.Type = specification.Change switch
					{
						BoomChangeType.TextureAndZeroSpecial => FloorMoveType.GeneralizedChangeZero,
						BoomChangeType.TextureOnly => FloorMoveType.GeneralizedChangeTexture,
						BoomChangeType.TextureAndSpecial => FloorMoveType.GeneralizedChangeSpecial,
						_ => FloorMoveType.Generalized
					};
				}
			}

			world.Thinkers.Add(floor);
			sector.FloorData = floor;
			return true;
		}

		private Fixed FindBoomFloorDestination(Sector sector, BoomFloorSpecial specification)
		{
			return specification.Target switch
			{
				BoomFloorTarget.HighestNeighborFloor => FindBoomHighestFloorSurrounding(sector),
				BoomFloorTarget.LowestNeighborFloor => FindLowestFloorSurrounding(sector),
				BoomFloorTarget.NextNeighborFloor => FindBoomNextFloor(sector, specification.Direction),
				BoomFloorTarget.LowestNeighborCeiling => FindBoomLowestCeilingSurrounding(sector),
				BoomFloorTarget.Ceiling => sector.CeilingHeight,
				BoomFloorTarget.ShortestLowerTexture => FindBoomShortestLowerTextureDestination(sector, specification.Direction),
				BoomFloorTarget.By24 => sector.FloorHeight + (int)specification.Direction * Fixed.FromInt(24),
				BoomFloorTarget.By32 => sector.FloorHeight + (int)specification.Direction * Fixed.FromInt(32),
				BoomFloorTarget.By512 => sector.FloorHeight + (int)specification.Direction * Fixed.FromInt(512),
				_ => sector.FloorHeight
			};
		}

		private Fixed FindBoomHighestFloorSurrounding(Sector sector)
		{
			var height = boomMinPlaneHeight;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var other = GetNextSector(sector.Lines[i], sector);
				if (other != null && other.FloorHeight > height)
					height = other.FloorHeight;
			}

			return height;
		}

		private Fixed FindBoomLowestCeilingSurrounding(Sector sector)
		{
			var height = sector.CeilingHeight;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var other = GetNextSector(sector.Lines[i], sector);
				if (other != null && other.CeilingHeight < height)
					height = other.CeilingHeight;
			}

			return height;
		}

		private Fixed FindBoomNextFloor(Sector sector, BoomPlaneDirection direction)
		{
			var current = sector.FloorHeight;
			var result = current;
			var found = false;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var other = GetNextSector(sector.Lines[i], sector);
				if (other == null)
					continue;

				var height = other.FloorHeight;
				if (direction == BoomPlaneDirection.Up)
				{
					if (height > current && (!found || height < result))
					{
						result = height;
						found = true;
					}
				}
				else if (height < current && (!found || height > result))
				{
					result = height;
					found = true;
				}
			}

			return result;
		}

		private Fixed FindBoomShortestLowerTextureDestination(Sector sector, BoomPlaneDirection direction)
		{
			var shortest = 32000;
			var textures = world.Map.Textures;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var line = sector.Lines[i];
				if (!BoomSectorModelCompatibility.IsTwoSided(line, world.Options.Compatibility))
					continue;

				if (BoomSectorModelCompatibility.IsUsableShortestTexture(line.FrontSide.BottomTexture, world.Options.Compatibility))
					shortest = Math.Min(shortest, textures[line.FrontSide.BottomTexture].Height);

				if (line.BackSide != null &&
					BoomSectorModelCompatibility.IsUsableShortestTexture(line.BackSide.BottomTexture, world.Options.Compatibility))
					shortest = Math.Min(shortest, textures[line.BackSide.BottomTexture].Height);
			}

			if (shortest == 32000)
				return direction == BoomPlaneDirection.Up ? boomMaxPlaneHeight : boomMinPlaneHeight;

			var delta = Fixed.FromInt(shortest);
			if (direction == BoomPlaneDirection.Up)
				return sector.FloorHeight > boomMaxPlaneHeight - delta ? boomMaxPlaneHeight : sector.FloorHeight + delta;

			return sector.FloorHeight < boomMinPlaneHeight + delta ? boomMinPlaneHeight : sector.FloorHeight - delta;
		}

		private Sector FindBoomModelFloorSector(Sector sector, Fixed destination)
		{
			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var other = GetNextSector(sector.Lines[i], sector);
				if (other != null && other.FloorHeight == destination)
					return other;
			}

			return null;
		}

		public bool DoFloor(LineDef line, FloorMoveType type)
		{
			var sectors = world.Map.Sectors;
			var sectorNumber = -1;
			var result = false;

			while ((sectorNumber = FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var sector = sectors[sectorNumber];

				// Already moving? If so, keep going...
				if (IsFloorBusy(sector))
				{
					continue;
				}

				result = true;

				// New floor thinker.
				var floor = new FloorMove(world);
				world.Thinkers.Add(floor);
				sector.FloorData = floor;
				floor.Type = type;
				floor.Crush = false;

				switch (type)
				{
					case FloorMoveType.LowerFloor:
						floor.Direction = -1;
						floor.Sector = sector;
						floor.Speed = floorSpeed;
						floor.FloorDestHeight = FindHighestFloorSurrounding(sector);
						break;

					case FloorMoveType.LowerFloorToLowest:
						floor.Direction = -1;
						floor.Sector = sector;
						floor.Speed = floorSpeed;
						floor.FloorDestHeight = FindLowestFloorSurrounding(sector);
						break;

					case FloorMoveType.TurboLower:
						floor.Direction = -1;
						floor.Sector = sector;
						floor.Speed = floorSpeed * 4;
						floor.FloorDestHeight = FindHighestFloorSurrounding(sector);
						if (floor.FloorDestHeight != sector.FloorHeight)
						{
							floor.FloorDestHeight += Fixed.FromInt(8);
						}
						break;

					case FloorMoveType.RaiseFloorCrush:
					case FloorMoveType.RaiseFloor:
						if (type == FloorMoveType.RaiseFloorCrush)
						{
							floor.Crush = true;
						}
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = floorSpeed;
						floor.FloorDestHeight = FindLowestCeilingSurrounding(sector);
						if (floor.FloorDestHeight > sector.CeilingHeight)
						{
							floor.FloorDestHeight = sector.CeilingHeight;
						}
						floor.FloorDestHeight -= Fixed.FromInt(8) * (type == FloorMoveType.RaiseFloorCrush ? 1 : 0);
						break;

					case FloorMoveType.RaiseFloorTurbo:
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = floorSpeed * 4;
						floor.FloorDestHeight = FindNextHighestFloor(sector, sector.FloorHeight);
						break;

					case FloorMoveType.RaiseFloorToNearest:
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = floorSpeed;
						floor.FloorDestHeight = FindNextHighestFloor(sector, sector.FloorHeight);
						break;

					case FloorMoveType.RaiseFloor24:
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = floorSpeed;
						floor.FloorDestHeight = floor.Sector.FloorHeight + Fixed.FromInt(24);
						break;

					case FloorMoveType.RaiseFloor512:
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = floorSpeed;
						floor.FloorDestHeight = floor.Sector.FloorHeight + Fixed.FromInt(512);
						break;

					case FloorMoveType.RaiseFloor24AndChange:
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = floorSpeed;
						floor.FloorDestHeight = floor.Sector.FloorHeight + Fixed.FromInt(24);
						sector.FloorFlat = line.FrontSector.FloorFlat;
						sector.Special = line.FrontSector.Special;
						break;

					case FloorMoveType.RaiseToTexture:
						var min = BoomSectorModelCompatibility.ShortestTextureInitial(world.Options.Compatibility);
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = floorSpeed;
						var textures = world.Map.Textures;
						for (var i = 0; i < sector.Lines.Length; i++)
						{
							var modelLine = sector.Lines[i];
							if (!BoomSectorModelCompatibility.IsTwoSided(modelLine, world.Options.Compatibility))
								continue;

							var frontSide = modelLine.FrontSide;
							if (BoomSectorModelCompatibility.IsUsableShortestTexture(frontSide.BottomTexture, world.Options.Compatibility) &&
								textures[frontSide.BottomTexture].Height < min)
							{
								min = textures[frontSide.BottomTexture].Height;
							}

							var backSide = modelLine.BackSide;
							if (backSide != null &&
								BoomSectorModelCompatibility.IsUsableShortestTexture(backSide.BottomTexture, world.Options.Compatibility) &&
								textures[backSide.BottomTexture].Height < min)
							{
								min = textures[backSide.BottomTexture].Height;
							}
						}
						floor.FloorDestHeight = BoomSectorModelCompatibility.AddShortestTextureHeight(
							floor.Sector.FloorHeight, min, world.Options.Compatibility);
						break;

					case FloorMoveType.LowerAndChange:
						floor.Direction = -1;
						floor.Sector = sector;
						floor.Speed = floorSpeed;
						floor.FloorDestHeight = FindLowestFloorSurrounding(sector);
						floor.Texture = sector.FloorFlat;
						var model = FindBoomModelFloorSector(sector, floor.FloorDestHeight);
						if (model != null)
						{
							floor.Texture = model.FloorFlat;
							floor.NewSpecial = model.Special;
						}
						break;
				}
			}

			return result;
		}


		public bool DoBoomStairs(LineDef line, BoomStairSpecial specification)
		{
			var result = false;

			if (!specification.UsesTagForTargeting)
			{
				var sector = line.BackSector;
				result = sector != null && StartBoomStaircase(sector, specification);
			}
			else
			{
				var sectors = world.Map.BoomTags.GetSectors(line.Tag);
				for (var i = 0; i < sectors.Length; i++)
				{
					if (StartBoomStaircase(sectors[i], specification))
						result = true;
				}
			}

			if (result)
			{
				// Generalized retriggerable stairs alternate direction on each
				// successful activation. One-shot triggers are cleared by the router
				// immediately afterwards, so toggling here matches Boom's ordering.
				line.Special = (LineSpecial)((int)line.Special ^ BoomStairTranslator.DirectionMask);
			}

			return result;
		}

		private bool StartBoomStaircase(Sector firstSector, BoomStairSpecial specification)
		{
			if (IsFloorBusy(firstSector) || firstSector.StairLock != 0)
				return false;

			var direction = (int)specification.Direction;
			var speed = GetBoomStairSpeed(specification.Speed);
			var stairSize = Fixed.FromInt(specification.StepSize);
			var texture = firstSector.FloorFlat;
			var height = firstSector.FloorHeight + stairSize * direction;

			firstSector.StairLock = -2;
			firstSector.StairPreviousSector = -1;
			firstSector.StairNextSector = -1;
			StartBoomStairStep(firstSector, direction, speed, height);

			var sector = firstSector;

			while (true)
			{
				Sector next = null;

				// Sector.Lines is built in global linedef order, so the first
				// matching front-facing two-sided line is Boom's lowest-numbered line.
				for (var i = 0; i < sector.Lines.Length; i++)
				{
					var check = sector.Lines[i];
					if (check.BackSector == null || check.FrontSector != sector)
						continue;

					var target = check.BackSector;
					if (!specification.IgnoreTexture && target.FloorFlat != texture)
						continue;

					if (IsFloorBusy(target) || target.StairLock != 0)
						continue;

					next = target;
					break;
				}

				if (next == null)
					break;

				height += stairSize * direction;

				sector.StairNextSector = next.Number;
				next.StairPreviousSector = sector.Number;
				next.StairNextSector = -1;
				next.StairLock = -2;

				StartBoomStairStep(next, direction, speed, height);
				sector = next;
			}

			return true;
		}

		private void StartBoomStairStep(Sector sector, int direction, Fixed speed, Fixed destination)
		{
			var floor = new FloorMove(world)
			{
				Type = FloorMoveType.GeneralizedStair,
				Crush = false,
				Direction = direction,
				Sector = sector,
				Speed = speed,
				FloorDestHeight = destination
			};

			world.Thinkers.Add(floor);
			sector.FloorData = floor;
		}

		private static Fixed GetBoomStairSpeed(BoomActionSpeed speed)
		{
			return speed switch
			{
				BoomActionSpeed.Slow => floorSpeed / 4,
				BoomActionSpeed.Normal => floorSpeed / 2,
				BoomActionSpeed.Fast => floorSpeed * 2,
				BoomActionSpeed.Turbo => floorSpeed * 4,
				_ => floorSpeed / 4
			};
		}


		public bool BuildStairs(LineDef line, StairType type)
		{
			var sectors = world.Map.Sectors;
			var taggedSectorNumber = -1;
			var result = false;
			var fixedTaggedScan = BoomClassicMoverCompatibility.UsesFixedMultiTaggedStairScan(
				world.Options.Compatibility);

			while ((taggedSectorNumber = FindSectorFromLineTag(line, taggedSectorNumber)) >= 0)
			{
				var sectorNumber = taggedSectorNumber;
				var sector = sectors[sectorNumber];

				// Already moving? If so, keep going...
				if (IsFloorBusy(sector))
				{
					continue;
				}

				result = true;

				// New floor thinker.
				var floor = new FloorMove(world);
				world.Thinkers.Add(floor);
				sector.FloorData = floor;
				floor.Direction = 1;
				floor.Sector = sector;

				Fixed speed;
				Fixed stairSize;
				switch (type)
				{
					case StairType.Build8:
						speed = floorSpeed / 4;
						stairSize = Fixed.FromInt(8);
						break;
					case StairType.Turbo16:
						speed = floorSpeed * 4;
						stairSize = Fixed.FromInt(16);
						break;
					default:
						throw new Exception("Unknown stair type!");
				}

				floor.Speed = speed;
				var height = sector.FloorHeight + stairSize;
				floor.FloorDestHeight = height;

				var texture = sector.FloorFlat;

				// Find next sector to raise.
				//     1. Find 2-sided line with same sector side[0].
				//     2. Other side is the next sector to raise.
				bool ok;
				do
				{
					ok = false;

					for (var i = 0; i < sector.Lines.Length; i++)
					{
						if (((sector.Lines[i]).Flags & LineFlags.TwoSided) == 0)
						{
							continue;
						}

						var target = (sector.Lines[i]).FrontSector;
						var newSectorNumber = target.Number;

						if (sectorNumber != newSectorNumber)
						{
							continue;
						}

						target = (sector.Lines[i]).BackSector;
						newSectorNumber = target.Number;

						if (target.FloorFlat != texture)
						{
							continue;
						}

						height += stairSize;

						if (IsFloorBusy(target))
						{
							continue;
						}

						sector = target;
						sectorNumber = newSectorNumber;
						floor = new FloorMove(world);

						world.Thinkers.Add(floor);

						sector.FloorData = floor;
						floor.Direction = 1;
						floor.Sector = sector;
						floor.Speed = speed;
						floor.FloorDestHeight = height;
						ok = true;
						break;
					}
				} while (ok);

				// Vanilla Doom accidentally advances the outer tagged-sector search from
				// the last sector in the stair chain. Boom keeps the two cursors separate.
				if (!fixedTaggedScan)
				{
					taggedSectorNumber = sectorNumber;
				}
			}

			return result;
		}



		////////////////////////////////////////////////////////////
		// Ceiling
		////////////////////////////////////////////////////////////

		public bool DoBoomCeiling(LineDef line, BoomCeilingSpecial specification)
		{
			if (!specification.UsesTagForTargeting)
			{
				var sector = line.BackSector;
				return sector != null && !IsCeilingBusy(sector) && StartBoomCeiling(line, sector, specification);
			}

			var result = false;
			var sectors = world.Map.BoomTags.GetSectors(line.Tag);

			for (var i = 0; i < sectors.Length; i++)
			{
				var sector = sectors[i];
				if (IsCeilingBusy(sector))
					continue;

				if (StartBoomCeiling(line, sector, specification))
					result = true;
			}

			return result;
		}

		private bool StartBoomCeiling(LineDef line, Sector sector, BoomCeilingSpecial specification)
		{
			var destination = FindBoomCeilingDestination(sector, specification);
			var ceiling = new CeilingMove(world)
			{
				Type = CeilingMoveType.Generalized,
				Crush = specification.Crush,
				Sector = sector,
				Direction = (int)specification.Direction,
				Speed = CeilingSpeed * (1 << (int)specification.Speed),
				BottomHeight = destination,
				TopHeight = destination,
				Tag = sector.Tag,
				Texture = sector.CeilingFlat,
				NewSpecial = sector.Special
			};

			if (specification.Change != BoomChangeType.None)
			{
				var model = specification.Model == BoomModelType.Trigger
					? line.FrontSector
					: FindBoomModelCeilingSector(sector, destination);

				if (model != null)
				{
					ceiling.Texture = model.CeilingFlat;
					ceiling.NewSpecial = model.Special;
					ceiling.Type = specification.Change switch
					{
						BoomChangeType.TextureAndZeroSpecial => CeilingMoveType.GeneralizedChangeZero,
						BoomChangeType.TextureOnly => CeilingMoveType.GeneralizedChangeTexture,
						BoomChangeType.TextureAndSpecial => CeilingMoveType.GeneralizedChangeSpecial,
						_ => CeilingMoveType.Generalized
					};
				}
			}

			world.Thinkers.Add(ceiling);
			sector.CeilingData = ceiling;
			AddActiveCeiling(ceiling);
			return true;
		}

		private Fixed FindBoomCeilingDestination(Sector sector, BoomCeilingSpecial specification)
		{
			return specification.Target switch
			{
				BoomCeilingTarget.HighestNeighborCeiling => FindBoomHighestCeilingSurrounding(sector),
				BoomCeilingTarget.LowestNeighborCeiling => FindBoomLowestCeilingSurroundingForCeiling(sector),
				BoomCeilingTarget.NextNeighborCeiling => FindBoomNextCeiling(sector, specification.Direction),
				BoomCeilingTarget.HighestNeighborFloor => FindBoomHighestFloorSurrounding(sector),
				BoomCeilingTarget.Floor => sector.FloorHeight,
				BoomCeilingTarget.ShortestUpperTexture => FindBoomShortestUpperTextureDestination(sector, specification.Direction),
				BoomCeilingTarget.By24 => sector.CeilingHeight + (int)specification.Direction * Fixed.FromInt(24),
				BoomCeilingTarget.By32 => sector.CeilingHeight + (int)specification.Direction * Fixed.FromInt(32),
				BoomCeilingTarget.FloorPlus8 => sector.FloorHeight + Fixed.FromInt(8),
				_ => sector.CeilingHeight
			};
		}

		private Fixed FindBoomHighestCeilingSurrounding(Sector sector)
		{
			var height = boomMinPlaneHeight;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var other = GetNextSector(sector.Lines[i], sector);
				if (other != null && other.CeilingHeight > height)
					height = other.CeilingHeight;
			}

			return height;
		}

		private Fixed FindBoomLowestCeilingSurroundingForCeiling(Sector sector)
		{
			var height = boomMaxPlaneHeight;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var other = GetNextSector(sector.Lines[i], sector);
				if (other != null && other.CeilingHeight < height)
					height = other.CeilingHeight;
			}

			return height;
		}

		private Fixed FindBoomNextCeiling(Sector sector, BoomPlaneDirection direction)
		{
			var current = sector.CeilingHeight;
			var result = current;
			var found = false;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var other = GetNextSector(sector.Lines[i], sector);
				if (other == null)
					continue;

				var height = other.CeilingHeight;
				if (direction == BoomPlaneDirection.Up)
				{
					if (height > current && (!found || height < result))
					{
						result = height;
						found = true;
					}
				}
				else if (height < current && (!found || height > result))
				{
					result = height;
					found = true;
				}
			}

			return result;
		}

		private Fixed FindBoomShortestUpperTextureDestination(Sector sector, BoomPlaneDirection direction)
		{
			var shortest = 32000;
			var textures = world.Map.Textures;

			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var line = sector.Lines[i];
				if (!BoomSectorModelCompatibility.IsTwoSided(line, world.Options.Compatibility))
					continue;

				if (BoomSectorModelCompatibility.IsUsableShortestTexture(line.FrontSide.TopTexture, world.Options.Compatibility))
					shortest = Math.Min(shortest, textures[line.FrontSide.TopTexture].Height);

				if (line.BackSide != null &&
					BoomSectorModelCompatibility.IsUsableShortestTexture(line.BackSide.TopTexture, world.Options.Compatibility))
					shortest = Math.Min(shortest, textures[line.BackSide.TopTexture].Height);
			}

			if (shortest == 32000)
				return direction == BoomPlaneDirection.Up ? boomMaxPlaneHeight : boomMinPlaneHeight;

			var delta = Fixed.FromInt(shortest);
			if (direction == BoomPlaneDirection.Up)
				return sector.CeilingHeight > boomMaxPlaneHeight - delta ? boomMaxPlaneHeight : sector.CeilingHeight + delta;

			return sector.CeilingHeight < boomMinPlaneHeight + delta ? boomMinPlaneHeight : sector.CeilingHeight - delta;
		}

		private Sector FindBoomModelCeilingSector(Sector sector, Fixed destination)
		{
			for (var i = 0; i < sector.Lines.Length; i++)
			{
				var other = GetNextSector(sector.Lines[i], sector);
				if (other != null && other.CeilingHeight == destination)
					return other;
			}

			return null;
		}

		public bool DoBoomCrusher(LineDef line, BoomCrusherSpecial specification)
		{
			var result = ActivateInStasisCeiling(line);

			if (!specification.UsesTagForTargeting)
			{
				var sector = line.BackSector;
				if (sector != null && !IsCeilingBusy(sector) && StartBoomCrusher(sector, specification))
					result = true;

				return result;
			}

			var sectors = world.Map.BoomTags.GetSectors(line.Tag);
			for (var i = 0; i < sectors.Length; i++)
			{
				var sector = sectors[i];
				if (IsCeilingBusy(sector))
					continue;

				if (StartBoomCrusher(sector, specification))
					result = true;
			}

			return result;
		}

		private bool StartBoomCrusher(Sector sector, BoomCrusherSpecial specification)
		{
			var speed = CeilingSpeed * (1 << (int)specification.Speed);
			var ceiling = new CeilingMove(world)
			{
				Type = specification.Silent
					? CeilingMoveType.GeneralizedSilentCrusher
					: CeilingMoveType.GeneralizedCrusher,
				Sector = sector,
				BottomHeight = sector.FloorHeight + Fixed.FromInt(8),
				TopHeight = sector.CeilingHeight,
				Speed = speed,
				OldSpeed = speed,
				Crush = true,
				Direction = -1,
				Tag = sector.Tag
			};

			world.Thinkers.Add(ceiling);
			sector.CeilingData = ceiling;
			AddActiveCeiling(ceiling);
			return true;
		}

		public bool DoBoomExtendedCrusher(LineDef line, BoomExtendedCrusherSpecial specification)
		{
			if (specification.Action == BoomExtendedCrusherAction.Stop)
				return CeilingCrushStop(line);

			var type = specification.Action switch
			{
				BoomExtendedCrusherAction.Fast => CeilingMoveType.FastCrushAndRaise,
				BoomExtendedCrusherAction.Silent => CeilingMoveType.SilentCrushAndRaise,
				_ => CeilingMoveType.CrushAndRaise
			};

			// DoCeiling reactivates crusher thinkers in stasis, but its return value only
			// reports newly created movers. Preserve Boom's successful-trigger semantics
			// when an existing crusher is restarted without creating a new thinker.
			var reactivated = ActivateInStasisCeiling(line);
			return DoCeiling(line, type) || reactivated;
		}

		public bool DoCeiling(LineDef line, CeilingMoveType type)
		{
			// Reactivate in-stasis ceilings...for certain types.
			switch (type)
			{
				case CeilingMoveType.FastCrushAndRaise:
				case CeilingMoveType.SilentCrushAndRaise:
				case CeilingMoveType.CrushAndRaise:
					ActivateInStasisCeiling(line);
					break;

				default:
					break;
			}

			var sectors = world.Map.Sectors;
			var sectorNumber = -1;
			var result = false;

			while ((sectorNumber = FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var sector = sectors[sectorNumber];
				if (IsCeilingBusy(sector))
				{
					continue;
				}

				result = true;

				// New ceiling thinker.
				var ceiling = new CeilingMove(world);
				world.Thinkers.Add(ceiling);
				sector.CeilingData = ceiling;
				ceiling.Sector = sector;
				ceiling.Crush = false;

				switch (type)
				{
					case CeilingMoveType.FastCrushAndRaise:
						ceiling.Crush = true;
						ceiling.TopHeight = sector.CeilingHeight;
						ceiling.BottomHeight = sector.FloorHeight + Fixed.FromInt(8);
						ceiling.Direction = -1;
						ceiling.Speed = CeilingSpeed * 2;
						break;

					case CeilingMoveType.SilentCrushAndRaise:
					case CeilingMoveType.CrushAndRaise:
					case CeilingMoveType.LowerAndCrush:
					case CeilingMoveType.LowerToFloor:
						if (type == CeilingMoveType.SilentCrushAndRaise
							|| type == CeilingMoveType.CrushAndRaise)
						{
							ceiling.Crush = true;
							ceiling.TopHeight = sector.CeilingHeight;
						}
						ceiling.BottomHeight = sector.FloorHeight;
						if (type != CeilingMoveType.LowerToFloor)
						{
							ceiling.BottomHeight += Fixed.FromInt(8);
						}
						ceiling.Direction = -1;
						ceiling.Speed = CeilingSpeed;
						break;

					case CeilingMoveType.RaiseToHighest:
						ceiling.TopHeight = FindHighestCeilingSurrounding(sector);
						ceiling.Direction = 1;
						ceiling.Speed = CeilingSpeed;
						break;
				}

				ceiling.Tag = sector.Tag;
				ceiling.Type = type;
				AddActiveCeiling(ceiling);
			}

			return result;
		}


		public static readonly Fixed CeilingSpeed = Fixed.One;
		public static readonly int CeilingWwait = 150;

		private static readonly int maxCeilingCount = 30;

		private CeilingMove[] activeCeilings = new CeilingMove[maxCeilingCount];

		public void AddActiveCeiling(CeilingMove ceiling)
		{
			for (var i = 0; i < activeCeilings.Length; i++)
			{
				if (activeCeilings[i] == null)
				{
					activeCeilings[i] = ceiling;

					return;
				}
			}
		}

		public void RemoveActiveCeiling(CeilingMove ceiling)
		{
			for (var i = 0; i < activeCeilings.Length; i++)
			{
				if (activeCeilings[i] == ceiling)
				{
					activeCeilings[i].Sector.CeilingData = null;
					world.Thinkers.Remove(activeCeilings[i]);
					activeCeilings[i] = null;
					break;
				}
			}
		}

		public bool CheckActiveCeiling(CeilingMove ceiling)
		{
			if (ceiling == null)
			{
				return false;
			}

			for (var i = 0; i < activeCeilings.Length; i++)
			{
				if (activeCeilings[i] == ceiling)
				{
					return true;
				}
			}

			return false;
		}

		public bool ActivateInStasisCeiling(LineDef line)
		{
			var result = false;

			for (var i = 0; i < activeCeilings.Length; i++)
			{
				if (activeCeilings[i] != null &&
					activeCeilings[i].Tag == line.Tag &&
					activeCeilings[i].Direction == 0)
				{
					activeCeilings[i].Direction = activeCeilings[i].OldDirection;
					activeCeilings[i].ThinkerState = ThinkerState.Active;
					result = true;
				}
			}

			return result;
		}

		public bool CeilingCrushStop(LineDef line)
		{
			var result = false;

			for (var i = 0; i < activeCeilings.Length; i++)
			{
				if (activeCeilings[i] != null &&
					activeCeilings[i].Tag == line.Tag &&
					activeCeilings[i].Direction != 0)
				{
					activeCeilings[i].OldDirection = activeCeilings[i].Direction;
					activeCeilings[i].ThinkerState = ThinkerState.InStasis;
					activeCeilings[i].Direction = 0;
					result = true;
				}
			}

			return result;
		}



		////////////////////////////////////////////////////////////
		// Teleport
		////////////////////////////////////////////////////////////

		public bool Teleport(LineDef line, int side, Mobj thing)
		{
			var boomSemantics = GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility);

			return Teleport(
				line,
				side,
				thing,
				forceFloorAtDestination: boomSemantics,
				boomPlayerSemantics: boomSemantics);
		}

		private bool Teleport(
			LineDef line,
			int side,
			Mobj thing,
			bool forceFloorAtDestination,
			bool boomPlayerSemantics)
		{
			// Don't teleport missiles.
			if ((thing.Flags & MobjFlags.Missile) != 0)
			{
				return false;
			}

			// Don't teleport if hit back of line, so you can get out of teleporter.
			if (side == 1)
			{
				return false;
			}

			foreach (var taggedSector in world.Map.BoomTags.GetSectors(line.Tag))
			{
				foreach (var thinker in world.Thinkers)
				{
					var dest = thinker as Mobj;

					if (dest == null)
					{
						// Not a mobj.
						continue;
					}

					if (dest.Type != MobjType.Teleportman)
					{
						// Not a teleportman.
						continue;
					}

					var sector = dest.Subsector.Sector;

					if (sector.Number != taggedSector.Number)
					{
						// Wrong sector.
						continue;
					}

					var oldX = thing.X;
					var oldY = thing.Y;
					var oldZ = thing.Z;

					if (!world.ThingMovement.TeleportMove(thing, dest.X, dest.Y))
					{
						return false;
					}

					// This compatibility fix is based on Chocolate Doom's implementation.
					if (forceFloorAtDestination || world.Options.GameVersion != GameVersion.Final)
					{
						thing.Z = thing.FloorZ;
					}

					var viewPlayer = boomPlayerSemantics
						? BoomTeleportQuirks.GetPlayerForTeleportViewUpdate(thing, world.Options.Compatibility)
						: thing.Player;

					if (viewPlayer != null)
					{
						viewPlayer.ViewZ = thing.Z + viewPlayer.ViewHeight;
					}

					var ta = world.ThingAllocation;

					// Spawn teleport fog at source position.
					var fog1 = ta.SpawnMobj(
						oldX,
						oldY,
						oldZ,
						MobjType.Tfog);
					world.StartSound(fog1, Sfx.TELEPT, SfxType.Misc);

					// Destination position.
					var angle = dest.Angle;
					var fog2 = ta.SpawnMobj(
						dest.X + 20 * Trig.Cos(angle),
						dest.Y + 20 * Trig.Sin(angle),
						thing.Z,
						MobjType.Tfog);
					world.StartSound(fog2, Sfx.TELEPT, SfxType.Misc);

					if (thing.Player != null)
					{
						// Don't move for a bit.
						thing.ReactionTime = 18;
					}

					thing.Angle = dest.Angle;
					thing.MomX = thing.MomY = thing.MomZ = Fixed.Zero;

					thing.DisableFrameInterpolationForOneFrame();
					viewPlayer?.DisableFrameInterpolationForOneFrame();

					return true;
				}
			}

			return false;
		}

		public bool DoBoomTeleport(LineDef line, int side, Mobj thing, BoomTeleportSpecial specification)
		{
			if (side != 0 || thing == null || (thing.Flags & MobjFlags.Missile) != 0)
			{
				return false;
			}

			if (specification.Destination == BoomTeleportDestination.Line)
			{
				return BoomSilentLineTeleport(line, thing, specification.Reverse);
			}

			if (specification.Silent)
			{
				return BoomSilentTeleport(line, thing, specification.PreserveOrientation);
			}

			return Teleport(line, side, thing, forceFloorAtDestination: true, boomPlayerSemantics: true);
		}

		private bool BoomSilentTeleport(LineDef line, Mobj thing, bool preserveOrientation)
		{
			var destination = FindBoomTeleportDestination(line);
			if (destination == null)
			{
				return false;
			}

			var heightAboveFloor = thing.Z - thing.FloorZ;
			var oldMomX = thing.MomX;
			var oldMomY = thing.MomY;
			var realPlayer = GetRealPlayer(thing);

			if (!world.ThingMovement.TeleportMove(thing, destination.X, destination.Y))
			{
				return false;
			}

			thing.Z = thing.FloorZ + heightAboveFloor;

			if (preserveOrientation)
			{
				var lineAngle = Geometry.PointToAngle(Fixed.Zero, Fixed.Zero, line.Dx, line.Dy);
				var angle = lineAngle - destination.Angle + Angle.Ang90;
				var sine = Trig.Sin(angle);
				var cosine = Trig.Cos(angle);

				thing.Angle += angle;
				thing.MomX = oldMomX * cosine - oldMomY * sine;
				thing.MomY = oldMomY * cosine + oldMomX * sine;
			}
			else
			{
				thing.Angle = destination.Angle;
				thing.MomX = Fixed.Zero;
				thing.MomY = Fixed.Zero;
			}

			UpdatePlayerAfterSilentTeleport(realPlayer);
			DisableTeleportInterpolation(thing, realPlayer);

			return true;
		}

		private bool BoomSilentLineTeleport(LineDef line, Mobj thing, bool reverse)
		{
			LineDef destination = null;

			foreach (var candidate in world.Map.BoomTags.GetLines(line.Tag))
			{
				if (candidate != line && candidate.BackSector != null)
				{
					destination = candidate;
					break;
				}
			}

			if (destination == null)
			{
				return false;
			}

			var position = Fixed.Abs(line.Dx) > Fixed.Abs(line.Dy)
				? (thing.X - line.Vertex1.X) / line.Dx
				: (thing.Y - line.Vertex1.Y) / line.Dy;

			if (reverse)
			{
				position = Fixed.One - position;
			}

			var sourceAngle = Geometry.PointToAngle(Fixed.Zero, Fixed.Zero, line.Dx, line.Dy);
			var destinationAngle = Geometry.PointToAngle(Fixed.Zero, Fixed.Zero, destination.Dx, destination.Dy);
			var angle = (reverse ? Angle.Ang0 : Angle.Ang180) + destinationAngle - sourceAngle;

			var x = destination.Vertex2.X - position * destination.Dx;
			var y = destination.Vertex2.Y - position * destination.Dy;
			var realPlayer = GetRealPlayer(thing);
			var stepDown = destination.FrontSector.FloorHeight < destination.BackSector.FloorHeight;
			var exitSide = reverse || (realPlayer != null && stepDown) ? 1 : 0;

			for (var fudge = 10; Geometry.PointOnLineSide(x, y, destination) != exitSide && fudge > 0; fudge--)
			{
				if (Fixed.Abs(destination.Dx) > Fixed.Abs(destination.Dy))
				{
					var movePositive = (destination.Dx < Fixed.Zero) != (exitSide != 0);
					y += movePositive ? Fixed.Epsilon : -Fixed.Epsilon;
				}
				else
				{
					var moveNegative = (destination.Dy < Fixed.Zero) != (exitSide != 0);
					x += moveNegative ? -Fixed.Epsilon : Fixed.Epsilon;
				}
			}

			var heightAboveFloor = thing.Z - thing.FloorZ;
			var oldMomX = thing.MomX;
			var oldMomY = thing.MomY;

			if (!world.ThingMovement.TeleportMove(thing, x, y))
			{
				return false;
			}

			thing.Z = heightAboveFloor + Fixed.Max(destination.FrontSector.FloorHeight, destination.BackSector.FloorHeight);
			thing.Angle += angle;

			var sine = Trig.Sin(angle);
			var cosine = Trig.Cos(angle);
			thing.MomX = oldMomX * cosine - oldMomY * sine;
			thing.MomY = oldMomY * cosine + oldMomX * sine;

			UpdatePlayerAfterSilentTeleport(realPlayer);
			DisableTeleportInterpolation(thing, realPlayer);

			return true;
		}

		private Mobj FindBoomTeleportDestination(LineDef line)
		{
			foreach (var taggedSector in world.Map.BoomTags.GetSectors(line.Tag))
			{
				foreach (var thinker in world.Thinkers)
				{
					if (thinker is Mobj destination &&
						destination.Type == MobjType.Teleportman &&
						destination.Subsector.Sector.Number == taggedSector.Number)
					{
						return destination;
					}
				}
			}

			return null;
		}

		private static Player GetRealPlayer(Mobj thing)
		{
			var player = thing.Player;
			return player != null && player.Mobj == thing ? player : null;
		}

		private void UpdatePlayerAfterSilentTeleport(Player player)
		{
			if (player == null)
			{
				return;
			}

			var deltaViewHeight = player.DeltaViewHeight;
			player.DeltaViewHeight = Fixed.Zero;
			world.PlayerBehavior.CalcHeight(player);
			player.DeltaViewHeight = deltaViewHeight;
		}

		private static void DisableTeleportInterpolation(Mobj thing, Player player)
		{
			thing.DisableFrameInterpolationForOneFrame();
			player?.DisableFrameInterpolationForOneFrame();
		}



		////////////////////////////////////////////////////////////
		// Lighting
		////////////////////////////////////////////////////////////

		public void TurnTagLightsOff(LineDef line)
		{
			foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
			{
				var min = sector.LightLevel;

				for (var j = 0; j < sector.Lines.Length; j++)
				{
					var target = GetNextSector(sector.Lines[j], sector);
					if (target == null)
					{
						continue;
					}

					if (target.LightLevel < min)
					{
						min = target.LightLevel;
					}
				}

				sector.LightLevel = min;
			}
		}

		public void LightTurnOn(LineDef line, int bright)
		{
			var boomLighting = GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility);

			foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
			{
				// bright = 0 means to search for highest light level surrounding sector.
				// Vanilla carries the first result into later tagged sectors; Boom fixes this
				// by calculating the maximum independently for every target sector.
				var targetBright = bright;
				if (targetBright == 0)
				{
					for (var j = 0; j < sector.Lines.Length; j++)
					{
						var target = GetNextSector(sector.Lines[j], sector);
						if (target == null)
						{
							continue;
						}

						if (target.LightLevel > targetBright)
						{
							targetBright = target.LightLevel;
						}
					}
				}

				sector.LightLevel = targetBright;

				if (!boomLighting)
				{
					bright = targetBright;
				}
			}
		}


		/// <summary>
		/// Boom EV_LightTurnOnPartway: interpolate every tagged sector between
		/// its minimum and maximum neighboring light levels.
		/// </summary>
		public void LightTurnOnPartway(LineDef line, Fixed level)
		{
			if (level < Fixed.Zero)
			{
				level = Fixed.Zero;
			}
			else if (level > Fixed.One)
			{
				level = Fixed.One;
			}

			foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
			{
				var bright = 0;
				var min = sector.LightLevel;

				for (var j = 0; j < sector.Lines.Length; j++)
				{
					var target = GetNextSector(sector.Lines[j], sector);
					if (target == null)
					{
						continue;
					}

					if (target.LightLevel > bright)
					{
						bright = target.LightLevel;
					}

					if (target.LightLevel < min)
					{
						min = target.LightLevel;
					}
				}

				var interpolated = level * bright + (Fixed.One - level) * min;
				sector.LightLevel = interpolated.ToIntFloor();
			}
		}


		public void StartLightStrobing(LineDef line)
		{
			var sectors = world.Map.Sectors;
			var sectorNumber = -1;
			var boomLighting = GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility);

			while ((sectorNumber = FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var sector = sectors[sectorNumber];

				if (boomLighting ? sector.LightingData != null : sector.SpecialData != null)
				{
					continue;
				}

				world.LightingChange.SpawnStrobeFlash(sector, StrobeFlash.SlowDark, false);
			}
		}

		public bool DoBoomLighting(LineDef line, BoomLightingSpecial specification)
		{
			switch (specification.Target)
			{
				case BoomLightingTarget.Light35:
					LightTurnOn(line, 35);
					break;

				case BoomLightingTarget.Light255:
					LightTurnOn(line, 255);
					break;

				case BoomLightingTarget.MaximumNeighbor:
					LightTurnOn(line, 0);
					break;

				case BoomLightingTarget.MinimumNeighbor:
					TurnTagLightsOff(line);
					break;

				case BoomLightingTarget.Blinking:
					StartLightStrobing(line);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(specification));
			}

			return true;
		}



		////////////////////////////////////////////////////////////
		// Miscellaneous
		////////////////////////////////////////////////////////////

		public bool DoDonut(LineDef line)
		{
			var sectors = world.Map.Sectors;
			var sectorNumber = -1;
			var result = false;

			while ((sectorNumber = FindSectorFromLineTag(line, sectorNumber)) >= 0)
			{
				var s1 = sectors[sectorNumber];

				// Already moving? If so, keep going...
				if (IsFloorBusy(s1))
				{
					continue;
				}

				result = true;

				var s2 = GetNextSector(s1.Lines[0], s1);

				//
				// The code below is based on Chocolate Doom's implementation.
				//

				if (s2 == null)
				{
					break;
				}

				for (var i = 0; i < s2.Lines.Length; i++)
				{
					var s3 = s2.Lines[i].BackSector;

					if (s3 == s1)
					{
						continue;
					}

					if (s3 == null)
					{
						// Undefined behavior in Vanilla Doom.
						return result;
					}

					var thinkers = world.Thinkers;

					// Spawn rising slime.
					var floor1 = new FloorMove(world);
					thinkers.Add(floor1);
					s2.FloorData = floor1;
					floor1.Type = FloorMoveType.DonutRaise;
					floor1.Crush = false;
					floor1.Direction = 1;
					floor1.Sector = s2;
					floor1.Speed = floorSpeed / 2;
					floor1.Texture = s3.FloorFlat;
					floor1.NewSpecial = 0;
					floor1.FloorDestHeight = s3.FloorHeight;

					// Spawn lowering donut-hole.
					var floor2 = new FloorMove(world);
					thinkers.Add(floor2);
					s1.FloorData = floor2;
					floor2.Type = FloorMoveType.LowerFloor;
					floor2.Crush = false;
					floor2.Direction = -1;
					floor2.Sector = s1;
					floor2.Speed = floorSpeed / 2;
					floor2.FloorDestHeight = s3.FloorHeight;

					break;
				}
			}

			return result;
		}


		public void SpawnDoorCloseIn30(Sector sector)
		{
			var door = new VerticalDoor(world);

			world.Thinkers.Add(door);

			sector.CeilingData = door;
			sector.Special = 0;

			door.Sector = sector;
			door.Direction = 0;
			door.Type = VerticalDoorType.Normal;
			door.Speed = doorSpeed;
			door.TopCountDown = 30 * 35;
		}

		public void SpawnDoorRaiseIn5Mins(Sector sector)
		{
			var door = new VerticalDoor(world);

			world.Thinkers.Add(door);

			sector.CeilingData = door;
			sector.Special = 0;

			door.Sector = sector;
			door.Direction = 2;
			door.Type = VerticalDoorType.RaiseIn5Mins;
			door.Speed = doorSpeed;
			door.TopHeight = FindLowestCeilingSurrounding(sector);
			door.TopHeight -= Fixed.FromInt(4);
			door.TopWait = doorWait;
			door.TopCountDown = 5 * 60 * 35;
		}
	}
}
