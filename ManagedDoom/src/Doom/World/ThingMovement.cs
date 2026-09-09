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
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Collision;
using ManagedDoom.Compatibility.Boom.Movement;
using ManagedDoom.Compatibility.Boom.Sectors;
using ManagedDoom.Compatibility.Mbf.AI;
using ManagedDoom.Compatibility.Mbf.Audio;
using ManagedDoom.Compatibility.Mbf.Movement;

namespace ManagedDoom
{
    public sealed class ThingMovement
    {
        private World world;
        private BoomSectorTouchingList boomSectorTouchingList;
        private BoomLedgeTorque boomLedgeTorque;

        public ThingMovement(World world)
        {
            this.world = world;
            boomSectorTouchingList = new BoomSectorTouchingList(world);
            boomLedgeTorque = new BoomLedgeTorque(world);

            InitThingMovement();
            InitSlideMovement();
            InitTeleportMovement();
        }



        ////////////////////////////////////////////////////////////
        // General thing movement
        ////////////////////////////////////////////////////////////

        public static readonly Fixed FloatSpeed = Fixed.FromInt(4);

        private static readonly int maxSpecialCrossCount = 64;
        private static readonly Fixed maxMove = Fixed.FromInt(30);
        private static readonly Fixed gravity = Fixed.One;

        private Mobj currentThing;
        private MobjFlags currentFlags;
        private Fixed currentX;
        private Fixed currentY;
        private Fixed[] currentBox;

        private Fixed currentFloorZ;
        private Fixed currentCeilingZ;
        private Fixed currentDropoffZ;
        private bool floatOk;
        private bool fellDown;

        private LineDef currentCeilingLine;
        private LineDef currentBlockingLine;
        private LineDef currentMbfBounceLine;

        public int crossedSpecialCount;
        public LineDef[] crossedSpecials;

        private Func<LineDef, bool> checkLineFunc;
        private Func<Mobj, bool> checkThingFunc;

        private void InitThingMovement()
        {
            currentBox = new Fixed[4];
            crossedSpecials = new LineDef[maxSpecialCrossCount];
            checkLineFunc = CheckLine;
            checkThingFunc = CheckThing;
        }


        /// <summary>
        /// Links a thing into both a block and a subsector based on
        /// its x and y. Sets thing.Subsector properly.
        /// </summary>
        public void SetThingPosition(Mobj thing)
        {
            var map = world.Map;

            var subsector = Geometry.PointInSubsector(thing.X, thing.Y, map);

            thing.Subsector = subsector;

            // Invisible things don't go into the sector links.
            if ((thing.Flags & MobjFlags.NoSector) == 0)
            {
                var sector = subsector.Sector;

                thing.SectorPrev = null;
                thing.SectorNext = sector.ThingList;

                if (sector.ThingList != null)
                {
                    sector.ThingList.SectorPrev = thing;
                }

                sector.ThingList = thing;
            }

            if (GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility))
            {
                boomSectorTouchingList.Update(thing);
            }

            // Inert things don't need to be in blockmap.
            if ((thing.Flags & MobjFlags.NoBlockMap) == 0)
            {
                var index = map.BlockMap.GetIndex(thing.X, thing.Y);

                if (index != -1)
                {
                    var link = map.BlockMap.ThingLists[index];

                    thing.BlockPrev = null;
                    thing.BlockNext = link;
                    thing.BlockMapIndex = index;

                    if (link != null)
                    {
                        link.BlockPrev = thing;
                    }

                    map.BlockMap.ThingLists[index] = thing;
                }
                else
                {
                    // Thing is off the map.
                    thing.BlockNext = null;
                    thing.BlockPrev = null;
                    thing.BlockMapIndex = -1;
                }
            }
            else
            {
                thing.BlockMapIndex = -1;
            }
        }

        /// <summary>
        /// Unlinks a thing from block map and sectors.
        /// On each position change, BLOCKMAP and other lookups
        /// maintaining lists ot things inside these structures
        /// need to be updated.
        /// </summary>
        public void UnsetThingPosition(Mobj thing)
        {
            var map = world.Map;

            // Invisible things don't go into the sector links.
            if ((thing.Flags & MobjFlags.NoSector) == 0)
            {
                // Unlink from subsector.
                if (thing.SectorNext != null)
                {
                    thing.SectorNext.SectorPrev = thing.SectorPrev;
                }

                if (thing.SectorPrev != null)
                {
                    thing.SectorPrev.SectorNext = thing.SectorNext;
                }
                else
                {
                    thing.Subsector.Sector.ThingList = thing.SectorNext;
                }
            }

            // Inert things don't need to be in blockmap.
            if ((thing.Flags & MobjFlags.NoBlockMap) == 0)
            {
                // Unlink from the block where the thing was actually linked.
                // Doom's bprev is a pointer-to-pointer, so unlinking does not
                // depend on the actor's current X/Y. ManagedDoom stores the
                // previous actor instead, therefore a head node also needs to
                // remember its owning block explicitly. This matters for MBF
                // projectiles such as the grenade, whose spawn check advances
                // X/Y before P_TryMove while the actor is present in BLOCKMAP.
                if (thing.BlockNext != null)
                {
                    thing.BlockNext.BlockPrev = thing.BlockPrev;
                }

                if (thing.BlockPrev != null)
                {
                    thing.BlockPrev.BlockNext = thing.BlockNext;
                }
                else if (thing.BlockMapIndex != -1)
                {
                    map.BlockMap.ThingLists[thing.BlockMapIndex] = thing.BlockNext;
                }

                thing.BlockNext = null;
                thing.BlockPrev = null;
                thing.BlockMapIndex = -1;
            }
        }

        public void RemoveTouchingSectorLinks(Mobj thing)
        {
            boomSectorTouchingList.RemoveAll(thing);
        }

        public void VisitThingsTouchingSector(Sector sector, Func<Mobj, bool> action)
        {
            boomSectorTouchingList.VisitSectorThings(sector, action);
        }


