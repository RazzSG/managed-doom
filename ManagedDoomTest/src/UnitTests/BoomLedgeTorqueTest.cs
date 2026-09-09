using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomLedgeTorqueTest
{
    [TestMethod]
    public void BlueKeyIsNonSentientAndEligibleForMbfTorque()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

        var key = CreateBlueKey(world);
        var monster = world.ThingAllocation.SpawnMobj(
            world.ConsolePlayer.Mobj.X,
            world.ConsolePlayer.Mobj.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);

        Assert.IsFalse(BoomLedgeTorque.IsSentient(key));
        Assert.IsTrue(BoomLedgeTorque.IsSentient(monster));
    }

    [TestMethod]
    public void MbfDefaultTorquePushesRestingKeyOffContactedTallLedge()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

        var line = FindLongTwoSidedLine(world);
        line.FrontSector.FloorHeight = Fixed.Zero;
        line.BackSector.FloorHeight = Fixed.FromInt(64);

        var key = CreateBlueKey(world);
        PlaceJustAcrossFrontSide(key, line);
        key.Z = Fixed.FromInt(64);
        key.FloorZ = Fixed.FromInt(64);
        key.DropoffZ = Fixed.Zero;
        key.MomX = Fixed.Zero;
        key.MomY = Fixed.Zero;
        key.MomZ = Fixed.Zero;

        var torque = new BoomLedgeTorque(world);
        torque.UpdateAtRest(key);

        Assert.IsTrue(
            key.MomX != Fixed.Zero || key.MomY != Fixed.Zero,
            "A resting key whose center of mass is past a contacted ledge must receive MBF pseudo-torque when comp_falloff is disabled.");
        Assert.IsTrue(key.BoomLedgeFalling);
    }

    [TestMethod]
    public void BoomProfileDoesNotApplyMbfLedgeTorque()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Boom },
            null);

        var key = CreateBlueKey(world);
        key.Z = Fixed.FromInt(64);
        key.FloorZ = Fixed.FromInt(64);
        key.DropoffZ = Fixed.Zero;
        key.BoomLedgeFalling = true;
        key.BoomTorqueGear = 7;

        var torque = new BoomLedgeTorque(world);
        torque.UpdateAtRest(key);

        Assert.AreEqual(Fixed.Zero.Data, key.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, key.MomY.Data);
        Assert.IsFalse(key.BoomLedgeFalling);
        Assert.AreEqual(0, key.BoomTorqueGear);
    }

    [TestMethod]
    public void MbfCompFalloffDisablesTorqueAndResetsState()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.MbfOptions.CompFalloff = true;
        var world = new World(content, options, null);

        var key = CreateBlueKey(world);
        key.Z = Fixed.FromInt(64);
        key.FloorZ = Fixed.FromInt(64);
        key.DropoffZ = Fixed.Zero;
        key.BoomLedgeFalling = true;
        key.BoomTorqueGear = 7;

        var torque = new BoomLedgeTorque(world);
        torque.UpdateAtRest(key);

        Assert.AreEqual(Fixed.Zero.Data, key.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, key.MomY.Data);
        Assert.IsFalse(key.BoomLedgeFalling);
        Assert.AreEqual(0, key.BoomTorqueGear);
    }

    [TestMethod]
    public void MbfMovingFloorResetsMaxTorqueGearForFallingCorpse()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

        var corpse = SpawnCorpseAtPlayer(world);
        corpse.BoomLedgeFalling = true;
        corpse.BoomTorqueGear = 22; // OVERDRIVE (6) + 16 = MAXGEAR.

        var sector = corpse.Subsector.Sector;
        var startFloor = sector.FloorHeight;

        world.SectorAction.MovePlane(
            sector,
            Fixed.One,
            startFloor - Fixed.One,
            crush: false,
            floorOrCeiling: 0,
            direction: -1);

        Assert.AreEqual(0, corpse.BoomTorqueGear);
        Assert.IsTrue(corpse.BoomLedgeFalling);
        Assert.AreEqual(sector.FloorHeight.Data, corpse.Z.Data);
    }

    [TestMethod]
    public void MbfMovingFloorKeepsSubMaxTorqueGearForFallingCorpse()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

        var corpse = SpawnCorpseAtPlayer(world);
        corpse.BoomLedgeFalling = true;
        corpse.BoomTorqueGear = 21;

        var sector = corpse.Subsector.Sector;
        var startFloor = sector.FloorHeight;

        world.SectorAction.MovePlane(
            sector,
            Fixed.One,
            startFloor - Fixed.One,
            crush: false,
            floorOrCeiling: 0,
            direction: -1);

        Assert.AreEqual(21, corpse.BoomTorqueGear);
        Assert.IsTrue(corpse.BoomLedgeFalling);
        Assert.AreEqual(sector.FloorHeight.Data, corpse.Z.Data);
    }

    [TestMethod]
    public void VanillaDoesNotApplyBoomLedgeTorque()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Vanilla },
            null);

        var key = CreateBlueKey(world);
        key.Z = Fixed.FromInt(64);
        key.FloorZ = Fixed.FromInt(64);
        key.DropoffZ = Fixed.Zero;

        var torque = new BoomLedgeTorque(world);
        torque.UpdateAtRest(key);

        Assert.AreEqual(Fixed.Zero.Data, key.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, key.MomY.Data);
        Assert.IsFalse(key.BoomLedgeFalling);
        Assert.AreEqual(0, key.BoomTorqueGear);
    }

    private static Mobj SpawnCorpseAtPlayer(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        var corpse = world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);

        corpse.Health = 0;
        corpse.Flags |= MobjFlags.Corpse;
        corpse.MomX = Fixed.Zero;
        corpse.MomY = Fixed.Zero;
        corpse.MomZ = Fixed.Zero;

        return corpse;
    }

    private static Mobj CreateBlueKey(World world)
    {
        var info = DoomInfo.MobjInfos[(int)MobjType.Misc4];
        return new Mobj(world)
        {
            Type = MobjType.Misc4,
            Info = info,
            Health = info.SpawnHealth,
            Radius = info.Radius,
            Height = info.Height,
            Flags = info.Flags
        };
    }

    private static LineDef FindLongTwoSidedLine(World world)
    {
        return world.Map.Lines
            .Where(line =>
                line.FrontSector != null &&
                line.BackSector != null &&
                !object.ReferenceEquals(line.FrontSector, line.BackSector))
            .OrderByDescending(line => Fixed.Abs(line.Dx).Data + Fixed.Abs(line.Dy).Data)
            .First();
    }

    private static void PlaceJustAcrossFrontSide(Mobj thing, LineDef line)
    {
        var midpointX = (line.Vertex1.X + line.Vertex2.X) / 2;
        var midpointY = (line.Vertex1.Y + line.Vertex2.Y) / 2;
        var lineAngle = Geometry.PointToAngle(Fixed.Zero, Fixed.Zero, line.Dx, line.Dy);
        var rightNormal = lineAngle - Angle.Ang90;
        var offset = Fixed.FromInt(8);

        // For Doom's line orientation the right side is the front side.
        thing.X = midpointX + Trig.Cos(rightNormal) * offset;
        thing.Y = midpointY + Trig.Sin(rightNormal) * offset;
        thing.Radius = Fixed.FromInt(20);

        Assert.AreEqual(0, Geometry.PointOnLineSide(thing.X, thing.Y, line));
    }
}
