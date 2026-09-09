using System;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfStairCompatibilityTest
{
    [TestMethod]
    public void CompatibilityBoundarySelectsDoomOrBoomStairSemantics()
    {
        Assert.IsTrue(MbfStairCompatibility.UsesDoomStairSemantics(
            GameCompatibility.Vanilla, compStairs: false));
        Assert.IsTrue(MbfStairCompatibility.UsesDoomStairSemantics(
            GameCompatibility.Vanilla, compStairs: true));

        Assert.IsFalse(MbfStairCompatibility.UsesDoomStairSemantics(
            GameCompatibility.Boom, compStairs: false));
        Assert.IsFalse(MbfStairCompatibility.UsesDoomStairSemantics(
            GameCompatibility.Boom, compStairs: true));

        Assert.IsFalse(MbfStairCompatibility.UsesDoomStairSemantics(
            GameCompatibility.Mbf, compStairs: false));
        Assert.IsTrue(MbfStairCompatibility.UsesDoomStairSemantics(
            GameCompatibility.Mbf, compStairs: true));

        Assert.IsFalse(MbfStairCompatibility.UsesDoomStairSemantics(
            GameCompatibility.Mbf21, compStairs: false));
        Assert.IsTrue(MbfStairCompatibility.UsesDoomStairSemantics(
            GameCompatibility.Mbf21, compStairs: true));
    }

    [TestMethod]
    public void MbfDefaultKeepsBoomMultiTaggedStairScan()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf, compStairs: false);
        var (first, laterTagged, chained, trigger) = PrepareMultiTaggedStairCase(world);

        Assert.IsTrue(world.SectorAction.BuildStairs(trigger, StairType.Build8));

        Assert.IsNotNull(first.FloorData as FloorMove);
        Assert.IsNotNull(chained.FloorData as FloorMove);
        Assert.IsNotNull(laterTagged.FloorData as FloorMove);
    }

    [TestMethod]
    public void MbfCompStairsRestoresDoomMultiTaggedStarterSkip()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf, compStairs: true);
        var (first, laterTagged, chained, trigger) = PrepareMultiTaggedStairCase(world);

        Assert.IsTrue(world.SectorAction.BuildStairs(trigger, StairType.Build8));

        Assert.IsNotNull(first.FloorData as FloorMove);
        Assert.IsNotNull(chained.FloorData as FloorMove);
        Assert.IsNull(laterTagged.FloorData);
    }

    [TestMethod]
    public void BoomDoesNotConsumeStepHeightForBusyCandidate()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom, compStairs: true);
        var (first, free, trigger) = PrepareBusyCandidateStairCase(world);

        Assert.IsTrue(world.SectorAction.BuildStairs(trigger, StairType.Build8));

        var firstMove = first.FloorData as FloorMove;
        var freeMove = free.FloorData as FloorMove;
        Assert.IsNotNull(firstMove);
        Assert.IsNotNull(freeMove);
        Assert.AreEqual(Fixed.FromInt(8).Data, firstMove.FloorDestHeight.Data);
        Assert.AreEqual(Fixed.FromInt(16).Data, freeMove.FloorDestHeight.Data);
    }

    [TestMethod]
    public void MbfDefaultDoesNotConsumeStepHeightForBusyCandidate()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf, compStairs: false);
        var (first, free, trigger) = PrepareBusyCandidateStairCase(world);

        Assert.IsTrue(world.SectorAction.BuildStairs(trigger, StairType.Build8));

        var firstMove = first.FloorData as FloorMove;
        var freeMove = free.FloorData as FloorMove;
        Assert.IsNotNull(firstMove);
        Assert.IsNotNull(freeMove);
        Assert.AreEqual(Fixed.FromInt(8).Data, firstMove.FloorDestHeight.Data);
        Assert.AreEqual(Fixed.FromInt(16).Data, freeMove.FloorDestHeight.Data);
    }

    [TestMethod]
    public void MbfCompStairsConsumesStepHeightForBusyCandidateLikeDoom()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf, compStairs: true);
        var (first, free, trigger) = PrepareBusyCandidateStairCase(world);

        Assert.IsTrue(world.SectorAction.BuildStairs(trigger, StairType.Build8));

        var firstMove = first.FloorData as FloorMove;
        var freeMove = free.FloorData as FloorMove;
        Assert.IsNotNull(firstMove);
        Assert.IsNotNull(freeMove);
        Assert.AreEqual(Fixed.FromInt(8).Data, firstMove.FloorDestHeight.Data);
        Assert.AreEqual(Fixed.FromInt(24).Data, freeMove.FloorDestHeight.Data);
    }

    private static World CreateWorld(
        GameContent content,
        GameCompatibility compatibility,
        bool compStairs)
    {
        var options = new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = compatibility
        };
        options.MbfOptions.CompStairs = compStairs;
        return new World(content, options, null);
    }

    private static (Sector first, Sector laterTagged, Sector chained, LineDef trigger)
        PrepareMultiTaggedStairCase(World world)
    {
        foreach (var sector in world.Map.Sectors)
            sector.Tag = 0;

        const short tag = 30000;
        var first = CreateSector(0, tag);
        var laterTagged = CreateSector(1, tag);
        var chained = CreateSector(2, 0);

        first.Lines = new[] { CreateLine(first, chained) };
        laterTagged.Lines = Array.Empty<LineDef>();
        chained.Lines = Array.Empty<LineDef>();

        world.Map.Sectors[0] = first;
        world.Map.Sectors[1] = laterTagged;
        world.Map.Sectors[2] = chained;
        world.Map.BoomTags.Rebuild();

        var trigger = CreateLine(first, laterTagged);
        trigger.Tag = tag;

        return (first, laterTagged, chained, trigger);
    }

    private static (Sector first, Sector free, LineDef trigger)
        PrepareBusyCandidateStairCase(World world)
    {
        foreach (var sector in world.Map.Sectors)
            sector.Tag = 0;

        const short tag = 30000;
        var first = CreateSector(0, tag);
        var busy = CreateSector(1, 0);
        var free = CreateSector(2, 0);

        // Both candidates have the same floor texture as the first step. The busy
        // one is visited first so Doom's ordering consumes an otherwise unused
        // stair-height increment before the free candidate is selected.
        first.Lines = new[]
        {
            CreateLine(first, busy),
            CreateLine(first, free)
        };
        busy.Lines = Array.Empty<LineDef>();
        free.Lines = Array.Empty<LineDef>();
        busy.FloorData = new FloorMove(world);

        world.Map.Sectors[0] = first;
        world.Map.Sectors[1] = busy;
        world.Map.Sectors[2] = free;
        world.Map.BoomTags.Rebuild();

        var trigger = CreateLine(first, free);
        trigger.Tag = tag;

        return (first, free, trigger);
    }

    private static Sector CreateSector(int number, int tag)
    {
        return new Sector(
            number,
            Fixed.Zero,
            Fixed.FromInt(128),
            0,
            0,
            128,
            SectorSpecial.Normal,
            tag)
        {
            Lines = Array.Empty<LineDef>()
        };
    }

    private static LineDef CreateLine(Sector front, Sector back)
    {
        var frontSide = new SideDef(Fixed.Zero, Fixed.Zero, 0, 0, 0, front);
        var backSide = new SideDef(Fixed.Zero, Fixed.Zero, 0, 0, 0, back);

        return new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.FromInt(64), Fixed.Zero),
            LineFlags.TwoSided,
            (LineSpecial)0,
            0,
            frontSide,
            backSide);
    }
}