        /// <summary>
        /// Adjusts currentFloorZ and currentCeilingZ as lines are contacted.
        /// </summary>
        private bool CheckLine(LineDef line)
        {
            var mc = world.MapCollision;

            if (currentBox.Right() <= line.BoundingBox.Left() ||
                currentBox.Left() >= line.BoundingBox.Right() ||
                currentBox.Top() <= line.BoundingBox.Bottom() ||
                currentBox.Bottom() >= line.BoundingBox.Top())
            {
                return true;
            }

            if (Geometry.BoxOnLineSide(currentBox, line) != -1)
            {
                return true;
            }

            // A line has been hit.
            //
            // The moving thing's destination position will cross the given line.
            // If this should not be allowed, return false.
            // If the line is special, keep track of it to process later if the move is proven ok.
            //
            // NOTE:
            //     specials are NOT sorted by order, so two special lines that are only 8 pixels
            //     apart could be crossed in either order.

            if (line.BackSector == null)
            {
                // One sided line. Keep the contacted line for MBF wall-bounce
                // reflection without changing the existing BlockingLine value
                // used by monster/door compatibility code.
                if (MbfBounceCompatibility.IsBouncer(world.Options.Compatibility, currentThing))
                    currentMbfBounceLine = line;
                return false;
            }

            if (!MbfBounceCompatibility.UsesMissileLineBlockingRules(
                    world.Options.Compatibility,
                    currentThing))
            {
                if ((line.Flags & LineFlags.Blocking) != 0)
                {
                    // Explicitly blocking everything.
                    return false;
                }

                if (currentThing.Player == null && (line.Flags & LineFlags.BlockMonsters) != 0)
                {
                    // Block monsters only.
                    return false;
                }
            }

            // Set openrange, opentop, openbottom.
            mc.LineOpening(line);

            // Adjust floor / ceiling heights.
            if (mc.OpenTop < currentCeilingZ)
            {
                currentCeilingZ = mc.OpenTop;
                currentCeilingLine = line;
                currentBlockingLine = line;
                if (MbfBounceCompatibility.IsBouncer(world.Options.Compatibility, currentThing))
                    currentMbfBounceLine = line;
            }

            if (mc.OpenBottom > currentFloorZ)
            {
                currentFloorZ = mc.OpenBottom;
                currentBlockingLine = line;
                if (MbfBounceCompatibility.IsBouncer(world.Options.Compatibility, currentThing))
                    currentMbfBounceLine = line;
            }

            if (mc.LowFloor < currentDropoffZ)
            {
                currentDropoffZ = mc.LowFloor;
            }

            // If contacted a special line, add it to the list.
            if (line.Special != 0)
            {
                crossedSpecials[crossedSpecialCount] = line;
                crossedSpecialCount++;
            }

            return true;
        }

        private bool CheckThing(Mobj thing)
        {
            var baseInteractionFlags = MobjFlags.Solid | MobjFlags.Special | MobjFlags.Shootable;
            if ((thing.Flags & baseInteractionFlags) == 0 &&
                !MbfTouchyCompatibility.RequiresThingCollisionCheck(
                    world.Options.Compatibility,
                    thing))
            {
                return true;
            }

            var blockDist = thing.Radius + currentThing.Radius;

            if (Fixed.Abs(thing.X - currentX) >= blockDist ||
                Fixed.Abs(thing.Y - currentY) >= blockDist)
            {
                // Didn't hit it.
                return true;
            }

            // Don't clip against self.
            if (thing == currentThing)
            {
                return true;
            }

            // MBF TOUCHY checks the object being contacted, not the mover.
            // This preserves the original rule that an inert touchy actor does
            // not trigger merely because it is the only object moving.
            if (MbfTouchyCompatibility.ShouldActivateOnThingContact(
                    world.Options.Compatibility,
                    thing,
                    currentThing))
            {
                world.ThingInteraction.DamageMobj(thing, null, null, thing.Health);
                return true;
            }

            // Check for skulls slamming into things.
            if ((currentThing.Flags & MobjFlags.SkullFly) != 0)
            {
                var damage = ((world.Random.Next() % 8) + 1) * currentThing.Info.Damage;

                world.ThingInteraction.DamageMobj(thing, currentThing, currentThing, damage);

                currentThing.Flags &= ~MobjFlags.SkullFly;
                currentThing.MomX = currentThing.MomY = currentThing.MomZ = Fixed.Zero;

                currentThing.SetState(currentThing.Info.SpawnState);

                // Stop moving.
                return false;
            }

            // MBF lets non-solid BOUNCES actors interact with solid actors
            // using the same vertical/source filtering as the original path,
            // but without applying the missile damage branch.
            if (MbfBounceCompatibility.IsNonSolidNonMissileBouncer(
                    world.Options.Compatibility,
                    currentThing))
            {
                if (currentThing.Z > thing.Z + thing.Height ||
                    currentThing.Z + currentThing.Height < thing.Z)
                {
                    return true;
                }

                if (currentThing.Target != null &&
                    (currentThing.Target.Type == thing.Type ||
                     (currentThing.Target.Type == MobjType.Knight && thing.Type == MobjType.Bruiser) ||
                     (currentThing.Target.Type == MobjType.Bruiser && thing.Type == MobjType.Knight)))
                {
                    if (thing == currentThing.Target)
                        return true;

                    if (thing.Type != MobjType.Player)
                        return false;
                }

                if ((thing.Flags & MobjFlags.Solid) == 0)
                    return true;

                MbfBounceCompatibility.BounceFromSolidThing(currentThing);
                return false;
            }

            // Missiles can hit other things.
            if ((currentThing.Flags & MobjFlags.Missile) != 0)
            {
                // See if it went over / under.
                if (currentThing.Z > thing.Z + thing.Height)
                {
                    // Overhead.
                    return true;
                }

                if (currentThing.Z + currentThing.Height < thing.Z)
                {
                    // Underneath.
                    return true;
                }

                if (currentThing.Target != null &&
                        (currentThing.Target.Type == thing.Type ||
                        (currentThing.Target.Type == MobjType.Knight && thing.Type == MobjType.Bruiser) ||
                        (currentThing.Target.Type == MobjType.Bruiser && thing.Type == MobjType.Knight)))
                {
                    // Don't hit same species as originator.
                    if (thing == currentThing.Target)
                    {
                        return true;
                    }

                    if (thing.Type != MobjType.Player && !DoomInfo.DeHackEdConst.MonstersInfight)
                    {
                        // Explode, but do no damage.
                        // Let players missile other players.
                        return false;
                    }
                }

                if ((thing.Flags & MobjFlags.Shootable) == 0)
                {
                    // Didn't do any damage.
                    return (thing.Flags & MobjFlags.Solid) == 0;
                }

                // Damage / explode.
                var damage = ((world.Random.Next() % 8) + 1) * currentThing.Info.Damage;
                world.ThingInteraction.DamageMobj(thing, currentThing, currentThing.Target, damage);

                // Don't traverse any more.
                return false;
            }

            // Check for special pickup.
            if ((thing.Flags & MobjFlags.Special) != 0)
            {
                var solid = (thing.Flags & MobjFlags.Solid) != 0;
                if ((currentFlags & MobjFlags.PickUp) != 0)
                {
                    // Can remove thing.
                    world.ItemPickup.TouchSpecialThing(thing, currentThing);
                }
                return !solid;
            }

            return !BoomCollisionQuirks.BlocksGenericThing(
                currentFlags,
                thing.Flags,
                world.Options.Compatibility);
        }

