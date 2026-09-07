using System;
using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomTeleportSpecialTest
{
    [TestMethod]
    public void TranslatorMapsAllExtendedTeleporters()
    {
        var expected = new[]
        {
            Entry(174, BoomTriggerType.SwitchOnce, BoomTeleportDestination.Thing, silent: false, preserveOrientation: false, reverse: false, playersAllowed: true, allowsZeroTag: true),
            Entry(195, BoomTriggerType.SwitchRepeat, BoomTeleportDestination.Thing, silent: false, preserveOrientation: false, reverse: false, playersAllowed: true, allowsZeroTag: true),
            Entry(207, BoomTriggerType.WalkOnce, BoomTeleportDestination.Thing, silent: true, preserveOrientation: true, reverse: false, playersAllowed: true, allowsZeroTag: true),
            Entry(208, BoomTriggerType.WalkRepeat, BoomTeleportDestination.Thing, silent: true, preserveOrientation: true, reverse: false, playersAllowed: true, allowsZeroTag: true),
            Entry(209, BoomTriggerType.SwitchOnce, BoomTeleportDestination.Thing, silent: true, preserveOrientation: true, reverse: false, playersAllowed: true, allowsZeroTag: true),
            Entry(210, BoomTriggerType.SwitchRepeat, BoomTeleportDestination.Thing, silent: true, preserveOrientation: true, reverse: false, playersAllowed: true, allowsZeroTag: true),
            Entry(243, BoomTriggerType.WalkOnce, BoomTeleportDestination.Line, silent: true, preserveOrientation: true, reverse: false, playersAllowed: true, allowsZeroTag: false),
            Entry(244, BoomTriggerType.WalkRepeat, BoomTeleportDestination.Line, silent: true, preserveOrientation: true, reverse: false, playersAllowed: true, allowsZeroTag: false),
            Entry(262, BoomTriggerType.WalkOnce, BoomTeleportDestination.Line, silent: true, preserveOrientation: true, reverse: true, playersAllowed: true, allowsZeroTag: false),
            Entry(263, BoomTriggerType.WalkRepeat, BoomTeleportDestination.Line, silent: true, preserveOrientation: true, reverse: true, playersAllowed: true, allowsZeroTag: false),
            Entry(264, BoomTriggerType.WalkOnce, BoomTeleportDestination.Line, silent: true, preserveOrientation: true, reverse: true, playersAllowed: false, allowsZeroTag: false),
            Entry(265, BoomTriggerType.WalkRepeat, BoomTeleportDestination.Line, silent: true, preserveOrientation: true, reverse: true, playersAllowed: false, allowsZeroTag: false),
            Entry(266, BoomTriggerType.WalkOnce, BoomTeleportDestination.Line, silent: true, preserveOrientation: true, reverse: false, playersAllowed: false, allowsZeroTag: false),
            Entry(267, BoomTriggerType.WalkRepeat, BoomTeleportDestination.Line, silent: true, preserveOrientation: true, reverse: false, playersAllowed: false, allowsZeroTag: false),
            Entry(268, BoomTriggerType.WalkOnce, BoomTeleportDestination.Thing, silent: true, preserveOrientation: true, reverse: false, playersAllowed: false, allowsZeroTag: false),
            Entry(269, BoomTriggerType.WalkRepeat, BoomTeleportDestination.Thing, silent: true, preserveOrientation: true, reverse: false, playersAllowed: false, allowsZeroTag: false)
        };

        foreach (var item in expected)
        {
            var specification = BoomTeleportTranslator.Translate((LineSpecial)item.Special);
            Assert.AreEqual(item.Trigger, specification.Trigger);
            Assert.AreEqual(item.Destination, specification.Destination);
            Assert.AreEqual(item.Silent, specification.Silent);
            Assert.AreEqual(item.PreserveOrientation, specification.PreserveOrientation);
            Assert.AreEqual(item.Reverse, specification.Reverse);
            Assert.AreEqual(item.PlayersAllowed, specification.PlayersAllowed);
            Assert.IsTrue(specification.MonstersAllowed);
            Assert.AreEqual(item.AllowsZeroTag, specification.AllowsZeroTag);
            Assert.AreEqual(item.Trigger.IsRepeatable(), specification.Repeatable);
        }

        Assert.IsFalse(BoomTeleportTranslator.TryTranslate((LineSpecial)173, out _));
        Assert.IsFalse(BoomTeleportTranslator.TryTranslate((LineSpecial)196, out _));
        Assert.IsFalse(BoomTeleportTranslator.TryTranslate((LineSpecial)206, out _));
        Assert.IsFalse(BoomTeleportTranslator.TryTranslate((LineSpecial)211, out _));
        Assert.IsFalse(BoomTeleportTranslator.TryTranslate((LineSpecial)242, out _));
        Assert.IsFalse(BoomTeleportTranslator.TryTranslate((LineSpecial)245, out _));
        Assert.IsFalse(BoomTeleportTranslator.TryTranslate((LineSpecial)261, out _));
        Assert.IsFalse(BoomTeleportTranslator.TryTranslate((LineSpecial)270, out _));
    }

    [TestMethod]
    public void WalkOnceTeleportIsNotConsumedFromBackSide()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = world.Map.Lines[0];

        line.Tag = 0;
        line.Special = (LineSpecial)207;

        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 1, world.ConsolePlayer.Mobj));
        Assert.AreEqual(207, (int)line.Special);
    }

    [TestMethod]
    public void MonsterOnlyTeleportDoesNotConsumeForPlayer()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = world.Map.Lines[0];

        line.Tag = 30000;
        line.Special = (LineSpecial)268;

        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, world.ConsolePlayer.Mobj));
        Assert.AreEqual(268, (int)line.Special);
    }

    [TestMethod]
    public void SecretSwitchTeleportRejectsNonPlayerUse()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = world.Map.Lines[0];
        var nonPlayer = new Mobj(world);

        line.Tag = 30000;
        line.Flags |= LineFlags.Secret;
        line.Special = (LineSpecial)174;

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, nonPlayer, out var result));
        Assert.IsFalse(result);
        Assert.AreEqual(174, (int)line.Special);
    }

    [TestMethod]
    public void LineTeleportRequiresNonZeroTag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = world.Map.Lines[0];

        line.Tag = 0;
        line.Special = (LineSpecial)243;

        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, world.ConsolePlayer.Mobj));
        Assert.AreEqual(243, (int)line.Special);
    }

    [TestMethod]
    public void BoomNormalThingTeleportPlacesThingOnDestinationFloor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions
        {
            Compatibility = GameCompatibility.Boom,
            GameVersion = GameVersion.Final
        };
        var world = new World(content, options, null);
        var thing = world.ConsolePlayer.Mobj;
        var line = world.Map.Lines[0];
        var destinationSector = thing.Subsector.Sector;

        foreach (var sector in world.Map.Sectors)
            sector.Tag = 0;

        line.Tag = 30000;
        destinationSector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        var destination = world.ThingAllocation.SpawnMobj(thing.X, thing.Y, Mobj.OnFloorZ, MobjType.Teleportman);
        thing.Z = thing.FloorZ + Fixed.FromInt(8);

        var specification = BoomTeleportTranslator.Translate((LineSpecial)174);
        Assert.IsTrue(world.SectorAction.DoBoomTeleport(line, 0, thing, specification));

        Assert.AreEqual(destination.X.Data, thing.X.Data);
        Assert.AreEqual(destination.Y.Data, thing.Y.Data);
        Assert.AreEqual(thing.FloorZ.Data, thing.Z.Data);
    }

    [TestMethod]
    public void SilentThingTeleportPreservesHeightMomentumAndReactionTime()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var thing = world.ConsolePlayer.Mobj;
        var line = world.Map.Lines.First(l => l.Dx != Fixed.Zero || l.Dy != Fixed.Zero);
        var destinationSector = thing.Subsector.Sector;

        foreach (var sector in world.Map.Sectors)
            sector.Tag = 0;

        line.Tag = 30000;
        destinationSector.Tag = 30000;
        world.Map.BoomTags.Rebuild();

        var destination = world.ThingAllocation.SpawnMobj(thing.X, thing.Y, Mobj.OnFloorZ, MobjType.Teleportman);
        destination.Angle = Angle.Ang90;

        thing.Z = thing.FloorZ + Fixed.FromInt(8);
        thing.Angle = Angle.Ang45;
        thing.MomX = Fixed.FromInt(3);
        thing.MomY = Fixed.FromInt(1);
        thing.MomZ = Fixed.FromInt(2);
        thing.ReactionTime = 7;

        var heightAboveFloor = thing.Z - thing.FloorZ;
        var oldMomX = thing.MomX;
        var oldMomY = thing.MomY;
        var oldMomZ = thing.MomZ;
        var oldAngle = thing.Angle;
        var adjustment = Geometry.PointToAngle(Fixed.Zero, Fixed.Zero, line.Dx, line.Dy) - destination.Angle + Angle.Ang90;
        var sine = Trig.Sin(adjustment);
        var cosine = Trig.Cos(adjustment);

        var specification = BoomTeleportTranslator.Translate((LineSpecial)207);
        Assert.IsTrue(world.SectorAction.DoBoomTeleport(line, 0, thing, specification));

        Assert.AreEqual(destination.X.Data, thing.X.Data);
        Assert.AreEqual(destination.Y.Data, thing.Y.Data);
        Assert.AreEqual(heightAboveFloor.Data, (thing.Z - thing.FloorZ).Data);
        Assert.AreEqual((oldAngle + adjustment).Data, thing.Angle.Data);
        Assert.AreEqual((oldMomX * cosine - oldMomY * sine).Data, thing.MomX.Data);
        Assert.AreEqual((oldMomY * cosine + oldMomX * sine).Data, thing.MomY.Data);
        Assert.AreEqual(oldMomZ.Data, thing.MomZ.Data);
        Assert.AreEqual(7, thing.ReactionTime);
    }

    private static (int Special, BoomTriggerType Trigger, BoomTeleportDestination Destination, bool Silent,
        bool PreserveOrientation, bool Reverse, bool PlayersAllowed, bool AllowsZeroTag) Entry(
        int special,
        BoomTriggerType trigger,
        BoomTeleportDestination destination,
        bool silent,
        bool preserveOrientation,
        bool reverse,
        bool playersAllowed,
        bool allowsZeroTag)
    {
        return (special, trigger, destination, silent, preserveOrientation, reverse, playersAllowed, allowsZeroTag);
    }
}
