using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomDelayedDoorSpecialTest
{
    [TestMethod]
    public void TranslatorMapsBothExtendedDelayedDoors()
    {
        AssertDoor(175, BoomTriggerType.SwitchOnce, false);
        AssertDoor(196, BoomTriggerType.SwitchRepeat, true);

        // The original W1/WR variants remain on the vanilla path.
        Assert.IsFalse(BoomDelayedDoorTranslator.TryTranslate((LineSpecial)16, out _));
        Assert.IsFalse(BoomDelayedDoorTranslator.TryTranslate((LineSpecial)76, out _));
        Assert.IsFalse(BoomDelayedDoorTranslator.TryTranslate((LineSpecial)174, out _));
        Assert.IsFalse(BoomDelayedDoorTranslator.TryTranslate((LineSpecial)176, out _));
        Assert.IsFalse(BoomDelayedDoorTranslator.TryTranslate((LineSpecial)195, out _));
        Assert.IsFalse(BoomDelayedDoorTranslator.TryTranslate((LineSpecial)197, out _));
    }

    [TestMethod]
    public void SwitchOnceStartsClose30ThenOpenAndConsumesOnSuccess()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);
        var originalCeiling = sector.CeilingHeight;

        line.Special = (LineSpecial)175;

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var result));
        Assert.IsTrue(result);
        Assert.AreEqual(0, (int)line.Special);

        var door = sector.SpecialData as VerticalDoor;
        Assert.IsNotNull(door);
        Assert.AreEqual(VerticalDoorType.Close30ThenOpen, door.Type);
        Assert.AreEqual(-1, door.Direction);
        Assert.AreEqual(Fixed.FromInt(2).Data, door.Speed.Data);
        Assert.AreEqual(originalCeiling.Data, door.TopHeight.Data);
        Assert.AreEqual(150, door.TopWait);
    }

    [TestMethod]
    public void SwitchRepeatStartsSameDoorAndKeepsSpecial()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);

        line.Special = (LineSpecial)196;

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var result));
        Assert.IsTrue(result);
        Assert.AreEqual(196, (int)line.Special);

        var door = sector.SpecialData as VerticalDoor;
        Assert.IsNotNull(door);
        Assert.AreEqual(VerticalDoorType.Close30ThenOpen, door.Type);
        Assert.AreEqual(-1, door.Direction);
    }

    [TestMethod]
    public void SwitchOnceIsNotConsumedWhenDoorCannotStart()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);

        sector.SpecialData = new VerticalDoor(world) { Sector = sector };
        line.Special = (LineSpecial)175;

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var result));
        Assert.IsTrue(result);
        Assert.AreEqual(175, (int)line.Special);
    }

    [TestMethod]
    public void ExtendedDelayedDoorsRequirePlayerFrontSideAndNonZeroTag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);
        var nonPlayer = new Mobj(world);

        line.Special = (LineSpecial)175;
        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, nonPlayer, out var nonPlayerResult));
        Assert.IsFalse(nonPlayerResult);
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(175, (int)line.Special);

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 1, world.ConsolePlayer.Mobj, out var backSideResult));
        Assert.IsFalse(backSideResult);
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(175, (int)line.Special);

        line.Tag = 0;
        line.Special = (LineSpecial)196;
        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var zeroTagResult));
        Assert.IsFalse(zeroTagResult);
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(196, (int)line.Special);
    }

    private static void AssertDoor(int special, BoomTriggerType trigger, bool repeatable)
    {
        var specification = BoomDelayedDoorTranslator.Translate((LineSpecial)special);
        Assert.AreEqual(trigger, specification.Trigger);
        Assert.AreEqual(repeatable, specification.Repeatable);
    }

    private static World CreateBoomWorld(GameContent content)
    {
        return new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Boom
        }, null);
    }

    private static (LineDef line, Sector sector) PrepareTaggedSector(World world)
    {
        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        var sector = world.Map.Sectors.First(s =>
            s.SpecialData == null &&
            s.CeilingHeight > s.FloorHeight);

        var line = world.Map.Lines.First(l =>
            l.FrontSide != null &&
            l.FrontSide.Sector != null &&
            (l.Flags & LineFlags.Secret) == 0);

        const int tag = 30003;
        line.Tag = tag;
        sector.Tag = tag;
        world.Map.BoomTags.Rebuild();

        return (line, sector);
    }
}