        /// <summary>
        /// This is purely informative, nothing is modified
        /// (except things picked up).
        ///
        /// In:
        ///     A Mobj (can be valid or invalid)
        ///     A position to be checked
        ///     (doesn't need to be related to the mobj.X and Y)
        ///
        /// During:
        ///     Special things are touched if MobjFlags.PickUp
        ///     Early out on solid lines?
        ///
        /// Out:
        ///     New subsector
        ///     CurrentFloorZ
        ///     CurrentCeilingZ
        ///     CurrentDropoffZ
        ///     The lowest point contacted
        ///     (monsters won't move to a dropoff)
        ///     crossedSpecials[]
        ///     crossedSpecialCount
        /// </summary>
        public bool CheckPosition(Mobj thing, Fixed x, Fixed y)
        {
            var map = world.Map;
            var bm = map.BlockMap;

            currentThing = thing;
            currentFlags = thing.Flags;

            currentX = x;
            currentY = y;

            currentBox[Box.Top] = y + currentThing.Radius;
            currentBox[Box.Bottom] = y - currentThing.Radius;
            currentBox[Box.Right] = x + currentThing.Radius;
            currentBox[Box.Left] = x - currentThing.Radius;

            var newSubsector = Geometry.PointInSubsector(x, y, map);

            currentCeilingLine = null;
            currentBlockingLine = null;
            currentMbfBounceLine = null;

            // The base floor / ceiling is from the subsector that contains the point.
            // Any contacted lines the step closer together will adjust them.
            currentFloorZ = currentDropoffZ = newSubsector.Sector.FloorHeight;
            currentCeilingZ = newSubsector.Sector.CeilingHeight;

            var validCount = world.GetNewValidCount();

            crossedSpecialCount = 0;

            if ((currentFlags & MobjFlags.NoClip) != 0)
            {
                return true;
            }

            // Check things first, possibly picking things up.
            // The bounding box is extended by MaxThingRadius because mobj_ts are grouped into
            // mapblocks based on their origin point, and can overlap into adjacent blocks by up
            // to MaxThingRadius units.
            {
                var blockX1 = bm.GetBlockX(currentBox[Box.Left] - GameConst.MaxThingRadius);
                var blockX2 = bm.GetBlockX(currentBox[Box.Right] + GameConst.MaxThingRadius);
                var blockY1 = bm.GetBlockY(currentBox[Box.Bottom] - GameConst.MaxThingRadius);
                var blockY2 = bm.GetBlockY(currentBox[Box.Top] + GameConst.MaxThingRadius);

                for (var bx = blockX1; bx <= blockX2; bx++)
                {
                    for (var by = blockY1; by <= blockY2; by++)
                    {
                        if (!map.BlockMap.IterateThings(bx, by, checkThingFunc))
                        {
                            return false;
                        }
                    }
                }
            }

            // Check lines.
            {
                var blockX1 = bm.GetBlockX(currentBox[Box.Left]);
                var blockX2 = bm.GetBlockX(currentBox[Box.Right]);
                var blockY1 = bm.GetBlockY(currentBox[Box.Bottom]);
                var blockY2 = bm.GetBlockY(currentBox[Box.Top]);

                for (var bx = blockX1; bx <= blockX2; bx++)
                {
                    for (var by = blockY1; by <= blockY2; by++)
                    {
                        if (!map.BlockMap.IterateLines(bx, by, checkLineFunc, validCount))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Attempt to move to a new position, crossing special lines unless
        /// MobjFlags.Teleport is set.
        /// </summary>
        public bool TryMove(Mobj thing, Fixed x, Fixed y)
        {
            return TryMove(thing, x, y, ThingDropoffMode.Disallow);
        }

        internal bool TryMove(
            Mobj thing,
            Fixed x,
            Fixed y,
            ThingDropoffMode dropoffMode)
        {
            floatOk = false;
            fellDown = false;

            if (!CheckPosition(thing, x, y))
            {
                // Solid wall or thing.
                return false;
            }

            if ((thing.Flags & MobjFlags.NoClip) == 0)
            {
                if (currentCeilingZ - currentFloorZ < thing.Height)
                {
                    // Doesn't fit.
                    return false;
                }

                floatOk = true;

                if ((thing.Flags & MobjFlags.Teleport) == 0 &&
                    currentCeilingZ - thing.Z < thing.Height)
                {
                    // Mobj must lower itself to fit.
                    return false;
                }

                if ((thing.Flags & MobjFlags.Teleport) == 0 &&
                    currentFloorZ - thing.Z > Fixed.FromInt(24))
                {
                    // Too big a step up.
                    return false;
                }

                if ((thing.Flags & (MobjFlags.DropOff | MobjFlags.Float)) == 0)
                {
                    var compatibility = world.Options.Compatibility;
                    var mbfOptions = world.Options.MbfOptions;

                    // comp_ledgeblock adds an unconditional ground-monster
                    // ledge block. MBF21 temporarily disables it for the next
                    // XY move after scroller/pusher momentum is applied.
                    if (MbfLedgeBlockCompatibility.BlocksTallDropoff(
                            compatibility,
                            mbfOptions.CompLedgeBlock,
                            mbfOptions.HasCompLedgeBlockOverride,
                            thing.MbfScrollingMovement,
                            currentFloorZ,
                            currentDropoffZ))
                    {
                        return false;
                    }

                    // comp_dropoff restores Doom's old absolute ledge block.
                    // It overrides both momentum permission and dog jumping.
                    if (MbfDropoffCompatibility.UsesClassicBlocking(
                            compatibility,
                            mbfOptions.CompDropoff))
                    {
                        if (currentFloorZ - currentDropoffZ > MbfDropoffCompatibility.MaxStep)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        var targetedDropoffAllowed =
                            dropoffMode == ThingDropoffMode.Targeted128 &&
                            MbfDogJumping.AllowsTargetedDropoff(
                                thing,
                                currentFloorZ,
                                currentDropoffZ);

                        var externalMomentumRequested =
                            dropoffMode == ThingDropoffMode.Allow;
                        var momentumDropoffAllowed =
                            BoomMomentumDropoff.Allows(
                                compatibility,
                                externalMomentumRequested) ||
                            MbfDropoffCompatibility.AllowsExternalMomentumDropoff(
                                compatibility,
                                mbfOptions.CompDropoff,
                                externalMomentumRequested);

                        if (!targetedDropoffAllowed &&
                            !momentumDropoffAllowed &&
                            MbfMonkeyClimbing.BlocksMove(
                                compatibility,
                                mbfOptions.Monkeys,
                                thing,
                                currentFloorZ,
                                currentDropoffZ))
                        {
                            // Voluntary movement still obeys the normal ledge
                            // rule. monkeys only changes how that rule compares
                            // the old and new floor/dropoff edges.
                            return false;
                        }

                        if ((targetedDropoffAllowed || momentumDropoffAllowed) &&
                            (thing.Flags & MobjFlags.NoGravity) == 0 &&
                            thing.Z - currentFloorZ > MbfDropoffCompatibility.MaxStep)
                        {
                            // Leave the old Z in place so gravity performs the
                            // fall rather than snapping the actor downward.
                            fellDown = true;
                        }
                    }
                }
            }

            // The move is ok,
            // so link the thing into its new position.
            UnsetThingPosition(thing);

            var oldx = thing.X;
            var oldy = thing.Y;
            thing.FloorZ = currentFloorZ;
            thing.DropoffZ = currentDropoffZ;
            thing.CeilingZ = currentCeilingZ;
            thing.X = x;
            thing.Y = y;

            SetThingPosition(thing);

            // If any special lines were hit, do the effect.
            if ((thing.Flags & (MobjFlags.Teleport | MobjFlags.NoClip)) == 0)
            {
                while (crossedSpecialCount-- > 0)
                {
                    // See if the line was crossed.
                    var line = crossedSpecials[crossedSpecialCount];
                    var newSide = Geometry.PointOnLineSide(thing.X, thing.Y, line);
                    var oldSide = Geometry.PointOnLineSide(oldx, oldy, line);
                    if (newSide != oldSide)
                    {
                        if (line.Special != 0)
                        {
                            world.MapInteraction.CrossSpecialLine(line, oldSide, thing);
                        }
                    }
                }
            }

            return true;
        }


        private static readonly Fixed stopSpeed = new Fixed(0x1000);
        private static readonly Fixed friction = new Fixed(0xe800);

        public void XYMovement(Mobj thing)
        {
            if (thing.MomX == Fixed.Zero && thing.MomY == Fixed.Zero)
            {
                if ((thing.Flags & MobjFlags.SkullFly) != 0)
                {
                    // The skull slammed into something.
                    thing.Flags &= ~MobjFlags.SkullFly;
                    thing.MomX = thing.MomY = thing.MomZ = Fixed.Zero;

                    thing.SetState(thing.Info.SpawnState);
                }

                return;
            }

            var player = thing.Player;

            if (thing.MomX > maxMove)
            {
                thing.MomX = maxMove;
            }
            else if (thing.MomX < -maxMove)
            {
                thing.MomX = -maxMove;
            }

            if (thing.MomY > maxMove)
            {
                thing.MomY = maxMove;
            }
            else if (thing.MomY < -maxMove)
            {
                thing.MomY = -maxMove;
            }

            var moveX = thing.MomX;
            var moveY = thing.MomY;
            var oldX = thing.X;
            var oldY = thing.Y;
            var halfMaxMove = maxMove / 2;

            do
            {
                Fixed pMoveX;
                Fixed pMoveY;

                if (moveX > halfMaxMove ||
                    moveY > halfMaxMove ||
                    MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
                        moveX,
                        moveY,
                        halfMaxMove,
                        world.Options.Compatibility,
                        world.Options.MbfOptions.CompMoveBlock))
                {
                    pMoveX = thing.X + moveX / 2;
                    pMoveY = thing.Y + moveY / 2;
                    moveX >>= 1;
                    moveY >>= 1;
                }
                else
                {
                    pMoveX = thing.X + moveX;
                    pMoveY = thing.Y + moveY;
                    moveX = moveY = Fixed.Zero;
                }

                if (!TryMove(thing, pMoveX, pMoveY, ThingDropoffMode.Allow))
                {
                    // MBF BOUNCES reflection takes precedence over player
                    // sliding for non-missile bouncers.
                    if (MbfBounceCompatibility.IsBouncer(world.Options.Compatibility, thing) &&
                        (thing.Flags & MobjFlags.Missile) == 0)
                    {
                        MbfBounceCompatibility.BounceFromLine(thing, currentMbfBounceLine);
                    }
                    else if (thing.Player != null)
                    {   // Try to slide along it.
                        SlideMove(thing);
                    }
                    else if ((thing.Flags & MobjFlags.Missile) != 0)
                    {
                        // Explode a missile.
                        if (currentCeilingLine != null &&
                            currentCeilingLine.BackSector != null &&
                            currentCeilingLine.BackSector.CeilingFlat == world.Map.SkyFlatNumber)
                        {
                            // Hack to prevent missiles exploding against the sky.
                            // Does not handle sky floors.
                            world.ThingAllocation.RemoveMobj(thing);
                            return;
                        }
                        world.ThingInteraction.ExplodeMissile(thing);
                    }
                    else
                    {
                        thing.MomX = thing.MomY = Fixed.Zero;
                    }
                }
            }
            while (moveX != Fixed.Zero || moveY != Fixed.Zero);

            // Slow down.
            if (player != null && (player.Cheats & CheatFlags.NoMomentum) != 0)
            {
                // Debug option for no sliding at all.
                thing.MomX = thing.MomY = Fixed.Zero;
                if (player.Mobj == thing)
                {
                    MbfPlayerBobbing.Stop(player, world.Options.Compatibility);
                }
                return;
            }

            if ((thing.Flags & (MobjFlags.Missile | MobjFlags.SkullFly)) != 0)
            {
                // No friction for missiles ever.
                return;
            }

            if (thing.Z > thing.FloorZ)
            {
                // No friction when airborne.
                return;
            }

            if (MbfBounceCompatibility.PreservesLedgeMomentum(
                    world.Options.Compatibility,
                    thing) ||
                (thing.Flags & MobjFlags.Corpse) != 0 ||
                thing.BoomLedgeFalling)
            {
                // Boom also preserves momentum for inert objects currently
                // being pushed off a ledge by P_ApplyTorque. Without this,
                // small torque impulses are killed by normal floor friction
                // before the object's radius clears the higher floor.
                if (thing.MomX > Fixed.One / 4 ||
                    thing.MomX < -Fixed.One / 4 ||
                    thing.MomY > Fixed.One / 4 ||
                    thing.MomY < -Fixed.One / 4)
                {
                    if (thing.FloorZ != thing.Subsector.Sector.FloorHeight)
                    {
                        return;
                    }
                }
            }

            if (thing.MomX > -stopSpeed &&
                thing.MomX < stopSpeed &&
                thing.MomY > -stopSpeed &&
                thing.MomY < stopSpeed &&
                (player == null ||
                 (player.Cmd.ForwardMove == 0 && player.Cmd.SideMove == 0) ||
                 MbfVoodooScrollerCompatibility.ShouldForceStop(
                     thing,
                     world.Options.Compatibility,
                     world.Options.MbfOptions.CompVoodooScroller,
                     world.Options.MbfOptions.HasCompVoodooScrollerOverride)))
            {
                // If in a walking frame, stop moving.
                if (player != null && (player.Mobj.State.Number - (int)MobjState.PlayRun1) < 4)
                {
                    player.Mobj.SetState(MobjState.Play);
                }

                thing.MomX = Fixed.Zero;
                thing.MomY = Fixed.Zero;
                if (player != null && player.Mobj == thing)
                {
                    MbfPlayerBobbing.Stop(player, world.Options.Compatibility);
                }
            }
            else
            {
                var frictionFactor = friction;
                if (player != null && GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility))
                {
                    frictionFactor = BoomMovementQuirks.GetCoastingFriction(
                        thing, oldX, oldY, world.Options.Compatibility);
                }
                else if (MbfMonsterFriction.Applies(
                             world.Options.Compatibility,
                             world.Options.MbfOptions.MonsterFriction,
                             thing))
                {
                    frictionFactor = MbfMonsterFriction.GetCoastingFriction(
                        world.Options.Compatibility,
                        world.Options.MbfOptions.MonsterFriction,
                        thing);
                }
                else if (MbfMonsterFriction.AppliesToCorpseCoasting(
                             world.Options.Compatibility,
                             world.Options.MbfOptions.MonsterFriction,
                             thing))
                {
                    // MBF's floor friction still controls residual momentum after
                    // a monster becomes a corpse. On full ice this preserves the
                    // death impulse until the corpse reaches ordinary floor.
                    frictionFactor = MbfMonsterFriction.GetCorpseCoastingFriction(
                        world.Options.Compatibility,
                        world.Options.MbfOptions.MonsterFriction,
                        thing);
                }

                thing.MomX = thing.MomX * frictionFactor;
                thing.MomY = thing.MomY * frictionFactor;

                if (player != null && player.Mobj == thing)
                {
                    // Boom 2.02 and later decay bob momentum at ordinary Doom
                    // friction even while physical momentum coasts on an icy floor.
                    MbfPlayerBobbing.ApplyOriginalFriction(
                        player, world.Options.Compatibility);
                }
            }
        }

        public void ZMovement(Mobj thing)
        {
            if (MbfBounceCompatibility.IsBouncer(world.Options.Compatibility, thing) &&
                thing.MomZ != Fixed.Zero)
            {
                BouncerZMovement(thing);
                return;
            }

            // Check for smooth step up.
            if (thing.Player != null && thing.Z < thing.FloorZ)
            {
                thing.Player.ViewHeight -= thing.FloorZ - thing.Z;

                thing.Player.DeltaViewHeight =
                    (Player.NormalViewHeight - thing.Player.ViewHeight) >> 3;
            }

            // Adjust height.
            thing.Z += thing.MomZ;

            if ((thing.Flags & MobjFlags.Float) != 0 && thing.Target != null)
            {
                // Float down towards target if too close.
                if ((thing.Flags & MobjFlags.SkullFly) == 0 &&
                    (thing.Flags & MobjFlags.InFloat) == 0)
                {
                    var dist = Geometry.AproxDistance(
                        thing.X - thing.Target.X,
                        thing.Y - thing.Target.Y);

                    var delta = (thing.Target.Z + (thing.Height >> 1)) - thing.Z;

                    if (delta < Fixed.Zero && dist < -(delta * 3))
                    {
                        thing.Z -= FloatSpeed;
                    }
                    else if (delta > Fixed.Zero && dist < (delta * 3))
                    {
                        thing.Z += FloatSpeed;
                    }
                }
            }

            // Clip movement.
            if (thing.Z <= thing.FloorZ)
            {
                // Hit the floor.

                //
                // The lost soul bounce fix below is based on Chocolate Doom's implementation.
                //

                var correctLostSoulBounce = MbfLostSoulCompatibility.UsesCorrectBounce(
                    world.Options.Compatibility,
                    world.Options.GameVersion,
                    world.Options.MbfOptions.CompSoul);

                if (correctLostSoulBounce && (thing.Flags & MobjFlags.SkullFly) != 0)
                {
                    // The skull slammed into something.
                    thing.MomZ = -thing.MomZ;
                }

                if (thing.MomZ < Fixed.Zero)
                {
                    if (MbfTouchyCompatibility.ShouldActivateOnFloorImpact(
                            world.Options.Compatibility,
                            thing,
                            thing.MomZ))
                    {
                        world.ThingInteraction.DamageMobj(thing, null, null, thing.Health);
                    }
                    else if (thing.Player != null && thing.MomZ < -gravity * 8)
                    {
                        // Squat down.
                        // Decrease viewheight for a moment after hitting the ground (hard),
                        // and utter appropriate sound.
                        thing.Player.DeltaViewHeight = (thing.MomZ >> 3);

                        if (MbfSoundCompatibility.ShouldPlayHardLandingSound(
                            world.Options.Compatibility,
                            world.Options.MbfOptions.CompSound,
                            thing.Health))
                        {
                            world.StartSound(thing, Sfx.OOF, SfxType.Voice);
                        }
                    }
                    thing.MomZ = Fixed.Zero;
                }
                thing.Z = thing.FloorZ;

                if (!correctLostSoulBounce &&
                    (thing.Flags & MobjFlags.SkullFly) != 0)
                {
                    thing.MomZ = -thing.MomZ;
                }

                if ((thing.Flags & MobjFlags.Missile) != 0 &&
                    (thing.Flags & MobjFlags.NoClip) == 0)
                {
                    world.ThingInteraction.ExplodeMissile(thing);
                    return;
                }
            }
            else if ((thing.Flags & MobjFlags.NoGravity) == 0)
            {
                if (thing.MomZ == Fixed.Zero)
                {
                    thing.MomZ = -gravity * 2;
                }
                else
                {
                    thing.MomZ -= gravity;
                }
            }

            if (thing.Z + thing.Height > thing.CeilingZ)
            {
                // Hit the ceiling.
                var correctLostSoulBounce = MbfLostSoulCompatibility.UsesCorrectBounce(
                    world.Options.Compatibility,
                    world.Options.GameVersion,
                    world.Options.MbfOptions.CompSoul);

                if (correctLostSoulBounce &&
                    (thing.Flags & MobjFlags.SkullFly) != 0)
                {
                    thing.MomZ = -thing.MomZ;
                }

                if (thing.MomZ > Fixed.Zero)
                {
                    thing.MomZ = Fixed.Zero;
                }

                thing.Z = thing.CeilingZ - thing.Height;

                if (!correctLostSoulBounce &&
                    (thing.Flags & MobjFlags.SkullFly) != 0)
                {
                    thing.MomZ = -thing.MomZ;
                }

                if ((thing.Flags & MobjFlags.Missile) != 0 &&
                    (thing.Flags & MobjFlags.NoClip) == 0)
                {
                    world.ThingInteraction.ExplodeMissile(thing);
                    return;
                }
            }
        }


        private void BouncerZMovement(Mobj thing)
        {
            thing.Z += thing.MomZ;

            if (thing.Z <= thing.FloorZ)
            {
                thing.Z = thing.FloorZ;

                if (thing.MomZ < Fixed.Zero)
                {
                    var incomingMomentumZ = thing.MomZ;
                    thing.MomZ = -thing.MomZ;
                    thing.MomZ = MbfBounceCompatibility.ApplyFloorDecay(
                        thing,
                        thing.MomZ,
                        gravity);

                    if (MbfTouchyCompatibility.ShouldActivateOnFloorImpact(
                            world.Options.Compatibility,
                            thing,
                            incomingMomentumZ))
                    {
                        world.ThingInteraction.DamageMobj(thing, null, null, thing.Health);
                    }
                    else
                    {
                        AdjustFloatingActor(thing);
                    }

                    return;
                }
            }
            else if (thing.Z >= thing.CeilingZ - thing.Height)
            {
                thing.Z = thing.CeilingZ - thing.Height;

                if (thing.MomZ > Fixed.Zero)
                {
                    var skyCeiling = thing.Subsector.Sector.CeilingFlat == world.Map.SkyFlatNumber;

                    if (!skyCeiling || (thing.Flags & MobjFlags.NoGravity) != 0)
                    {
                        thing.MomZ = -thing.MomZ;
                    }
                    else if ((thing.Flags & MobjFlags.Missile) != 0)
                    {
                        world.ThingAllocation.RemoveMobj(thing);
                        return;
                    }

                    AdjustFloatingActor(thing);
                    return;
                }
            }
            else
            {
                if ((thing.Flags & MobjFlags.NoGravity) == 0)
                {
                    thing.MomZ -= MbfBounceCompatibility.GetVerticalGravityStep(thing, gravity);
                }

                AdjustFloatingActor(thing);
                return;
            }

            // Came to rest. Keep the ordinary missile/no-clip handling below
            // isolated from non-missile MBF bouncers.
            thing.MomZ = Fixed.Zero;

            if ((thing.Flags & MobjFlags.Missile) != 0 &&
                (thing.Flags & MobjFlags.NoClip) == 0)
            {
                world.ThingInteraction.ExplodeMissile(thing);
                return;
            }

            AdjustFloatingActor(thing);
        }

        private static void AdjustFloatingActor(Mobj thing)
        {
            if ((thing.Flags & MobjFlags.Float) == 0 ||
                (thing.Flags & (MobjFlags.SkullFly | MobjFlags.InFloat)) != 0 ||
                thing.Target == null ||
                !BoomLedgeTorque.IsSentient(thing))
            {
                return;
            }

            var dist = Geometry.AproxDistance(
                thing.X - thing.Target.X,
                thing.Y - thing.Target.Y);
            var delta = (thing.Target.Z + (thing.Height >> 1)) - thing.Z;

            if (delta < Fixed.Zero && dist < -(delta * 3))
                thing.Z -= FloatSpeed;
            else if (delta > Fixed.Zero && dist < delta * 3)
                thing.Z += FloatSpeed;
        }

        public Fixed CurrentFloorZ => currentFloorZ;
        public Fixed CurrentCeilingZ => currentCeilingZ;
        public void UpdateBoomLedgeTorqueAtRest(Mobj thing)
        {
            boomLedgeTorque.UpdateAtRest(thing);
        }

        public Fixed CurrentDropoffZ => currentDropoffZ;
        public LineDef BlockingLine => currentBlockingLine;
        public bool FloatOk => floatOk;

        public bool FellDown => fellDown;



        ////////////////////////////////////////////////////////////
        // Player's slide movement
        ////////////////////////////////////////////////////////////

        private Fixed bestSlideFrac;
        private Fixed secondSlideFrac;

        private LineDef bestSlideLine;
        private LineDef secondSlideLine;

        private Mobj slideThing;
        private Fixed slideMoveX;
        private Fixed slideMoveY;

        private Func<Intercept, bool> slideTraverseFunc;

        private void InitSlideMovement()
        {
            slideTraverseFunc = SlideTraverse;
        }

        /// <summary>
        /// Adjusts the x and y movement so that the next move will
        /// slide along the wall.
        /// </summary>
        private void HitSlideLine(LineDef line)
        {
            if (line.SlopeType == SlopeType.Horizontal)
            {
                slideMoveY = Fixed.Zero;
                return;
            }

            if (line.SlopeType == SlopeType.Vertical)
            {
                slideMoveX = Fixed.Zero;
                return;
            }

            var side = Geometry.PointOnLineSide(slideThing.X, slideThing.Y, line);

            var lineAngle = Geometry.PointToAngle(Fixed.Zero, Fixed.Zero, line.Dx, line.Dy);
            if (side == 1)
            {
                lineAngle += Angle.Ang180;
            }

            var moveAngle = Geometry.PointToAngle(Fixed.Zero, Fixed.Zero, slideMoveX, slideMoveY);

            var deltaAngle = moveAngle - lineAngle;
            if (deltaAngle > Angle.Ang180)
            {
                deltaAngle += Angle.Ang180;
            }

            var moveDist = Geometry.AproxDistance(slideMoveX, slideMoveY);
            var newDist = moveDist * Trig.Cos(deltaAngle);

            slideMoveX = newDist * Trig.Cos(lineAngle);
            slideMoveY = newDist * Trig.Sin(lineAngle);
        }

        private bool SlideTraverse(Intercept intercept)
        {
            var mc = world.MapCollision;

            if (intercept.Line == null)
            {
                throw new Exception("ThingMovement.SlideTraverse: Not a line?");
            }

            var line = intercept.Line;

            if ((line.Flags & LineFlags.TwoSided) == 0)
            {
                if (Geometry.PointOnLineSide(slideThing.X, slideThing.Y, line) != 0)
                {
                    // Don't hit the back side.
                    return true;
                }

                goto isBlocking;
            }

            // Set openrange, opentop, openbottom.
            mc.LineOpening(line);

            if (mc.OpenRange < slideThing.Height)
            {
                // Doesn't fit.
                goto isBlocking;
            }

            if (mc.OpenTop - slideThing.Z < slideThing.Height)
            {
                // Mobj is too high.
                goto isBlocking;
            }

            if (mc.OpenBottom - slideThing.Z > Fixed.FromInt(24))
            {
                // Too big a step up.
                goto isBlocking;
            }

            // This line doesn't block movement.
            return true;

            // The line does block movement, see if it is closer than best so far.
            isBlocking:
            if (intercept.Frac < bestSlideFrac)
            {
                secondSlideFrac = bestSlideFrac;
                secondSlideLine = bestSlideLine;
                bestSlideFrac = intercept.Frac;
                bestSlideLine = line;
            }

            // Stop.
            return false;
        }

        /// <summary>
        /// The MomX / MomY move is bad, so try to slide along a wall.
        /// Find the first line hit, move flush to it, and slide along it.
        /// This is a kludgy mess.
        /// </summary>
        private void SlideMove(Mobj thing)
        {
            var pt = world.PathTraversal;

            slideThing = thing;

            var hitCount = 0;

            retry:
            // Don't loop forever.
            if (++hitCount == 3)
            {
                // The move most have hit the middle, so stairstep.
                StairStep(thing);
                return;
            }

            Fixed leadX;
            Fixed leadY;
            Fixed trailX;
            Fixed trailY;

            // Trace along the three leading corners.
            if (thing.MomX > Fixed.Zero)
            {
                leadX = thing.X + thing.Radius;
                trailX = thing.X - thing.Radius;
            }
            else
            {
                leadX = thing.X - thing.Radius;
                trailX = thing.X + thing.Radius;
            }

            if (thing.MomY > Fixed.Zero)
            {
                leadY = thing.Y + thing.Radius;
                trailY = thing.Y - thing.Radius;
            }
            else
            {
                leadY = thing.Y - thing.Radius;
                trailY = thing.Y + thing.Radius;
            }

            bestSlideFrac = Fixed.OnePlusEpsilon;

            pt.PathTraverse(
                leadX, leadY, leadX + thing.MomX, leadY + thing.MomY,
                PathTraverseFlags.AddLines, slideTraverseFunc);

            pt.PathTraverse(
                trailX, leadY, trailX + thing.MomX, leadY + thing.MomY,
                PathTraverseFlags.AddLines, slideTraverseFunc);

            pt.PathTraverse(
                leadX, trailY, leadX + thing.MomX, trailY + thing.MomY,
                PathTraverseFlags.AddLines, slideTraverseFunc);

            // Move up to the wall.
            if (bestSlideFrac == Fixed.OnePlusEpsilon)
            {
                // The move most have hit the middle, so stairstep.
                StairStep(thing);
                return;
            }

            // Fudge a bit to make sure it doesn't hit.
            bestSlideFrac = new Fixed(bestSlideFrac.Data - 0x800);
            if (bestSlideFrac > Fixed.Zero)
            {
                var newX = thing.MomX * bestSlideFrac;
                var newY = thing.MomY * bestSlideFrac;

                if (!TryMove(thing, thing.X + newX, thing.Y + newY, ThingDropoffMode.Allow))
                {
                    // The move most have hit the middle, so stairstep.
                    StairStep(thing);
                    return;
                }
            }

            // Now continue along the wall.
            // First calculate remainder.
            bestSlideFrac = new Fixed(Fixed.FracUnit - (bestSlideFrac.Data + 0x800));

            if (bestSlideFrac > Fixed.One)
            {
                bestSlideFrac = Fixed.One;
            }

            if (bestSlideFrac <= Fixed.Zero)
            {
                return;
            }

            slideMoveX = thing.MomX * bestSlideFrac;
            slideMoveY = thing.MomY * bestSlideFrac;

            // Clip the moves.
            HitSlideLine(bestSlideLine);

            thing.MomX = slideMoveX;
            thing.MomY = slideMoveY;

            var slidePlayer = thing.Player;
            if (slidePlayer != null &&
                slidePlayer.Mobj == thing &&
                MbfPlayerBobbing.Applies(world.Options.Compatibility))
            {
                if (Fixed.Abs(slidePlayer.BobMomX) > Fixed.Abs(slideMoveX))
                    slidePlayer.BobMomX = slideMoveX;
                if (Fixed.Abs(slidePlayer.BobMomY) > Fixed.Abs(slideMoveY))
                    slidePlayer.BobMomY = slideMoveY;
            }

            if (!TryMove(thing, thing.X + slideMoveX, thing.Y + slideMoveY, ThingDropoffMode.Allow))
            {
                goto retry;
            }
        }

        private void StairStep(Mobj thing)
        {
            if (!TryMove(thing, thing.X, thing.Y + thing.MomY, ThingDropoffMode.Allow))
            {
                TryMove(thing, thing.X + thing.MomX, thing.Y, ThingDropoffMode.Allow);
            }
        }



        ////////////////////////////////////////////////////////////
        // Teleport movement
        ////////////////////////////////////////////////////////////

        private Func<Mobj, bool> stompThingFunc;
        private bool teleportCanStomp;

        private void InitTeleportMovement()
        {
            stompThingFunc = StompThing;
        }

        private bool StompThing(Mobj thing)
        {
            if ((thing.Flags & MobjFlags.Shootable) == 0)
            {
                return true;
            }

            var blockDist = thing.Radius + currentThing.Radius;
            var dx = Fixed.Abs(thing.X - currentX);
            var dy = Fixed.Abs(thing.Y - currentY);
            if (dx >= blockDist || dy >= blockDist)
            {
                // Didn't hit it.
                return true;
            }

            // Don't clip against self.
            if (thing == currentThing)
            {
                return true;
            }

            if (!teleportCanStomp)
            {
                return false;
            }

            world.ThingInteraction.DamageMobj(thing, currentThing, currentThing, 10000);

            return true;
        }

        public bool TeleportMove(Mobj thing, Fixed x, Fixed y, bool bossTeleport = false)
        {
            // Kill anything occupying the position.
            currentThing = thing;
            currentFlags = thing.Flags;
            teleportCanStomp = MbfTelefragCompatibility.CanTelefragAtDestination(
                thing,
                bossTeleport,
                world.Options.Map,
                world.Options.Compatibility,
                world.Options.MbfOptions.CompTelefrag);

            currentX = x;
            currentY = y;

            currentBox[Box.Top] = y + currentThing.Radius;
            currentBox[Box.Bottom] = y - currentThing.Radius;
            currentBox[Box.Right] = x + currentThing.Radius;
            currentBox[Box.Left] = x - currentThing.Radius;

            var ss = Geometry.PointInSubsector(x, y, world.Map);

            currentCeilingLine = null;

            // The base floor / ceiling is from the subsector that contains the point.
            // Any contacted lines the step closer together will adjust them.
            currentFloorZ = currentDropoffZ = ss.Sector.FloorHeight;
            currentCeilingZ = ss.Sector.CeilingHeight;

            var validcount = world.GetNewValidCount();

            crossedSpecialCount = 0;

            // Stomp on any things contacted.
            var bm = world.Map.BlockMap;
            var blockX1 = bm.GetBlockX(currentBox[Box.Left] - GameConst.MaxThingRadius);
            var blockX2 = bm.GetBlockX(currentBox[Box.Right] + GameConst.MaxThingRadius);
            var blockY1 = bm.GetBlockY(currentBox[Box.Bottom] - GameConst.MaxThingRadius);
            var blockY2 = bm.GetBlockY(currentBox[Box.Top] + GameConst.MaxThingRadius);

            for (var bx = blockX1; bx <= blockX2; bx++)
            {
                for (var by = blockY1; by <= blockY2; by++)
                {
                    if (!bm.IterateThings(bx, by, stompThingFunc))
                    {
                        return false;
                    }
                }
            }

            // the move is ok, so link the thing into its new position
            UnsetThingPosition(thing);

            thing.FloorZ = currentFloorZ;
            thing.DropoffZ = currentDropoffZ;
            thing.CeilingZ = currentCeilingZ;
            thing.X = x;
            thing.Y = y;

            SetThingPosition(thing);

            return true;
        }
    }
}
