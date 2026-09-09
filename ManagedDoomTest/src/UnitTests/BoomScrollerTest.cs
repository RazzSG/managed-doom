using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Scrolling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomScrollerTest
{
    private const short TestTag = 32000;

    [TestMethod]
    public void ScrollRightMovesFirstSideOneUnitPerTic()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var side = line.FrontSide;

        line.Special = (LineSpecial)85;
        side.TextureOffset = Fixed.Zero;
        side.RowOffset = Fixed.Zero;

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, side);

        scroller.Run();

        Assert.AreEqual(Fixed.FromInt(-1).Data, side.TextureOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, side.RowOffset.Data);
    }

    [TestMethod]
    public void SidedefOffsetScrollerCapturesRateAtSpawn()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var side = line.FrontSide;

        line.Special = (LineSpecial)255;
        side.TextureOffset = Fixed.FromInt(3);
        side.RowOffset = Fixed.FromInt(2);

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, side);

        Assert.AreEqual(Fixed.FromInt(-3).Data, scroller.Dx.Data);
        Assert.AreEqual(Fixed.FromInt(2).Data, scroller.Dy.Data);

        scroller.Run();
        Assert.AreEqual(Fixed.Zero.Data, side.TextureOffset.Data);
        Assert.AreEqual(Fixed.FromInt(4).Data, side.RowOffset.Data);

        scroller.Run();
        Assert.AreEqual(Fixed.FromInt(-3).Data, side.TextureOffset.Data);
        Assert.AreEqual(Fixed.FromInt(6).Data, side.RowOffset.Data);
    }

    [TestMethod]
    public void StaticFloorScrollerUsesTriggerVectorAndTaggedSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var sector = world.Map.Sectors[0];

        line.Special = (LineSpecial)251;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.FloorXOffset = Fixed.Zero;
        sector.FloorYOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Floor);
        var expectedDx = -(line.Dx >> 5);
        var expectedDy = line.Dy >> 5;

        Assert.AreEqual(expectedDx.Data, scroller.Dx.Data);
        Assert.AreEqual(expectedDy.Data, scroller.Dy.Data);

        scroller.Run();

        Assert.AreEqual(expectedDx.Data, sector.FloorXOffset.Data);
        Assert.AreEqual(expectedDy.Data, sector.FloorYOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, sector.CeilingXOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, sector.CeilingYOffset.Data);
    }

    [TestMethod]
    public void StaticCeilingScrollerUsesTriggerVectorAndTaggedSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var sector = world.Map.Sectors[0];

        line.Special = (LineSpecial)250;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.CeilingXOffset = Fixed.Zero;
        sector.CeilingYOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Ceiling);
        var expectedDx = -(line.Dx >> 5);
        var expectedDy = line.Dy >> 5;

        Assert.AreEqual(expectedDx.Data, scroller.Dx.Data);
        Assert.AreEqual(expectedDy.Data, scroller.Dy.Data);

        scroller.Run();

        Assert.AreEqual(expectedDx.Data, sector.CeilingXOffset.Data);
        Assert.AreEqual(expectedDy.Data, sector.CeilingYOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorXOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorYOffset.Data);
    }


    [TestMethod]
    public void CarryOnlyScrollerUsesBoomCarryFactor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        line.Special = (LineSpecial)252;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.FloorXOffset = Fixed.Zero;
        sector.FloorYOffset = Fixed.Zero;
        thing.Z = sector.FloorHeight;
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Carry);
        var carryFactor = new Fixed(Fixed.FracUnit * 3 / 32);
        var expectedDx = (line.Dx >> 5) * carryFactor;
        var expectedDy = (line.Dy >> 5) * carryFactor;

        Assert.AreEqual(expectedDx.Data, scroller.Dx.Data);
        Assert.AreEqual(expectedDy.Data, scroller.Dy.Data);

        scroller.Run();

        Assert.AreEqual(expectedDx.Data, thing.MomX.Data);
        Assert.AreEqual(expectedDy.Data, thing.MomY.Data);
        Assert.IsTrue(thing.MbfScrollingMovement);
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorXOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorYOffset.Data);
    }

    [TestMethod]
    public void ScrollAndCarryCreatesIndependentFloorAndCarryScrollers()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        line.Special = (LineSpecial)253;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.FloorXOffset = Fixed.Zero;
        sector.FloorYOffset = Fixed.Zero;
        thing.Z = sector.FloorHeight;
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var floorScroller = FindScroller(world, sector, BoomScrollerType.Floor);
        var carryScroller = FindScroller(world, sector, BoomScrollerType.Carry);
        var carryFactor = new Fixed(Fixed.FracUnit * 3 / 32);
        var expectedFloorDx = -(line.Dx >> 5);
        var expectedFloorDy = line.Dy >> 5;
        var expectedCarryDx = (line.Dx >> 5) * carryFactor;
        var expectedCarryDy = (line.Dy >> 5) * carryFactor;

        floorScroller.Run();
        carryScroller.Run();

        Assert.AreEqual(expectedFloorDx.Data, sector.FloorXOffset.Data);
        Assert.AreEqual(expectedFloorDy.Data, sector.FloorYOffset.Data);
        Assert.AreEqual(expectedCarryDx.Data, thing.MomX.Data);
        Assert.AreEqual(expectedCarryDy.Data, thing.MomY.Data);
    }

    [TestMethod]
    public void CarryScrollerKeepsCarryingThingWhoseOriginCrossedSectorBoundary()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var boundary = world.Map.Lines.First(line =>
            line.FrontSector != null &&
            line.BackSector != null &&
            !object.ReferenceEquals(line.FrontSector, line.BackSector));

        var thing = new Mobj(world)
        {
            X = (boundary.Vertex1.X + boundary.Vertex2.X) / 2,
            Y = (boundary.Vertex1.Y + boundary.Vertex2.Y) / 2,
            Radius = Fixed.FromInt(16),
            Flags = MobjFlags.Solid
        };

        world.ThingMovement.SetThingPosition(thing);

        var originSector = thing.Subsector.Sector;
        var conveyorSector = object.ReferenceEquals(originSector, boundary.FrontSector)
            ? boundary.BackSector
            : boundary.FrontSector;

        Assert.IsNotNull(conveyorSector);
        Assert.AreNotSame(conveyorSector, originSector);
        Assert.IsTrue(TouchesSector(thing, conveyorSector),
            "Test thing must overlap the conveyor sector through its radius.");
        Assert.IsFalse(IsInSectorThingList(conveyorSector, thing),
            "The regression requires the thing origin/subsector to belong to the neighbouring sector.");

        thing.Z = conveyorSector.FloorHeight;
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;

        var carryDx = Fixed.FromInt(2);
        var carryDy = Fixed.FromInt(-1);
        var scroller = new BoomScroller(BoomScrollerType.Carry, conveyorSector, carryDx, carryDy);

        scroller.Run();

        Assert.AreEqual(carryDx.Data, thing.MomX.Data);
        Assert.AreEqual(carryDy.Data, thing.MomY.Data);

        world.ThingMovement.UnsetThingPosition(thing);
        world.ThingMovement.RemoveTouchingSectorLinks(thing);
    }

    [TestMethod]
    public void CarryScrollerSkipsAirborneNoGravityAndNoClipThings()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        line.Special = (LineSpecial)252;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Carry);

        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        thing.Z = sector.FloorHeight + Fixed.One;
        scroller.Run();
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data);

        thing.Z = sector.FloorHeight;
        thing.Flags |= MobjFlags.NoGravity;
        scroller.Run();
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data);

        thing.Flags &= ~MobjFlags.NoGravity;
        thing.Flags |= MobjFlags.NoClip;
        scroller.Run();
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data);
    }

    [TestMethod]
    public void DisplacementFloorScrollerUsesControlHeightDelta()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var control = line.FrontSide.Sector;
        var sector = world.Map.Sectors.First(value => !object.ReferenceEquals(value, control));

        line.Special = (LineSpecial)246;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.FloorXOffset = Fixed.Zero;
        sector.FloorYOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Floor);
        var initialControlHeight = control.FloorHeight + control.CeilingHeight;

        Assert.AreSame(control, scroller.ControlSector);
        Assert.AreEqual(initialControlHeight.Data, scroller.LastHeight.Data);

        scroller.Run();
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorXOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorYOffset.Data);

        var firstDelta = Fixed.FromInt(8);
        control.FloorHeight += firstDelta;
        scroller.Run();

        Assert.AreEqual((scroller.Dx * firstDelta).Data, sector.FloorXOffset.Data);
        Assert.AreEqual((scroller.Dy * firstDelta).Data, sector.FloorYOffset.Data);

        var secondDelta = Fixed.FromInt(-3);
        control.CeilingHeight += secondDelta;
        scroller.Run();

        var totalDelta = firstDelta + secondDelta;
        Assert.AreEqual((scroller.Dx * totalDelta).Data, sector.FloorXOffset.Data);
        Assert.AreEqual((scroller.Dy * totalDelta).Data, sector.FloorYOffset.Data);
    }

    [TestMethod]
    public void DisplacementCeilingScrollerUsesFirstSideSectorAsControl()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var control = line.FrontSide.Sector;
        var sector = world.Map.Sectors.First(value => !object.ReferenceEquals(value, control));

        line.Special = (LineSpecial)245;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.CeilingXOffset = Fixed.Zero;
        sector.CeilingYOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Ceiling);
        var delta = Fixed.FromInt(5);

        control.CeilingHeight += delta;
        scroller.Run();

        Assert.AreSame(control, scroller.ControlSector);
        Assert.AreEqual((scroller.Dx * delta).Data, sector.CeilingXOffset.Data);
        Assert.AreEqual((scroller.Dy * delta).Data, sector.CeilingYOffset.Data);
    }

    [TestMethod]
    public void DisplacementCarryScrollerUsesControlHeightDelta()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;
        var line = FindUnusedLine(world, value => !object.ReferenceEquals(value.FrontSide.Sector, sector));
        var control = line.FrontSide.Sector;

        line.Special = (LineSpecial)247;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        thing.Z = sector.FloorHeight;
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Carry);
        var delta = Fixed.FromInt(4);

        scroller.Run();
        Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data);

        control.FloorHeight += delta;
        scroller.Run();

        Assert.AreSame(control, scroller.ControlSector);
        Assert.AreEqual((scroller.Dx * delta).Data, thing.MomX.Data);
        Assert.AreEqual((scroller.Dy * delta).Data, thing.MomY.Data);
    }

    [TestMethod]
    public void DisplacementScrollAndCarryCreatesTwoControlledScrollers()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;
        var line = FindUnusedLine(world, value => !object.ReferenceEquals(value.FrontSide.Sector, sector));
        var control = line.FrontSide.Sector;

        line.Special = (LineSpecial)248;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.FloorXOffset = Fixed.Zero;
        sector.FloorYOffset = Fixed.Zero;
        thing.Z = sector.FloorHeight;
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var floorScroller = FindScroller(world, sector, BoomScrollerType.Floor);
        var carryScroller = FindScroller(world, sector, BoomScrollerType.Carry);
        var delta = Fixed.FromInt(2);

        control.FloorHeight += delta;
        floorScroller.Run();
        carryScroller.Run();

        Assert.AreSame(control, floorScroller.ControlSector);
        Assert.AreSame(control, carryScroller.ControlSector);
        Assert.AreEqual((floorScroller.Dx * delta).Data, sector.FloorXOffset.Data);
        Assert.AreEqual((floorScroller.Dy * delta).Data, sector.FloorYOffset.Data);
        Assert.AreEqual((carryScroller.Dx * delta).Data, thing.MomX.Data);
        Assert.AreEqual((carryScroller.Dy * delta).Data, thing.MomY.Data);
    }

    [TestMethod]
    public void DisplacementWallScroller249UsesControlHeightDelta()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var target = FindUnusedLine(world, value => !object.ReferenceEquals(value, line));
        var side = target.FrontSide;
        var control = line.FrontSide.Sector;

        line.Special = (LineSpecial)249;
        line.Tag = TestTag;
        target.Tag = TestTag;
        side.TextureOffset = Fixed.Zero;
        side.RowOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        var baseVector = BoomScrollerVector.ResolveWall(line.Dx >> 5, line.Dy >> 5, target);

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, side);
        var delta = Fixed.FromInt(3);

        scroller.Run();
        Assert.AreEqual(Fixed.Zero.Data, side.TextureOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, side.RowOffset.Data);

        control.CeilingHeight += delta;
        scroller.Run();

        Assert.AreSame(control, scroller.ControlSector);
        Assert.AreEqual((baseVector.X * delta).Data, side.TextureOffset.Data);
        Assert.AreEqual((baseVector.Y * delta).Data, side.RowOffset.Data);
    }

    [TestMethod]
    public void WallProjectionMatchesBoomAxisRules()
    {
        var sector = new Sector(
            0,
            Fixed.Zero,
            Fixed.FromInt(128),
            0,
            0,
            160,
            0,
            0);
        var side = new SideDef(Fixed.Zero, Fixed.Zero, 0, 0, 0, sector);
        var horizontal = new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.FromInt(64), Fixed.Zero),
            0,
            0,
            0,
            side,
            null);
        var dx = Fixed.FromInt(2);
        var dy = Fixed.FromInt(3);

        var projected = BoomScrollerVector.ResolveWall(dx, dy, horizontal);
        var distance = Geometry.PointToDist(Fixed.Zero, Fixed.Zero, horizontal.Dx, horizontal.Dy);
        var expectedX = new Fixed((int)(-(long)dx.Data * horizontal.Dx.Data / distance.Data));
        var expectedY = new Fixed((int)((long)dy.Data * horizontal.Dx.Data / distance.Data));

        Assert.AreEqual(expectedX.Data, projected.X.Data);
        Assert.AreEqual(expectedY.Data, projected.Y.Data);
    }

    [TestMethod]
    public void StaticTaggedWallScroller254UsesProjectedVector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var target = FindUnusedLine(world, value => !object.ReferenceEquals(value, line));
        var side = target.FrontSide;

        line.Special = (LineSpecial)254;
        line.Tag = TestTag;
        target.Tag = TestTag;
        side.TextureOffset = Fixed.Zero;
        side.RowOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        var expected = BoomScrollerVector.ResolveWall(line.Dx >> 5, line.Dy >> 5, target);

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, side);

        Assert.IsNull(scroller.ControlSector);
        Assert.AreEqual(expected.X.Data, scroller.Dx.Data);
        Assert.AreEqual(expected.Y.Data, scroller.Dy.Data);

        scroller.Run();

        Assert.AreEqual(expected.X.Data, side.TextureOffset.Data);
        Assert.AreEqual(expected.Y.Data, side.RowOffset.Data);
    }

    [TestMethod]
    public void StaticTaggedWallScroller254DoesNotScrollTriggerLine()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var target = FindUnusedLine(world, value => !object.ReferenceEquals(value, line));

        line.Special = (LineSpecial)254;
        line.Tag = TestTag;
        target.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);

        Assert.IsNull(TryFindScroller(world, line.FrontSide));
        Assert.IsNotNull(TryFindScroller(world, target.FrontSide));
    }

    [TestMethod]
    public void StaticTaggedWallScrollerTargetsAreResolvedAtSpawn()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var target = FindUnusedLine(world, value => !object.ReferenceEquals(value, line));
        var side = target.FrontSide;

        line.Special = (LineSpecial)254;
        line.Tag = TestTag;
        target.Tag = TestTag;
        side.TextureOffset = Fixed.Zero;
        side.RowOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, side);

        target.Tag = 0;
        world.Map.BoomTags.Rebuild();
        scroller.Run();

        Assert.AreEqual(scroller.Dx.Data, side.TextureOffset.Data);
        Assert.AreEqual(scroller.Dy.Data, side.RowOffset.Data);
    }

    [TestMethod]
    public void TaggedPlaneScrollerTargetsAreResolvedAtSpawn()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var sector = world.Map.Sectors[0];

        line.Special = (LineSpecial)251;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.FloorXOffset = Fixed.Zero;
        sector.FloorYOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Floor);

        sector.Tag = 0;
        world.Map.BoomTags.Rebuild();
        scroller.Run();

        Assert.AreEqual(scroller.Dx.Data, sector.FloorXOffset.Data);
        Assert.AreEqual(scroller.Dy.Data, sector.FloorYOffset.Data);
    }

    [TestMethod]
    public void BoomType48RunsExactlyOncePerTic()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Vanilla };
        var world = new World(content, options, null);
        var line = FindUnusedLine(world);
        var side = line.FrontSide;

        line.Special = (LineSpecial)48;
        side.TextureOffset = Fixed.Zero;
        side.RowOffset = Fixed.Zero;
        options.Compatibility = GameCompatibility.Boom;

        world.Specials.SpawnSpecials();
        world.Thinkers.Run();
        world.Specials.Update();

        Assert.AreEqual(Fixed.One.Data, side.TextureOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, side.RowOffset.Data);
    }

    [TestMethod]
    public void AccelerativeFloorScrollerAccumulatesAndKeepsVelocity()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var control = line.FrontSide.Sector;
        var sector = world.Map.Sectors.First(value => !object.ReferenceEquals(value, control));

        line.Special = (LineSpecial)215;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.FloorXOffset = Fixed.Zero;
        sector.FloorYOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Floor);
        var delta = Fixed.FromInt(2);

        scroller.Run();
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorXOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorYOffset.Data);

        control.FloorHeight += delta;
        scroller.Run();
        var velocityX = scroller.Dx * delta;
        var velocityY = scroller.Dy * delta;

        Assert.IsTrue(scroller.IsAccelerative);
        Assert.AreEqual(velocityX.Data, scroller.VelocityDx.Data);
        Assert.AreEqual(velocityY.Data, scroller.VelocityDy.Data);
        Assert.AreEqual(velocityX.Data, sector.FloorXOffset.Data);
        Assert.AreEqual(velocityY.Data, sector.FloorYOffset.Data);

        scroller.Run();

        Assert.AreEqual(velocityX.Data, scroller.VelocityDx.Data);
        Assert.AreEqual(velocityY.Data, scroller.VelocityDy.Data);
        Assert.AreEqual((velocityX + velocityX).Data, sector.FloorXOffset.Data);
        Assert.AreEqual((velocityY + velocityY).Data, sector.FloorYOffset.Data);
    }

    [TestMethod]
    public void AccelerativeScrollerReverseControlMotionChangesVelocity()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var control = line.FrontSide.Sector;
        var sector = world.Map.Sectors.First(value => !object.ReferenceEquals(value, control));

        line.Special = (LineSpecial)214;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.CeilingXOffset = Fixed.Zero;
        sector.CeilingYOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Ceiling);
        var delta = Fixed.FromInt(3);

        control.CeilingHeight += delta;
        scroller.Run();
        var initialVelocityX = scroller.VelocityDx;
        var initialVelocityY = scroller.VelocityDy;

        control.CeilingHeight -= Fixed.FromInt(1);
        scroller.Run();

        Assert.AreEqual((initialVelocityX - scroller.Dx).Data, scroller.VelocityDx.Data);
        Assert.AreEqual((initialVelocityY - scroller.Dy).Data, scroller.VelocityDy.Data);
    }

    [TestMethod]
    public void AccelerativeCarry216CarriesWithoutScrollingFloor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;
        var line = FindUnusedLine(world, value => !object.ReferenceEquals(value.FrontSide.Sector, sector));
        var control = line.FrontSide.Sector;

        line.Special = (LineSpecial)216;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.FloorXOffset = Fixed.Zero;
        sector.FloorYOffset = Fixed.Zero;
        thing.Z = sector.FloorHeight;
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var carryScroller = FindScroller(world, sector, BoomScrollerType.Carry);

        Assert.IsNull(TryFindScroller(world, sector, BoomScrollerType.Floor));

        control.FloorHeight += Fixed.One;
        carryScroller.Run();
        var velocityX = carryScroller.Dx;
        var velocityY = carryScroller.Dy;

        Assert.IsTrue(carryScroller.IsAccelerative);
        Assert.AreEqual(velocityX.Data, thing.MomX.Data);
        Assert.AreEqual(velocityY.Data, thing.MomY.Data);
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorXOffset.Data);
        Assert.AreEqual(Fixed.Zero.Data, sector.FloorYOffset.Data);

        carryScroller.Run();

        Assert.AreEqual((velocityX + velocityX).Data, thing.MomX.Data);
        Assert.AreEqual((velocityY + velocityY).Data, thing.MomY.Data);
    }

    [TestMethod]
    public void AccelerativeScrollAndCarry217CreatesTwoControlledScrollers()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;
        var line = FindUnusedLine(world, value => !object.ReferenceEquals(value.FrontSide.Sector, sector));
        var control = line.FrontSide.Sector;

        line.Special = (LineSpecial)217;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.FloorXOffset = Fixed.Zero;
        sector.FloorYOffset = Fixed.Zero;
        thing.Z = sector.FloorHeight;
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var floorScroller = FindScroller(world, sector, BoomScrollerType.Floor);
        var carryScroller = FindScroller(world, sector, BoomScrollerType.Carry);
        var delta = Fixed.FromInt(2);

        control.FloorHeight += delta;
        floorScroller.Run();
        carryScroller.Run();

        Assert.IsTrue(floorScroller.IsAccelerative);
        Assert.IsTrue(carryScroller.IsAccelerative);
        Assert.AreEqual((floorScroller.Dx * delta).Data, sector.FloorXOffset.Data);
        Assert.AreEqual((floorScroller.Dy * delta).Data, sector.FloorYOffset.Data);
        Assert.AreEqual((carryScroller.Dx * delta).Data, thing.MomX.Data);
        Assert.AreEqual((carryScroller.Dy * delta).Data, thing.MomY.Data);
    }

    [TestMethod]
    public void AccelerativeWallScroller218UsesProjectedPersistentVelocity()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var target = FindUnusedLine(world, value => !object.ReferenceEquals(value, line));
        var side = target.FrontSide;
        var control = line.FrontSide.Sector;

        line.Special = (LineSpecial)218;
        line.Tag = TestTag;
        target.Tag = TestTag;
        side.TextureOffset = Fixed.Zero;
        side.RowOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        var projected = BoomScrollerVector.ResolveWall(line.Dx >> 5, line.Dy >> 5, target);

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, side);
        var delta = Fixed.FromInt(2);

        control.FloorHeight += delta;
        scroller.Run();
        var velocityX = projected.X * delta;
        var velocityY = projected.Y * delta;

        Assert.IsTrue(scroller.IsAccelerative);
        Assert.AreEqual(velocityX.Data, side.TextureOffset.Data);
        Assert.AreEqual(velocityY.Data, side.RowOffset.Data);

        scroller.Run();

        Assert.AreEqual((velocityX + velocityX).Data, side.TextureOffset.Data);
        Assert.AreEqual((velocityY + velocityY).Data, side.RowOffset.Data);
    }

    [TestMethod]
    public void AccelerativeScrollerTargetsAreResolvedAtSpawn()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var control = line.FrontSide.Sector;
        var sector = world.Map.Sectors.First(value => !object.ReferenceEquals(value, control));

        line.Special = (LineSpecial)215;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        sector.FloorXOffset = Fixed.Zero;
        sector.FloorYOffset = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);
        var scroller = FindScroller(world, sector, BoomScrollerType.Floor);

        sector.Tag = 0;
        world.Map.BoomTags.Rebuild();
        control.FloorHeight += Fixed.One;
        scroller.Run();

        Assert.AreEqual(scroller.Dx.Data, sector.FloorXOffset.Data);
        Assert.AreEqual(scroller.Dy.Data, sector.FloorYOffset.Data);
    }

    [TestMethod]
    public void VanillaDoesNotSpawnAccelerativeScrollers()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var line = FindUnusedLine(world);
        var control = line.FrontSide.Sector;
        var sector = world.Map.Sectors.First(value => !object.ReferenceEquals(value, control));

        line.Special = (LineSpecial)215;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);

        Assert.IsNull(TryFindScroller(world, sector, BoomScrollerType.Floor));
    }

    [TestMethod]
    public void VanillaDoesNotSpawnBoomOnlyWallScrollers()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var line = FindUnusedLine(world);
        var side = line.FrontSide;

        line.Special = (LineSpecial)85;
        side.TextureOffset = Fixed.Zero;
        side.RowOffset = Fixed.Zero;

        BoomScrollerSpawner.SpawnScrollers(world);

        Assert.IsNull(TryFindScroller(world, side));
    }

    [TestMethod]
    public void VanillaDoesNotSpawnStaticTaggedWallScroller254()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var line = FindUnusedLine(world);
        var target = FindUnusedLine(world, value => !object.ReferenceEquals(value, line));

        line.Special = (LineSpecial)254;
        line.Tag = TestTag;
        target.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);

        Assert.IsNull(TryFindScroller(world, target.FrontSide));
    }

    [TestMethod]
    public void VanillaDoesNotSpawnBoomPlaneScrollers()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var line = FindUnusedLine(world);
        var sector = world.Map.Sectors[0];

        line.Special = (LineSpecial)251;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);

        Assert.IsNull(TryFindScroller(world, sector, BoomScrollerType.Floor));
        Assert.IsNull(TryFindScroller(world, sector, BoomScrollerType.Carry));
    }

    [TestMethod]
    public void VanillaDoesNotSpawnDisplacementScrollers()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var line = FindUnusedLine(world);
        var control = line.FrontSide.Sector;
        var sector = world.Map.Sectors.First(value => !object.ReferenceEquals(value, control));

        line.Special = (LineSpecial)246;
        line.Tag = TestTag;
        sector.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        BoomScrollerSpawner.SpawnScrollers(world);

        Assert.IsNull(TryFindScroller(world, sector, BoomScrollerType.Floor));
    }

    private static bool TouchesSector(Mobj thing, Sector sector)
    {
        for (var node = thing.TouchingSectorList; node != null; node = node.ThingNext)
        {
            if (object.ReferenceEquals(node.Sector, sector))
                return true;
        }

        return false;
    }

    private static bool IsInSectorThingList(Sector sector, Mobj thing)
    {
        for (var current = sector.ThingList; current != null; current = current.SectorNext)
        {
            if (object.ReferenceEquals(current, thing))
                return true;
        }

        return false;
    }

    private static LineDef FindUnusedLine(World world)
    {
        return FindUnusedLine(world, static _ => true);
    }

    private static LineDef FindUnusedLine(World world, System.Func<LineDef, bool> predicate)
    {
        return world.Map.Lines.First(
            line =>
                line.FrontSide != null &&
                (int)line.Special == 0 &&
                ((line.Dx >> 5) != Fixed.Zero || (line.Dy >> 5) != Fixed.Zero) &&
                predicate(line));
    }

    private static BoomScroller FindScroller(World world, SideDef side)
    {
        var scroller = TryFindScroller(world, side);
        Assert.IsNotNull(scroller, "Expected BoomScroller was not spawned.");
        return scroller;
    }

    private static BoomScroller TryFindScroller(World world, SideDef side)
    {
        foreach (var thinker in world.Thinkers)
        {
            if (thinker is BoomScroller scroller && object.ReferenceEquals(scroller.Side, side))
                return scroller;
        }

        return null;
    }

    private static BoomScroller FindScroller(World world, Sector sector, BoomScrollerType type)
    {
        var scroller = TryFindScroller(world, sector, type);
        Assert.IsNotNull(scroller, "Expected Boom plane scroller was not spawned.");
        return scroller;
    }

    private static BoomScroller TryFindScroller(World world, Sector sector, BoomScrollerType type)
    {
        foreach (var thinker in world.Thinkers)
        {
            if (thinker is BoomScroller scroller &&
                scroller.Type == type &&
                object.ReferenceEquals(scroller.Sector, sector))
            {
                return scroller;
            }
        }

        return null;
    }
}
