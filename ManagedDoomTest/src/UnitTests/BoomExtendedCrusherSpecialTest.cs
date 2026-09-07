using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomExtendedCrusherSpecialTest
{
    [TestMethod]
    public void TranslatorMapsAllExtendedCrusherActions()
    {
        AssertCrusher(150, BoomExtendedCrusherAction.Silent, BoomTriggerType.WalkRepeat, true);
        AssertCrusher(164, BoomExtendedCrusherAction.Fast, BoomTriggerType.SwitchOnce, false);
        AssertCrusher(165, BoomExtendedCrusherAction.Silent, BoomTriggerType.SwitchOnce, false);
        AssertCrusher(168, BoomExtendedCrusherAction.Stop, BoomTriggerType.SwitchOnce, false);
        AssertCrusher(183, BoomExtendedCrusherAction.Fast, BoomTriggerType.SwitchRepeat, true);
        AssertCrusher(184, BoomExtendedCrusherAction.Slow, BoomTriggerType.SwitchRepeat, true);
        AssertCrusher(185, BoomExtendedCrusherAction.Silent, BoomTriggerType.SwitchRepeat, true);
        AssertCrusher(188, BoomExtendedCrusherAction.Stop, BoomTriggerType.SwitchRepeat, true);

        // 141 is the original W1 silent crusher already handled by vanilla Doom.
        Assert.IsFalse(BoomExtendedCrusherTranslator.TryTranslate((LineSpecial)141, out _));
        Assert.IsFalse(BoomExtendedCrusherTranslator.TryTranslate((LineSpecial)149, out _));
        Assert.IsFalse(BoomExtendedCrusherTranslator.TryTranslate((LineSpecial)151, out _));
        Assert.IsFalse(BoomExtendedCrusherTranslator.TryTranslate((LineSpecial)189, out _));
    }

    [TestMethod]
    public void SwitchOnceFastCrusherStartsAndConsumesOnSuccess()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);

        line.Special = (LineSpecial)164;

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var result));
        Assert.IsTrue(result);
        Assert.AreEqual(0, (int)line.Special);

        var ceiling = sector.SpecialData as CeilingMove;
        Assert.IsNotNull(ceiling);
        Assert.AreEqual(CeilingMoveType.FastCrushAndRaise, ceiling.Type);
        Assert.AreEqual((SectorAction.CeilingSpeed * 2).Data, ceiling.Speed.Data);
        Assert.AreEqual(-1, ceiling.Direction);
        Assert.IsTrue(ceiling.Crush);
    }

    [TestMethod]
    public void WalkRepeatSilentCrusherKeepsSpecialAndUsesSilentMover()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);

        line.Special = (LineSpecial)150;

        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 1, world.ConsolePlayer.Mobj));
        Assert.AreEqual(150, (int)line.Special);

        var ceiling = sector.SpecialData as CeilingMove;
        Assert.IsNotNull(ceiling);
        Assert.AreEqual(CeilingMoveType.SilentCrushAndRaise, ceiling.Type);
        Assert.AreEqual(SectorAction.CeilingSpeed.Data, ceiling.Speed.Data);
        Assert.AreEqual(-1, ceiling.Direction);
        Assert.IsTrue(ceiling.Crush);
    }

    [TestMethod]
    public void RepeatableCrusherCanBeStoppedAndRestartedFromStasis()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);

        line.Special = (LineSpecial)184;
        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var started));
        Assert.IsTrue(started);

        var ceiling = sector.SpecialData as CeilingMove;
        Assert.IsNotNull(ceiling);
        Assert.AreEqual(CeilingMoveType.CrushAndRaise, ceiling.Type);
        Assert.AreEqual(-1, ceiling.Direction);
        Assert.AreEqual(184, (int)line.Special);

        line.Special = (LineSpecial)188;
        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var stopped));
        Assert.IsTrue(stopped);
        Assert.AreEqual(0, ceiling.Direction);
        Assert.AreEqual(-1, ceiling.OldDirection);
        Assert.AreEqual(ThinkerState.InStasis, ceiling.ThinkerState);
        Assert.AreEqual(188, (int)line.Special);

        line.Special = (LineSpecial)184;
        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var restarted));
        Assert.IsTrue(restarted);
        Assert.AreEqual(-1, ceiling.Direction);
        Assert.AreEqual(ThinkerState.Active, ceiling.ThinkerState);
        Assert.AreEqual(184, (int)line.Special);
        Assert.AreSame(ceiling, sector.SpecialData);
    }

    [TestMethod]
    public void ExtendedCrushersRequirePlayerAndNonZeroTag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var (line, sector) = PrepareTaggedSector(world);
        var nonPlayer = new Mobj(world);

        line.Special = (LineSpecial)150;
        Assert.IsTrue(BoomLineSpecials.TryCross(world, line, 0, nonPlayer));
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(150, (int)line.Special);

        line.Tag = 0;
        line.Special = (LineSpecial)164;
        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var result));
        Assert.IsFalse(result);
        Assert.IsNull(sector.SpecialData);
        Assert.AreEqual(164, (int)line.Special);
    }

    private static void AssertCrusher(
        int special,
        BoomExtendedCrusherAction action,
        BoomTriggerType trigger,
        bool repeatable)
    {
        var specification = BoomExtendedCrusherTranslator.Translate((LineSpecial)special);
        Assert.AreEqual(action, specification.Action);
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

        const int tag = 30001;
        line.Tag = tag;
        sector.Tag = tag;
        world.Map.BoomTags.Rebuild();

        return (line, sector);
    }
}
