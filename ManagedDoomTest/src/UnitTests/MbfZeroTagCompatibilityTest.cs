using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using ManagedDoom.Compatibility.Mbf.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfZeroTagCompatibilityTest
{
    [TestMethod]
    public void SelectorMatchesVanillaBoomMbfAndMbf21Boundaries()
    {
        var regular = CreateLine(tag: 0, special: 5);

        Assert.IsTrue(MbfZeroTagCompatibility.CanActivateRegularLine(
            regular, GameCompatibility.Vanilla, compZeroTags: false));
        Assert.IsFalse(MbfZeroTagCompatibility.CanActivateRegularLine(
            regular, GameCompatibility.Boom, compZeroTags: true));
        Assert.IsFalse(MbfZeroTagCompatibility.CanActivateRegularLine(
            regular, GameCompatibility.Mbf, compZeroTags: false));
        Assert.IsTrue(MbfZeroTagCompatibility.CanActivateRegularLine(
            regular, GameCompatibility.Mbf, compZeroTags: true));
        Assert.IsFalse(MbfZeroTagCompatibility.CanActivateRegularLine(
            regular, GameCompatibility.Mbf21, compZeroTags: false));
        Assert.IsTrue(MbfZeroTagCompatibility.CanActivateRegularLine(
            regular, GameCompatibility.Mbf21, compZeroTags: true));

        var whitelisted = CreateLine(tag: 0, special: 13);
        Assert.IsTrue(MbfZeroTagCompatibility.CanActivateRegularLine(
            whitelisted, GameCompatibility.Boom, compZeroTags: false));
        Assert.IsTrue(MbfZeroTagCompatibility.CanActivateRegularLine(
            whitelisted, GameCompatibility.Mbf, compZeroTags: false));
    }

    [TestMethod]
    public void MbfDefaultRejectsRegularZeroTagWalkOnceWithoutConsumingIt()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf, compZeroTags: false);
        var line = world.Map.Lines[0];

        SetAllSectorTags(world, 0);
        line.Tag = 0;
        line.Special = (LineSpecial)5; // W1 raise floor.
        world.Map.BoomTags.Rebuild();

        var activeFloorsBefore = CountActiveFloors(world);
        world.MapInteraction.CrossSpecialLine(line, 0, world.ConsolePlayer.Mobj);

        Assert.AreEqual(5, (int)line.Special);
        Assert.AreEqual(activeFloorsBefore, CountActiveFloors(world));
    }

    [TestMethod]
    public void CompZeroTagsAllowsRegularZeroTagWalkOnceAndConsumesIt()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf, compZeroTags: true);
        var line = world.Map.Lines[0];

        SetAllSectorTags(world, 0);
        line.Tag = 0;
        line.Special = (LineSpecial)5; // W1 raise floor.
        world.Map.BoomTags.Rebuild();

        world.MapInteraction.CrossSpecialLine(line, 0, world.ConsolePlayer.Mobj);

        Assert.AreEqual(0, (int)line.Special);
    }

    [TestMethod]
    public void CompZeroTagsAllowsRegularZeroTagUsePath()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf, compZeroTags: true);
        var line = world.Map.Lines.First(l => l.FrontSide != null);

        SetAllSectorTags(world, 0);
        line.Tag = 0;
        line.Special = (LineSpecial)101; // S1 raise floor.
        world.Map.BoomTags.Rebuild();

        Assert.IsTrue(world.MapInteraction.UseSpecialLine(world.ConsolePlayer.Mobj, line, 0));
    }

    [TestMethod]
    public void CompZeroTagsAllowsRegularZeroTagGunPath()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf, compZeroTags: true);
        var line = world.Map.Lines[0];

        SetAllSectorTags(world, 0);
        line.Tag = 0;
        line.Special = (LineSpecial)24; // G1 raise floor.
        world.Map.BoomTags.Rebuild();

        var activeFloorsBefore = CountActiveFloors(world);
        world.MapInteraction.ShootSpecialLine(world.ConsolePlayer.Mobj, line);

        Assert.IsTrue(CountActiveFloors(world) > activeFloorsBefore);
    }

    [TestMethod]
    public void CompZeroTagsDoesNotRelaxGeneralizedBoomTagRequirement()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf, compZeroTags: true);
        var line = world.Map.Lines.First(l => l.FrontSide != null);

        SetAllSectorTags(world, 0);
        line.Tag = 0;
        line.Special = (LineSpecial)(0x6000 | (int)BoomTriggerType.SwitchOnce);
        world.Map.BoomTags.Rebuild();

        var activeFloorsBefore = CountActiveFloors(world);
        Assert.IsTrue(BoomLineSpecials.TryUse(
            world, line, 0, world.ConsolePlayer.Mobj, out var result));
        Assert.IsFalse(result);
        Assert.AreEqual(activeFloorsBefore, CountActiveFloors(world));
        Assert.AreNotEqual(0, (int)line.Special);
    }

    private static World CreateWorld(
        GameContent content,
        GameCompatibility compatibility,
        bool compZeroTags)
    {
        var options = new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = compatibility
        };
        options.MbfOptions.CompZeroTags = compZeroTags;
        return new World(content, options, null);
    }

    private static LineDef CreateLine(int tag, int special)
    {
        return new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.One, Fixed.Zero),
            (LineFlags)0,
            (LineSpecial)special,
            (short)tag,
            null,
            null);
    }

    private static void SetAllSectorTags(World world, short tag)
    {
        foreach (var sector in world.Map.Sectors)
            sector.Tag = tag;
    }

    private static int CountActiveFloors(World world)
    {
        return world.Map.Sectors.Count(sector => sector.SpecialData is FloorMove);
    }
}
