using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomTriggerLifecycleTest
{
    [TestMethod]
    public void OnceAndRepeatPairsHaveExpectedLifecycle()
    {
        var once = new[]
        {
            BoomTriggerType.WalkOnce,
            BoomTriggerType.SwitchOnce,
            BoomTriggerType.GunOnce,
            BoomTriggerType.PushOnce
        };

        var repeat = new[]
        {
            BoomTriggerType.WalkRepeat,
            BoomTriggerType.SwitchRepeat,
            BoomTriggerType.GunRepeat,
            BoomTriggerType.PushRepeat
        };

        foreach (var trigger in once)
            Assert.IsTrue(BoomTriggerLifecycle.ConsumesSpecialOnSuccess(trigger), trigger.ToString());

        foreach (var trigger in repeat)
            Assert.IsFalse(BoomTriggerLifecycle.ConsumesSpecialOnSuccess(trigger), trigger.ToString());

        Assert.IsFalse(BoomTriggerLifecycle.UsesSwitchTexture(BoomTriggerType.WalkOnce));
        Assert.IsFalse(BoomTriggerLifecycle.UsesSwitchTexture(BoomTriggerType.WalkRepeat));
        Assert.IsTrue(BoomTriggerLifecycle.UsesSwitchTexture(BoomTriggerType.SwitchOnce));
        Assert.IsTrue(BoomTriggerLifecycle.UsesSwitchTexture(BoomTriggerType.SwitchRepeat));
        Assert.IsTrue(BoomTriggerLifecycle.UsesSwitchTexture(BoomTriggerType.GunOnce));
        Assert.IsTrue(BoomTriggerLifecycle.UsesSwitchTexture(BoomTriggerType.GunRepeat));
        Assert.IsFalse(BoomTriggerLifecycle.UsesSwitchTexture(BoomTriggerType.PushOnce));
        Assert.IsFalse(BoomTriggerLifecycle.UsesSwitchTexture(BoomTriggerType.PushRepeat));

        Assert.IsFalse(BoomTriggerLifecycle.ResetsSwitchTexture(BoomTriggerType.SwitchOnce));
        Assert.IsTrue(BoomTriggerLifecycle.ResetsSwitchTexture(BoomTriggerType.SwitchRepeat));
        Assert.IsFalse(BoomTriggerLifecycle.ResetsSwitchTexture(BoomTriggerType.GunOnce));
        Assert.IsTrue(BoomTriggerLifecycle.ResetsSwitchTexture(BoomTriggerType.GunRepeat));
    }

    [TestMethod]
    public void WalkAndPushOnceClearSpecialOnlyAfterSuccessLifecycleRuns()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var line = FindUsableLine(world);

        line.Special = (LineSpecial)0x6000;
        BoomTriggerLifecycle.ApplySuccess(world, line, BoomTriggerType.WalkOnce);
        Assert.AreEqual(0, (int)line.Special);

        line.Special = (LineSpecial)0x6006;
        BoomTriggerLifecycle.ApplySuccess(world, line, BoomTriggerType.PushOnce);
        Assert.AreEqual(0, (int)line.Special);
    }

    [TestMethod]
    public void WalkAndPushRepeatKeepSpecialAfterSuccess()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var line = FindUsableLine(world);

        line.Special = (LineSpecial)0x6001;
        BoomTriggerLifecycle.ApplySuccess(world, line, BoomTriggerType.WalkRepeat);
        Assert.AreEqual(0x6001, (int)line.Special);

        line.Special = (LineSpecial)0x6007;
        BoomTriggerLifecycle.ApplySuccess(world, line, BoomTriggerType.PushRepeat);
        Assert.AreEqual(0x6007, (int)line.Special);
    }

    [TestMethod]
    public void SwitchOnceChangesTextureAndConsumesSpecialPermanently()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var line = PrepareVanillaSwitch(world, content, out var off, out var on);

        line.Special = (LineSpecial)0x6002;
        BoomTriggerLifecycle.ApplySuccess(world, line, BoomTriggerType.SwitchOnce);

        Assert.AreEqual(0, (int)line.Special);
        Assert.AreEqual(on, line.FrontSide.TopTexture);

        for (var i = 0; i < 40; i++)
            world.Specials.Update();

        Assert.AreEqual(on, line.FrontSide.TopTexture);
        Assert.AreNotEqual(off, line.FrontSide.TopTexture);
    }

    [TestMethod]
    public void SwitchRepeatKeepsSpecialAndRestoresTextureAfterButtonTime()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var line = PrepareVanillaSwitch(world, content, out var off, out var on);

        line.Special = (LineSpecial)0x6003;
        BoomTriggerLifecycle.ApplySuccess(world, line, BoomTriggerType.SwitchRepeat);

        Assert.AreEqual(0x6003, (int)line.Special);
        Assert.AreEqual(on, line.FrontSide.TopTexture);

        for (var i = 0; i < 34; i++)
            world.Specials.Update();

        Assert.AreEqual(on, line.FrontSide.TopTexture);
        Assert.AreEqual(0x6003, (int)line.Special);

        world.Specials.Update();

        Assert.AreEqual(off, line.FrontSide.TopTexture);
        Assert.AreEqual(0x6003, (int)line.Special);
    }

    [TestMethod]
    public void GunRepeatUsesSameButtonResetLifecycleAsSwitchRepeat()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var line = PrepareVanillaSwitch(world, content, out var off, out var on);

        line.Special = (LineSpecial)0x6005;
        BoomTriggerLifecycle.ApplySuccess(world, line, BoomTriggerType.GunRepeat);

        Assert.AreEqual(0x6005, (int)line.Special);
        Assert.AreEqual(on, line.FrontSide.TopTexture);

        for (var i = 0; i < 35; i++)
            world.Specials.Update();

        Assert.AreEqual(off, line.FrontSide.TopTexture);
        Assert.AreEqual(0x6005, (int)line.Special);
    }

    [TestMethod]
    public void FailedOneShotActionDoesNotConsumeOrStartResetLifecycle()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateBoomWorld(content);
        var line = world.Map.Lines.First(l => l.Tag != 0 && l.SoundOrigin != null && l.FrontSide != null);
        var blocker = new Thinker();
        var off = content.Textures.GetNumber("SW1BRCOM");

        Assert.AreNotEqual(-1, off);
        line.FrontSide.TopTexture = off;
        line.FrontSide.MiddleTexture = 0;
        line.FrontSide.BottomTexture = 0;

        foreach (var sector in world.Map.Sectors.Where(s => s.Tag == line.Tag))
            sector.SpecialData = blocker;

        // Generalized floor: normal speed, no monster activation, S1 trigger.
        line.Special = (LineSpecial)(0x6000 | (1 << 3) | (int)BoomTriggerType.SwitchOnce);
        var originalSpecial = (int)line.Special;

        Assert.IsTrue(BoomLineSpecials.TryUse(world, line, 0, world.ConsolePlayer.Mobj, out var handled));
        Assert.IsTrue(handled);
        Assert.AreEqual(originalSpecial, (int)line.Special);
        Assert.AreEqual(off, line.FrontSide.TopTexture);

        for (var i = 0; i < 35; i++)
            world.Specials.Update();

        Assert.AreEqual(off, line.FrontSide.TopTexture);
    }

    private static World CreateBoomWorld(GameContent content) =>
        new(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Episode = 1,
            Map = 1,
            Compatibility = GameCompatibility.Boom
        }, null);

    private static LineDef FindUsableLine(World world) =>
        world.Map.Lines.First(l => l.SoundOrigin != null && l.FrontSide != null);

    private static LineDef PrepareVanillaSwitch(World world, GameContent content, out int off, out int on)
    {
        off = content.Textures.GetNumber("SW1BRCOM");
        on = content.Textures.GetNumber("SW2BRCOM");

        Assert.AreNotEqual(-1, off);
        Assert.AreNotEqual(-1, on);

        var line = FindUsableLine(world);
        line.FrontSide.TopTexture = off;
        line.FrontSide.MiddleTexture = 0;
        line.FrontSide.BottomTexture = 0;
        return line;
    }
}
