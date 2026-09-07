using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomExtendedStairSpecialTest
{
    [TestMethod]
    public void TranslatorMapsAllExtendedStairActions()
    {
        AssertStair(256, BoomExtendedStairAction.Build8, BoomTriggerType.WalkRepeat);
        AssertStair(257, BoomExtendedStairAction.Turbo16, BoomTriggerType.WalkRepeat);
        AssertStair(258, BoomExtendedStairAction.Build8, BoomTriggerType.SwitchRepeat);
        AssertStair(259, BoomExtendedStairAction.Turbo16, BoomTriggerType.SwitchRepeat);

        // Original S1/W1 stair builders stay on the vanilla path.
        Assert.IsFalse(BoomExtendedStairTranslator.TryTranslate((LineSpecial)7, out _));
        Assert.IsFalse(BoomExtendedStairTranslator.TryTranslate((LineSpecial)8, out _));
        Assert.IsFalse(BoomExtendedStairTranslator.TryTranslate((LineSpecial)100, out _));
        Assert.IsFalse(BoomExtendedStairTranslator.TryTranslate((LineSpecial)127, out _));
        Assert.IsFalse(BoomExtendedStairTranslator.TryTranslate((LineSpecial)255, out _));
        Assert.IsFalse(BoomExtendedStairTranslator.TryTranslate((LineSpecial)260, out _));
    }

    [TestMethod]
    public void WalkRepeatBuild8UsesClassicStairMoverAndKeepsSpecial()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareIsolatedTaggedSector(world);

        line.Special = (LineSpecial)256;

        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 1, world.ConsolePlayer.Mobj));
        Assert.AreEqual(256, (int)line.Special);

        var floor = sector.SpecialData as FloorMove;
        Assert.IsNotNull(floor);
        Assert.AreEqual(1, floor.Direction);
        Assert.AreEqual((Fixed.One / 4).Data, floor.Speed.Data);
        Assert.AreEqual(Fixed.FromInt(8).Data, floor.FloorDestHeight.Data);
        Assert.IsFalse(floor.Crush);
        Assert.AreEqual(0, sector.StairLock);
    }

    [TestMethod]
    public void SwitchRepeatTurbo16UsesClassicStairMoverWithoutDirectionToggle()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareIsolatedTaggedSector(world);

        line.Special = (LineSpecial)259;

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var result));
        Assert.IsTrue(result);
        Assert.AreEqual(259, (int)line.Special);

        var floor = sector.SpecialData as FloorMove;
        Assert.IsNotNull(floor);
        Assert.AreEqual(1, floor.Direction);
        Assert.AreEqual((Fixed.One * 4).Data, floor.Speed.Data);
        Assert.AreEqual(Fixed.FromInt(16).Data, floor.FloorDestHeight.Data);
        Assert.IsFalse(floor.Crush);
        Assert.AreEqual(0, sector.StairLock);
    }

    [TestMethod]
    public void ExtendedStairsRequirePlayerAndNonZeroTag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareIsolatedTaggedSector(world);
        var nonPlayer = new Mobj(world);

        line.Special = (LineSpecial)256;
        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, nonPlayer));
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(256, (int)line.Special);

        line.Tag = 0;
        line.Special = (LineSpecial)258;
        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var result));
        Assert.IsFalse(result);
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(258, (int)line.Special);
    }

    private static void AssertStair(int special, BoomExtendedStairAction action, BoomTriggerType trigger)
    {
        var specification = BoomExtendedStairTranslator.Translate((LineSpecial)special);
        Assert.AreEqual(action, specification.Action);
        Assert.AreEqual(trigger, specification.Trigger);
        Assert.IsTrue(specification.Repeatable);
    }

    private static World CreateBoomWorld(GameContent content)
    {
        return new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Boom
        }, null);
    }

    private static (LineDef line, Sector sector) PrepareIsolatedTaggedSector(World world)
    {
        foreach (var mapSector in world.Map.Sectors)
        {
            mapSector.Tag = 0;
            mapSector.StairLock = 0;
            mapSector.StairPreviousSector = -1;
            mapSector.StairNextSector = -1;
            mapSector.FloorFlat = 10000 + mapSector.Number;
        }

        var sector = world.Map.Sectors.First(s =>
            s.SpecialData == null &&
            s.CeilingHeight > s.FloorHeight);
        sector.FloorHeight = Fixed.Zero;
        sector.CeilingHeight = Fixed.FromInt(128);

        var line = world.Map.Lines.First(l =>
            l.FrontSide != null &&
            l.FrontSide.Sector != null &&
            (l.Flags & LineFlags.Secret) == 0);

        const int tag = 30002;
        line.Tag = tag;
        sector.Tag = tag;
        world.Map.BoomTags.Rebuild();

        return (line, sector);
    }
}
