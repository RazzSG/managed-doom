using ManagedDoom.Compatibility.Mbf.Gameplay;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomLineSpecials
{
    public static bool TryUse(World world, LineDef line, int side, Mobj thing, out bool result)
    {
        result = false;

        if (BoomTeleportTranslator.TryTranslate(line.Special, out var teleport))
        {
            if (!BoomTriggerSemantics.CanActivateFromSwitchUse(teleport.Trigger, side))
                return true;

            if (thing?.Player == null && (line.Flags & LineFlags.Secret) != 0)
                return true;

            if (!CanActivateTeleport(line, thing, teleport))
                return true;

            if (world.SectorAction.DoBoomTeleport(line, side, thing, teleport))
                BoomTriggerLifecycle.ApplySuccess(world, line, teleport.Trigger);

            result = true;
            return true;
        }

        if (BoomDonutTranslator.TryTranslate(line.Special, out var donut))
        {
            if (!BoomTriggerSemantics.CanActivateFromSwitchUse(donut.Trigger, side))
                return true;

            if (!CanActivateDonut(line, thing))
                return true;

            if (world.SectorAction.DoDonut(line))
                BoomTriggerLifecycle.ApplySuccess(world, line, donut.Trigger);

            result = true;
            return true;
        }

        if (BoomPlatformTranslator.TryTranslate(line.Special, out var platform))
        {
            if (!BoomTriggerSemantics.CanActivateFromSwitchUse(platform.Trigger, side))
                return true;

            if (!CanActivatePlatform(line, thing))
                return true;

            var activated = world.SectorAction.DoBoomPlatform(line, platform);
            if (activated || ShouldApplyPlatformTriggerWithoutAction(platform))
                BoomTriggerLifecycle.ApplySuccess(world, line, platform.Trigger);

            result = true;
            return true;
        }

        if (BoomExtendedCrusherTranslator.TryTranslate(line.Special, out var extendedCrusher))
        {
            if (!BoomTriggerSemantics.CanActivateFromSwitchUse(extendedCrusher.Trigger, side))
                return true;

            if (!CanActivateExtendedCrusher(line, thing))
                return true;

            if (world.SectorAction.DoBoomExtendedCrusher(line, extendedCrusher))
                BoomTriggerLifecycle.ApplySuccess(world, line, extendedCrusher.Trigger);

            result = true;
            return true;
        }

        if (BoomExtendedStairTranslator.TryTranslate(line.Special, out var extendedStair))
        {
            if (!BoomTriggerSemantics.CanActivateFromSwitchUse(extendedStair.Trigger, side))
                return true;

            if (!CanActivateExtendedStair(line, thing))
                return true;

            if (DoExtendedStairs(world, line, extendedStair))
                BoomTriggerLifecycle.ApplySuccess(world, line, extendedStair.Trigger);

            result = true;
            return true;
        }

        if (BoomDelayedDoorTranslator.TryTranslate(line.Special, out var delayedDoor))
        {
            if (!BoomTriggerSemantics.CanActivateFromSwitchUse(delayedDoor.Trigger, side))
                return true;

            if (!CanActivateDelayedDoor(line, thing))
                return true;

            if (world.SectorAction.DoDoor(line, VerticalDoorType.Close30ThenOpen))
                BoomTriggerLifecycle.ApplySuccess(world, line, delayedDoor.Trigger);

            result = true;
            return true;
        }

        if (BoomLightingTranslator.TryTranslate(line.Special, out var lighting))
        {
            if (!BoomTriggerSemantics.CanActivateFromSwitchUse(lighting.Trigger, side))
                return true;

            if (!CanActivateLighting(thing))
                return true;

            if (world.SectorAction.DoBoomLighting(line, lighting))
                BoomTriggerLifecycle.ApplySuccess(world, line, lighting.Trigger);

            result = true;
            return true;
        }

        if (BoomElevatorTranslator.TryTranslate(line.Special, out var elevator))
        {
            if (!BoomTriggerSemantics.CanActivateFromSwitchUse(elevator.Trigger, side))
                return true;

            if (!CanActivateElevator(line, thing))
                return true;

            if (world.SectorAction.DoBoomElevator(line, elevator))
                BoomTriggerLifecycle.ApplySuccess(world, line, elevator.Trigger);

            result = true;
            return true;
        }

        if (BoomExtendedFloorTranslator.TryTranslate(line.Special, out var extendedFloor))
        {
            if (!BoomTriggerSemantics.CanActivateFromSwitchUse(extendedFloor.Trigger, side))
                return true;

            if (!CanActivateExtendedPlane(line, thing))
                return true;

            if (world.SectorAction.DoBoomFloor(line, extendedFloor))
                BoomTriggerLifecycle.ApplySuccess(world, line, extendedFloor.Trigger);

            result = true;
            return true;
        }

        if (BoomExtendedCeilingTranslator.TryTranslate(line.Special, out var extendedCeiling))
        {
            if (!BoomTriggerSemantics.CanActivateFromSwitchUse(extendedCeiling.Trigger, side))
                return true;

            if (!CanActivateExtendedPlane(line, thing))
                return true;

            if (world.SectorAction.DoBoomCeiling(line, extendedCeiling))
                BoomTriggerLifecycle.ApplySuccess(world, line, extendedCeiling.Trigger);

            result = true;
            return true;
        }

        if (BoomFloorTranslator.TryTranslate(line.Special, out var floor))
        {
            if (!BoomTriggerSemantics.CanActivateFromUse(floor.Trigger, side))
                return true;

            if (!CanActivate(line, thing, floor.Common))
                return true;

            var activated = world.SectorAction.DoBoomFloor(line, floor);
            if (activated)
                BoomTriggerLifecycle.ApplySuccess(world, line, floor.Trigger);

            result = true;
            return true;
        }

        if (BoomCeilingTranslator.TryTranslate(line.Special, out var ceiling))
        {
            if (!BoomTriggerSemantics.CanActivateFromUse(ceiling.Trigger, side))
                return true;

            if (!CanActivate(line, thing, ceiling.Common))
                return true;

            var activated = world.SectorAction.DoBoomCeiling(line, ceiling);
            if (activated)
                BoomTriggerLifecycle.ApplySuccess(world, line, ceiling.Trigger);

            result = true;
            return true;
        }

        if (BoomLockedDoorTranslator.TryTranslate(line.Special, out var lockedDoor))
        {
            if (!BoomTriggerSemantics.CanActivateFromUse(lockedDoor.Trigger, side))
                return true;

            if (!CanActivateLockedDoor(world, line, thing, lockedDoor))
                return true;

            var activated = world.SectorAction.DoBoomLockedDoor(line, lockedDoor);
            if (activated)
                BoomTriggerLifecycle.ApplySuccess(world, line, lockedDoor.Trigger);

            result = true;
            return true;
        }

        if (BoomDoorTranslator.TryTranslate(line.Special, out var door))
        {
            if (!BoomTriggerSemantics.CanActivateFromUse(door.Trigger, side))
                return true;

            if (!CanActivateDoor(line, thing, door))
                return true;

            var activated = world.SectorAction.DoBoomDoor(line, door);
            if (activated)
                BoomTriggerLifecycle.ApplySuccess(world, line, door.Trigger);

            result = true;
            return true;
        }

        if (BoomLiftTranslator.TryTranslate(line.Special, out var lift))
        {
            if (!BoomTriggerSemantics.CanActivateFromUse(lift.Trigger, side))
                return true;

            if (!CanActivate(line, thing, lift.Common))
                return true;

            var activated = world.SectorAction.DoBoomLift(line, lift);
            if (activated)
                BoomTriggerLifecycle.ApplySuccess(world, line, lift.Trigger);

            result = true;
            return true;
        }

        if (BoomStairTranslator.TryTranslate(line.Special, out var stair))
        {
            if (!BoomTriggerSemantics.CanActivateFromUse(stair.Trigger, side))
                return true;

            if (!CanActivate(line, thing, stair.Common))
                return true;

            var activated = world.SectorAction.DoBoomStairs(line, stair);
            if (activated)
                BoomTriggerLifecycle.ApplySuccess(world, line, stair.Trigger);

            result = true;
            return true;
        }

        if (BoomCrusherTranslator.TryTranslate(line.Special, out var crusher))
        {
            if (!BoomTriggerSemantics.CanActivateFromUse(crusher.Trigger, side))
                return true;

            if (!CanActivate(line, thing, crusher.Common))
                return true;

            var activated = world.SectorAction.DoBoomCrusher(line, crusher);
            if (activated)
                BoomTriggerLifecycle.ApplySuccess(world, line, crusher.Trigger);

            result = true;
            return true;
        }

        return false;
    }

    public static bool TryCross(World world, LineDef line, int side, Mobj thing)
    {
        if (BoomTeleportTranslator.TryTranslate(line.Special, out var teleport))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(teleport.Trigger) || !CanActivateTeleport(line, thing, teleport))
                return true;

            if (world.SectorAction.DoBoomTeleport(line, side, thing, teleport))
                BoomTriggerLifecycle.ApplySuccess(world, line, teleport.Trigger);

            return true;
        }

        if (BoomDonutTranslator.TryTranslate(line.Special, out var donut))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(donut.Trigger) || !CanActivateDonut(line, thing))
                return true;

            if (world.SectorAction.DoDonut(line))
                BoomTriggerLifecycle.ApplySuccess(world, line, donut.Trigger);

            return true;
        }

        if (BoomPlatformTranslator.TryTranslate(line.Special, out var platform))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(platform.Trigger) || !CanActivatePlatform(line, thing))
                return true;

            if (world.SectorAction.DoBoomPlatform(line, platform))
                BoomTriggerLifecycle.ApplySuccess(world, line, platform.Trigger);

            return true;
        }

        if (BoomExtendedCrusherTranslator.TryTranslate(line.Special, out var extendedCrusher))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(extendedCrusher.Trigger) || !CanActivateExtendedCrusher(line, thing))
                return true;

            if (world.SectorAction.DoBoomExtendedCrusher(line, extendedCrusher))
                BoomTriggerLifecycle.ApplySuccess(world, line, extendedCrusher.Trigger);

            return true;
        }

        if (BoomExtendedStairTranslator.TryTranslate(line.Special, out var extendedStair))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(extendedStair.Trigger) || !CanActivateExtendedStair(line, thing))
                return true;

            if (DoExtendedStairs(world, line, extendedStair))
                BoomTriggerLifecycle.ApplySuccess(world, line, extendedStair.Trigger);

            return true;
        }

        if (BoomDelayedDoorTranslator.TryTranslate(line.Special, out _))
            return true;

        if (BoomLightingTranslator.TryTranslate(line.Special, out var lighting))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(lighting.Trigger) || !CanActivateLighting(thing))
                return true;

            if (world.SectorAction.DoBoomLighting(line, lighting))
                BoomTriggerLifecycle.ApplySuccess(world, line, lighting.Trigger);

            return true;
        }

        if (BoomElevatorTranslator.TryTranslate(line.Special, out var elevator))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(elevator.Trigger) || !CanActivateElevator(line, thing))
                return true;

            if (world.SectorAction.DoBoomElevator(line, elevator))
                BoomTriggerLifecycle.ApplySuccess(world, line, elevator.Trigger);

            return true;
        }

        if (BoomExtendedFloorTranslator.TryTranslate(line.Special, out var extendedFloor))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(extendedFloor.Trigger) || !CanActivateExtendedPlane(line, thing))
                return true;

            if (world.SectorAction.DoBoomFloor(line, extendedFloor))
                BoomTriggerLifecycle.ApplySuccess(world, line, extendedFloor.Trigger);

            return true;
        }

        if (BoomExtendedCeilingTranslator.TryTranslate(line.Special, out var extendedCeiling))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(extendedCeiling.Trigger) || !CanActivateExtendedPlane(line, thing))
                return true;

            if (world.SectorAction.DoBoomCeiling(line, extendedCeiling))
                BoomTriggerLifecycle.ApplySuccess(world, line, extendedCeiling.Trigger);

            return true;
        }

        if (BoomFloorTranslator.TryTranslate(line.Special, out var floor))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(floor.Trigger) || !CanActivate(line, thing, floor.Common))
                return true;

            if (world.SectorAction.DoBoomFloor(line, floor))
                BoomTriggerLifecycle.ApplySuccess(world, line, floor.Trigger);

            return true;
        }

        if (BoomCeilingTranslator.TryTranslate(line.Special, out var ceiling))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(ceiling.Trigger) || !CanActivate(line, thing, ceiling.Common))
                return true;

            if (world.SectorAction.DoBoomCeiling(line, ceiling))
                BoomTriggerLifecycle.ApplySuccess(world, line, ceiling.Trigger);

            return true;
        }

        if (BoomLockedDoorTranslator.TryTranslate(line.Special, out var lockedDoor))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(lockedDoor.Trigger) || !CanActivateLockedDoor(world, line, thing, lockedDoor))
                return true;

            if (world.SectorAction.DoBoomLockedDoor(line, lockedDoor))
                BoomTriggerLifecycle.ApplySuccess(world, line, lockedDoor.Trigger);

            return true;
        }

        if (BoomDoorTranslator.TryTranslate(line.Special, out var door))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(door.Trigger) || !CanActivateDoor(line, thing, door))
                return true;

            if (world.SectorAction.DoBoomDoor(line, door))
                BoomTriggerLifecycle.ApplySuccess(world, line, door.Trigger);

            return true;
        }

        if (BoomLiftTranslator.TryTranslate(line.Special, out var lift))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(lift.Trigger) || !CanActivate(line, thing, lift.Common))
                return true;

            if (world.SectorAction.DoBoomLift(line, lift))
                BoomTriggerLifecycle.ApplySuccess(world, line, lift.Trigger);

            return true;
        }

        if (BoomStairTranslator.TryTranslate(line.Special, out var stair))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(stair.Trigger) || !CanActivate(line, thing, stair.Common))
                return true;

            if (world.SectorAction.DoBoomStairs(line, stair))
                BoomTriggerLifecycle.ApplySuccess(world, line, stair.Trigger);

            return true;
        }

        if (BoomCrusherTranslator.TryTranslate(line.Special, out var crusher))
        {
            if (!BoomTriggerSemantics.CanActivateFromCross(crusher.Trigger) || !CanActivate(line, thing, crusher.Common))
                return true;

            if (world.SectorAction.DoBoomCrusher(line, crusher))
                BoomTriggerLifecycle.ApplySuccess(world, line, crusher.Trigger);

            return true;
        }

        return false;
    }

    public static bool TryShoot(World world, LineDef line, Mobj thing)
    {
        if (BoomTeleportTranslator.TryTranslate(line.Special, out _))
            return true;

        if (BoomDonutTranslator.TryTranslate(line.Special, out _))
            return true;

        if (BoomPlatformTranslator.TryTranslate(line.Special, out _))
            return true;

        if (BoomExtendedStairTranslator.TryTranslate(line.Special, out _))
            return true;

        if (BoomDelayedDoorTranslator.TryTranslate(line.Special, out _))
            return true;

        if (BoomExitTranslator.TryTranslate(line.Special, out var exit))
        {
            if (!BoomTriggerSemantics.CanActivateFromShoot(exit.Trigger) ||
                !CanActivateExit(thing) ||
                !MbfZombieExitCompatibility.CanTriggerLineExit(
                    world.Options.Compatibility,
                    world.Options.MbfOptions.CompZombie,
                    thing))
            {
                return true;
            }

            BoomTriggerLifecycle.ApplySuccess(world, line, exit.Trigger);

            if (exit.Type == BoomExitType.Secret)
                world.SecretExitLevel();
            else
                world.ExitLevel();

            return true;
        }

        if (BoomLightingTranslator.TryTranslate(line.Special, out _))
            return true;

        if (BoomElevatorTranslator.TryTranslate(line.Special, out _))
            return true;

        if (BoomFloorTranslator.TryTranslate(line.Special, out var floor))
        {
            if (!BoomTriggerSemantics.CanActivateFromShoot(floor.Trigger) || !CanActivate(line, thing, floor.Common))
                return true;

            if (world.SectorAction.DoBoomFloor(line, floor))
                BoomTriggerLifecycle.ApplySuccess(world, line, floor.Trigger);

            return true;
        }

        if (BoomCeilingTranslator.TryTranslate(line.Special, out var ceiling))
        {
            if (!BoomTriggerSemantics.CanActivateFromShoot(ceiling.Trigger) || !CanActivate(line, thing, ceiling.Common))
                return true;

            if (world.SectorAction.DoBoomCeiling(line, ceiling))
                BoomTriggerLifecycle.ApplySuccess(world, line, ceiling.Trigger);

            return true;
        }

        if (BoomLockedDoorTranslator.TryTranslate(line.Special, out var lockedDoor))
        {
            if (!BoomTriggerSemantics.CanActivateFromShoot(lockedDoor.Trigger) || !CanActivateLockedDoor(world, line, thing, lockedDoor))
                return true;

            if (world.SectorAction.DoBoomLockedDoor(line, lockedDoor))
                BoomTriggerLifecycle.ApplySuccess(world, line, lockedDoor.Trigger);

            return true;
        }

        if (BoomDoorTranslator.TryTranslate(line.Special, out var door))
        {
            if (!BoomTriggerSemantics.CanActivateFromShoot(door.Trigger) || !CanActivateDoor(line, thing, door))
                return true;

            if (world.SectorAction.DoBoomDoor(line, door))
                BoomTriggerLifecycle.ApplySuccess(world, line, door.Trigger);

            return true;
        }

        if (BoomLiftTranslator.TryTranslate(line.Special, out var lift))
        {
            if (!BoomTriggerSemantics.CanActivateFromShoot(lift.Trigger) || !CanActivate(line, thing, lift.Common))
                return true;

            if (world.SectorAction.DoBoomLift(line, lift))
                BoomTriggerLifecycle.ApplySuccess(world, line, lift.Trigger);

            return true;
        }

        if (BoomStairTranslator.TryTranslate(line.Special, out var stair))
        {
            if (!BoomTriggerSemantics.CanActivateFromShoot(stair.Trigger) || !CanActivate(line, thing, stair.Common))
                return true;

            if (world.SectorAction.DoBoomStairs(line, stair))
                BoomTriggerLifecycle.ApplySuccess(world, line, stair.Trigger);

            return true;
        }

        if (BoomCrusherTranslator.TryTranslate(line.Special, out var crusher))
        {
            if (!BoomTriggerSemantics.CanActivateFromShoot(crusher.Trigger) || !CanActivate(line, thing, crusher.Common))
                return true;

            if (world.SectorAction.DoBoomCrusher(line, crusher))
                BoomTriggerLifecycle.ApplySuccess(world, line, crusher.Trigger);

            return true;
        }

        return false;
    }

    public static bool TryPush(World world, LineDef line, int side, Mobj thing)
    {
        if (BoomFloorTranslator.TryTranslate(line.Special, out var floor))
        {
            if (!BoomTriggerSemantics.Matches(floor.Trigger, BoomActivationChannel.Push))
                return false;

            if (!BoomTriggerSemantics.CanActivateFromPushUse(floor.Trigger, side) || !CanActivate(line, thing, floor.Common))
                return true;

            if (world.SectorAction.DoBoomFloor(line, floor))
                BoomTriggerLifecycle.ApplySuccess(world, line, floor.Trigger);

            return true;
        }

        if (BoomCeilingTranslator.TryTranslate(line.Special, out var ceiling))
        {
            if (!BoomTriggerSemantics.Matches(ceiling.Trigger, BoomActivationChannel.Push))
                return false;

            if (!BoomTriggerSemantics.CanActivateFromPushUse(ceiling.Trigger, side) || !CanActivate(line, thing, ceiling.Common))
                return true;

            if (world.SectorAction.DoBoomCeiling(line, ceiling))
                BoomTriggerLifecycle.ApplySuccess(world, line, ceiling.Trigger);

            return true;
        }

        if (BoomLockedDoorTranslator.TryTranslate(line.Special, out var lockedDoor))
        {
            if (!BoomTriggerSemantics.Matches(lockedDoor.Trigger, BoomActivationChannel.Push))
                return false;

            if (!BoomTriggerSemantics.CanActivateFromPushUse(lockedDoor.Trigger, side) || !CanActivateLockedDoor(world, line, thing, lockedDoor))
                return true;

            if (world.SectorAction.DoBoomLockedDoor(line, lockedDoor))
                BoomTriggerLifecycle.ApplySuccess(world, line, lockedDoor.Trigger);

            return true;
        }

        if (BoomDoorTranslator.TryTranslate(line.Special, out var door))
        {
            if (!BoomTriggerSemantics.Matches(door.Trigger, BoomActivationChannel.Push))
                return false;

            if (!BoomTriggerSemantics.CanActivateFromPushUse(door.Trigger, side) || !CanActivateDoor(line, thing, door))
                return true;

            if (world.SectorAction.DoBoomDoor(line, door))
                BoomTriggerLifecycle.ApplySuccess(world, line, door.Trigger);

            return true;
        }

        if (BoomLiftTranslator.TryTranslate(line.Special, out var lift))
        {
            if (!BoomTriggerSemantics.Matches(lift.Trigger, BoomActivationChannel.Push))
                return false;

            if (!BoomTriggerSemantics.CanActivateFromPushUse(lift.Trigger, side) || !CanActivate(line, thing, lift.Common))
                return true;

            if (world.SectorAction.DoBoomLift(line, lift))
                BoomTriggerLifecycle.ApplySuccess(world, line, lift.Trigger);

            return true;
        }

        if (BoomStairTranslator.TryTranslate(line.Special, out var stair))
        {
            if (!BoomTriggerSemantics.Matches(stair.Trigger, BoomActivationChannel.Push))
                return false;

            if (!BoomTriggerSemantics.CanActivateFromPushUse(stair.Trigger, side) || !CanActivate(line, thing, stair.Common))
                return true;

            if (world.SectorAction.DoBoomStairs(line, stair))
                BoomTriggerLifecycle.ApplySuccess(world, line, stair.Trigger);

            return true;
        }

        if (BoomCrusherTranslator.TryTranslate(line.Special, out var crusher))
        {
            if (!BoomTriggerSemantics.Matches(crusher.Trigger, BoomActivationChannel.Push))
                return false;

            if (!BoomTriggerSemantics.CanActivateFromPushUse(crusher.Trigger, side) || !CanActivate(line, thing, crusher.Common))
                return true;

            if (world.SectorAction.DoBoomCrusher(line, crusher))
                BoomTriggerLifecycle.ApplySuccess(world, line, crusher.Trigger);

            return true;
        }

        return false;
    }

    public static bool TryGetGeneralizedTrigger(LineSpecial special, out BoomTriggerType trigger)
    {
        if (!BoomGeneralizedSpecial.IsGeneralized(special))
        {
            trigger = default;
            return false;
        }

        trigger = BoomGeneralizedSpecial.DecodeTrigger(special);
        return true;
    }

    private static bool CanActivateTeleport(LineDef line, Mobj thing, BoomTeleportSpecial specification)
    {
        if (!BoomMonsterActivationRules.CanActivateTeleport(
                thing,
                specification.PlayersAllowed,
                specification.MonstersAllowed))
        {
            return false;
        }

        return line.Tag != 0 || specification.AllowsZeroTag;
    }

    private static bool CanActivateExit(Mobj thing)
    {
        return BoomMonsterActivationRules.CanActivatePlayerOnly(thing);
    }

    private static bool CanActivateDonut(LineDef line, Mobj thing)
    {
        return BoomMonsterActivationRules.CanActivatePlayerOnly(thing) && line.Tag != 0;
    }

    private static bool ShouldApplyPlatformTriggerWithoutAction(BoomPlatformSpecial specification)
    {
        return specification.Action == BoomPlatformAction.Perpetual &&
            specification.Trigger == BoomTriggerType.SwitchRepeat;
    }

    private static bool CanActivatePlatform(LineDef line, Mobj thing)
    {
        return BoomMonsterActivationRules.CanActivatePlayerOnly(thing) && line.Tag != 0;
    }

    private static bool CanActivateExtendedCrusher(LineDef line, Mobj thing)
    {
        return BoomMonsterActivationRules.CanActivatePlayerOnly(thing) && line.Tag != 0;
    }

    private static bool CanActivateExtendedStair(LineDef line, Mobj thing)
    {
        return BoomMonsterActivationRules.CanActivatePlayerOnly(thing) && line.Tag != 0;
    }

    private static bool CanActivateDelayedDoor(LineDef line, Mobj thing)
    {
        return BoomMonsterActivationRules.CanActivatePlayerOnly(thing) && line.Tag != 0;
    }

    private static bool DoExtendedStairs(World world, LineDef line, BoomExtendedStairSpecial specification)
    {
        var type = specification.Action == BoomExtendedStairAction.Build8
            ? StairType.Build8
            : StairType.Turbo16;

        return world.SectorAction.BuildStairs(line, type);
    }

    private static bool CanActivateLighting(Mobj thing)
    {
        return BoomMonsterActivationRules.CanActivatePlayerOnly(thing);
    }

    private static bool CanActivateExtendedPlane(LineDef line, Mobj thing)
    {
        return BoomMonsterActivationRules.CanActivatePlayerOnly(thing) && line.Tag != 0;
    }

    private static bool CanActivateElevator(LineDef line, Mobj thing)
    {
        return BoomMonsterActivationRules.CanActivatePlayerOnly(thing) && line.Tag != 0;
    }

    private static bool CanActivate(LineDef line, Mobj thing, BoomActionSpecification specification)
    {
        if (!BoomMonsterActivationRules.CanActivate(thing, specification.AllowsMonsters))
            return false;

        return !specification.UsesTagForTargeting || line.Tag != 0;
    }

    private static bool CanActivateDoor(LineDef line, Mobj thing, BoomDoorSpecial specification)
    {
        if (!BoomMonsterActivationRules.CanActivateDoor(
                line,
                thing,
                specification.AllowsMonsters))
        {
            return false;
        }

        return !specification.Common.UsesTagForTargeting || line.Tag != 0;
    }

    private static bool CanActivateLockedDoor(World world, LineDef line, Mobj thing, BoomLockedDoorSpecial specification)
    {
        var player = thing?.Player;
        if (player == null)
            return false;

        if (!HasRequiredKey(player, specification))
        {
            player.SendMessage(GetLockedDoorFailureMessage(specification));
            world.StartSound(player.Mobj, Sfx.OOF, SfxType.Voice);
            return false;
        }

        return !specification.UsesTagForTargeting || line.Tag != 0;
    }

    private static bool HasRequiredKey(Player player, BoomLockedDoorSpecial specification)
    {
        var cards = player.Cards;
        var redCard = cards[(int)CardType.RedCard];
        var blueCard = cards[(int)CardType.BlueCard];
        var yellowCard = cards[(int)CardType.YellowCard];
        var redSkull = cards[(int)CardType.RedSkull];
        var blueSkull = cards[(int)CardType.BlueSkull];
        var yellowSkull = cards[(int)CardType.YellowSkull];

        return specification.Key switch
        {
            BoomLockedDoorKey.Any => redCard || blueCard || yellowCard || redSkull || blueSkull || yellowSkull,
            BoomLockedDoorKey.RedCard => redCard || (specification.SkullIsCard && redSkull),
            BoomLockedDoorKey.BlueCard => blueCard || (specification.SkullIsCard && blueSkull),
            BoomLockedDoorKey.YellowCard => yellowCard || (specification.SkullIsCard && yellowSkull),
            BoomLockedDoorKey.RedSkull => redSkull || (specification.SkullIsCard && redCard),
            BoomLockedDoorKey.BlueSkull => blueSkull || (specification.SkullIsCard && blueCard),
            BoomLockedDoorKey.YellowSkull => yellowSkull || (specification.SkullIsCard && yellowCard),
            BoomLockedDoorKey.All when specification.SkullIsCard =>
                (redCard || redSkull) && (blueCard || blueSkull) && (yellowCard || yellowSkull),
            BoomLockedDoorKey.All =>
                redCard && blueCard && yellowCard && redSkull && blueSkull && yellowSkull,
            _ => false
        };
    }

    private static string GetLockedDoorFailureMessage(BoomLockedDoorSpecial specification)
    {
        if (specification.Key == BoomLockedDoorKey.Any)
            return DoomInfo.Strings.PD_ANY;

        if (specification.Key == BoomLockedDoorKey.All)
            return specification.SkullIsCard ? DoomInfo.Strings.PD_ALL3 : DoomInfo.Strings.PD_ALL6;

        if (specification.SkullIsCard)
        {
            return specification.Key switch
            {
                BoomLockedDoorKey.RedCard or BoomLockedDoorKey.RedSkull => DoomInfo.Strings.PD_REDK,
                BoomLockedDoorKey.BlueCard or BoomLockedDoorKey.BlueSkull => DoomInfo.Strings.PD_BLUEK,
                BoomLockedDoorKey.YellowCard or BoomLockedDoorKey.YellowSkull => DoomInfo.Strings.PD_YELLOWK,
                _ => DoomInfo.Strings.PD_ANY
            };
        }

        return specification.Key switch
        {
            BoomLockedDoorKey.RedCard => DoomInfo.Strings.PD_REDC,
            BoomLockedDoorKey.BlueCard => DoomInfo.Strings.PD_BLUEC,
            BoomLockedDoorKey.YellowCard => DoomInfo.Strings.PD_YELLOWC,
            BoomLockedDoorKey.RedSkull => DoomInfo.Strings.PD_REDS,
            BoomLockedDoorKey.BlueSkull => DoomInfo.Strings.PD_BLUES,
            BoomLockedDoorKey.YellowSkull => DoomInfo.Strings.PD_YELLOWS,
            _ => DoomInfo.Strings.PD_ANY
        };
    }

}
