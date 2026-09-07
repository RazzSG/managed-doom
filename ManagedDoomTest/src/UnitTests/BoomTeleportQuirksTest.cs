using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomTeleportQuirksTest
{
    [TestMethod]
    public void BossTeleportCanTelefragOutsideMap30OnlyAtBoomAndLaterCompatibility()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var monster = new Mobj(world);

        Assert.IsFalse(BoomTeleportQuirks.CanTelefragAtDestination(
            monster, bossTeleport: true, mapNumber: 1, GameCompatibility.Vanilla));

        Assert.IsTrue(BoomTeleportQuirks.CanTelefragAtDestination(
            monster, bossTeleport: true, mapNumber: 1, GameCompatibility.Boom));
        Assert.IsTrue(BoomTeleportQuirks.CanTelefragAtDestination(
            monster, bossTeleport: true, mapNumber: 1, GameCompatibility.Mbf));
        Assert.IsTrue(BoomTeleportQuirks.CanTelefragAtDestination(
            monster, bossTeleport: true, mapNumber: 1, GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void OrdinaryMonsterTeleportStillCannotTelefragOutsideMap30InBoom()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var monster = new Mobj(world);

        Assert.IsFalse(BoomTeleportQuirks.CanTelefragAtDestination(
            monster, bossTeleport: false, mapNumber: 1, GameCompatibility.Boom));
    }

    [TestMethod]
    public void OrdinaryMonsterTeleportDoesNotGainMap30TelefragPermissionInBoom()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var monster = new Mobj(world);

        Assert.IsFalse(BoomTeleportQuirks.CanTelefragAtDestination(
            monster, bossTeleport: false, mapNumber: 30, GameCompatibility.Boom));
    }

    [TestMethod]
    public void VanillaMap30MonsterTelefragBehaviorIsPreserved()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var monster = new Mobj(world);

        Assert.IsTrue(BoomTeleportQuirks.CanTelefragAtDestination(
            monster, bossTeleport: false, mapNumber: 30, GameCompatibility.Vanilla));
    }

    [TestMethod]
    public void PlayerTeleportCanAlwaysTelefrag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var player = world.ConsolePlayer.Mobj;

        Assert.IsTrue(BoomTeleportQuirks.CanTelefragAtDestination(
            player, bossTeleport: false, mapNumber: 1, GameCompatibility.Vanilla));
        Assert.IsTrue(BoomTeleportQuirks.CanTelefragAtDestination(
            player, bossTeleport: false, mapNumber: 1, GameCompatibility.Boom));
    }

    [TestMethod]
    public void TeleportMoveUsesBossFlagForBoomTelefragPermission()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var vanillaWorld = new World(content, new GameOptions
        {
            Compatibility = GameCompatibility.Vanilla,
            Map = 1
        }, null);
        var vanillaPlayer = vanillaWorld.ConsolePlayer.Mobj;
        var vanillaMonster = vanillaWorld.ThingAllocation.SpawnMobj(
            vanillaPlayer.X, vanillaPlayer.Y, Mobj.OnFloorZ, MobjType.Possessed);

        Assert.IsFalse(vanillaWorld.ThingMovement.TeleportMove(
            vanillaMonster, vanillaMonster.X, vanillaMonster.Y, bossTeleport: true));

        var boomWorld = new World(content, new GameOptions
        {
            Compatibility = GameCompatibility.Boom,
            Map = 1
        }, null);
        var boomPlayer = boomWorld.ConsolePlayer.Mobj;
        var boomMonster = boomWorld.ThingAllocation.SpawnMobj(
            boomPlayer.X, boomPlayer.Y, Mobj.OnFloorZ, MobjType.Possessed);

        Assert.IsTrue(boomWorld.ThingMovement.TeleportMove(
            boomMonster, boomMonster.X, boomMonster.Y, bossTeleport: true));
    }

    [TestMethod]
    public void StandardTeleportUsesBoomFloorPlacementAtBoomCompatibility()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions
        {
            Compatibility = GameCompatibility.Boom,
            GameVersion = GameVersion.Final
        }, null);
        var thing = world.ConsolePlayer.Mobj;
        var line = world.Map.Lines[0];
        var destinationSector = thing.Subsector.Sector;

        foreach (var sector in world.Map.Sectors)
            sector.Tag = 0;

        line.Tag = 30000;
        destinationSector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        world.ThingAllocation.SpawnMobj(
            thing.X, thing.Y, Mobj.OnFloorZ, MobjType.Teleportman);
        thing.Z = thing.FloorZ + Fixed.FromInt(8);

        Assert.IsTrue(world.SectorAction.Teleport(line, 0, thing));
        Assert.AreEqual(thing.FloorZ.Data, thing.Z.Data);
    }

    [TestMethod]
    public void VanillaFinalDoomStandardTeleportKeepsExistingZBehavior()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions
        {
            Compatibility = GameCompatibility.Vanilla,
            GameVersion = GameVersion.Final
        }, null);
        var thing = world.ConsolePlayer.Mobj;
        var line = world.Map.Lines[0];
        var destinationSector = thing.Subsector.Sector;

        foreach (var sector in world.Map.Sectors)
            sector.Tag = 0;

        line.Tag = 30000;
        destinationSector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        world.ThingAllocation.SpawnMobj(
            thing.X, thing.Y, Mobj.OnFloorZ, MobjType.Teleportman);
        var heightAboveFloor = Fixed.FromInt(8);
        thing.Z = thing.FloorZ + heightAboveFloor;

        Assert.IsTrue(world.SectorAction.Teleport(line, 0, thing));
        Assert.AreEqual(heightAboveFloor.Data, (thing.Z - thing.FloorZ).Data);
    }

    [TestMethod]
    public void BoomNormalTeleportExcludesVoodooDollFromPlayerViewUpdate()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var doll = new Mobj(world) { Player = player };

        Assert.AreSame(player, BoomTeleportQuirks.GetPlayerForTeleportViewUpdate(
            player.Mobj, GameCompatibility.Boom));
        Assert.IsNull(BoomTeleportQuirks.GetPlayerForTeleportViewUpdate(
            doll, GameCompatibility.Boom));

        // Preserve the old ManagedDoom/vanilla behavior below Boom.
        Assert.AreSame(player, BoomTeleportQuirks.GetPlayerForTeleportViewUpdate(
            doll, GameCompatibility.Vanilla));
    }
}
