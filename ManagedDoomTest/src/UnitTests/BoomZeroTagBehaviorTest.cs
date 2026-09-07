using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomZeroTagBehaviorTest
{
    private static readonly int[] AllowedZeroTagSpecials =
    {
        // Manual doors.
        1, 26, 27, 28, 31, 32, 33, 34, 117, 118,

        // Lighting.
        139, 170, 79, 35, 138, 171, 81, 13, 192, 169,
        80, 12, 194, 173, 157, 104, 193, 172, 156, 17,

        // Thing teleporters.
        195, 174, 97, 39, 126, 125, 210, 209, 208, 207,

        // Exits.
        11, 52, 197, 51, 124, 198,

        // Scrolling walls.
        48, 85
    };

    [TestMethod]
    public void ZeroTagWhitelistMatchesBoomPCheckTag()
    {
        for (var special = 0; special <= 300; special++)
        {
            var expected = AllowedZeroTagSpecials.Contains(special);
            Assert.AreEqual(
                expected,
                BoomTagRules.AllowsZeroTag((LineSpecial)special),
                $"Unexpected zero-tag rule for linedef special {special}.");
        }
    }

    [TestMethod]
    public void VanillaKeepsLegacyZeroTagBehaviorWhileBoomAndDescendantsRejectRegularTaggedActions()
    {
        var line = CreateLine(0, 5);

        Assert.IsTrue(BoomTagRules.CanActivate(line, GameCompatibility.Vanilla));
        Assert.IsFalse(BoomTagRules.CanActivate(line, GameCompatibility.Boom));
        Assert.IsFalse(BoomTagRules.CanActivate(line, GameCompatibility.Mbf));
        Assert.IsFalse(BoomTagRules.CanActivate(line, GameCompatibility.Mbf21));

        line.Tag = 1;
        Assert.IsTrue(BoomTagRules.CanActivate(line, GameCompatibility.Boom));
        Assert.IsTrue(BoomTagRules.CanActivate(line, GameCompatibility.Mbf));
        Assert.IsTrue(BoomTagRules.CanActivate(line, GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void BoomUseRejectsRegularZeroTagSwitchBeforeDispatch()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var line = world.Map.Lines.First(l => l.FrontSide != null);

        line.Tag = 0;
        line.Special = (LineSpecial)101; // S1 raise floor.

        Assert.IsFalse(world.MapInteraction.UseSpecialLine(world.ConsolePlayer.Mobj, line, 0));
        Assert.AreEqual(101, (int)line.Special);
    }

    [TestMethod]
    public void BoomCrossRejectsRegularZeroTagWalkOnceWithoutConsumingIt()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
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
    public void VanillaCrossKeepsLegacyZeroTagActivation()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Vanilla);
        var line = world.Map.Lines[0];

        SetAllSectorTags(world, 0);
        line.Tag = 0;
        line.Special = (LineSpecial)5; // W1 raise floor.
        world.Map.BoomTags.Rebuild();

        world.MapInteraction.CrossSpecialLine(line, 0, world.ConsolePlayer.Mobj);

        Assert.AreEqual(0, (int)line.Special);
    }

    [TestMethod]
    public void BoomCrossStillAllowsWhitelistedZeroTagLightingSpecial()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var line = world.Map.Lines[0];

        SetAllSectorTags(world, 0);
        line.Tag = 0;
        line.Special = (LineSpecial)13; // W1 light to 255.
        world.Map.BoomTags.Rebuild();

        world.MapInteraction.CrossSpecialLine(line, 0, world.ConsolePlayer.Mobj);

        Assert.AreEqual(0, (int)line.Special);
    }

    [TestMethod]
    public void BoomShootRejectsRegularZeroTagSpecialBeforeStartingFloorMotion()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var line = world.Map.Lines[0];

        SetAllSectorTags(world, 0);
        line.Tag = 0;
        line.Special = (LineSpecial)24; // G1 raise floor.
        world.Map.BoomTags.Rebuild();

        var activeFloorsBefore = CountActiveFloors(world);
        world.MapInteraction.ShootSpecialLine(world.ConsolePlayer.Mobj, line);

        Assert.AreEqual(activeFloorsBefore, CountActiveFloors(world));
        Assert.AreEqual(24, (int)line.Special);
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility)
    {
        return new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = compatibility
        }, null);
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
