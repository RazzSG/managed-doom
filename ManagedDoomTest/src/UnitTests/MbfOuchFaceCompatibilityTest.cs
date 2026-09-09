using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfOuchFaceCompatibilityTest
{
    [TestMethod]
    public void CompatibilityBoundaryMatchesPrBoomAndMbf21Rules()
    {
        Assert.IsTrue(MbfOuchFaceCompatibility.UsesBuggyCode(
            GameCompatibility.Vanilla, compOuchFace: false));
        Assert.IsTrue(MbfOuchFaceCompatibility.UsesBuggyCode(
            GameCompatibility.Boom, compOuchFace: false));

        Assert.IsFalse(MbfOuchFaceCompatibility.UsesBuggyCode(
            GameCompatibility.Mbf, compOuchFace: false));
        Assert.IsTrue(MbfOuchFaceCompatibility.UsesBuggyCode(
            GameCompatibility.Mbf, compOuchFace: true));

        Assert.IsFalse(MbfOuchFaceCompatibility.UsesBuggyCode(
            GameCompatibility.Mbf21, compOuchFace: false));
        Assert.IsFalse(MbfOuchFaceCompatibility.UsesBuggyCode(
            GameCompatibility.Mbf21, compOuchFace: true));
    }

    [TestMethod]
    public void FixedCodeUsesDamageTakenInsteadOfBuggyHealthGainDelta()
    {
        Assert.IsFalse(MbfOuchFaceCompatibility.ShouldShowOuchFace(
            GameCompatibility.Mbf,
            compOuchFace: true,
            currentHealth: 70,
            previousHealth: 100,
            muchPain: StatusBar.Face.MuchPain));

        Assert.IsTrue(MbfOuchFaceCompatibility.ShouldShowOuchFace(
            GameCompatibility.Mbf,
            compOuchFace: false,
            currentHealth: 70,
            previousHealth: 100,
            muchPain: StatusBar.Face.MuchPain));

        Assert.IsTrue(MbfOuchFaceCompatibility.ShouldShowOuchFace(
            GameCompatibility.Mbf,
            compOuchFace: true,
            currentHealth: 130,
            previousHealth: 100,
            muchPain: StatusBar.Face.MuchPain));
    }

    [TestMethod]
    public void FixedMonsterDamageUsesHigherOuchPriority()
    {
        Assert.AreEqual(7, MbfOuchFaceCompatibility.ResolveMonsterDamagePriority(
            GameCompatibility.Vanilla, compOuchFace: false, legacyPriority: 7, fixedPriority: 8));
        Assert.AreEqual(7, MbfOuchFaceCompatibility.ResolveMonsterDamagePriority(
            GameCompatibility.Mbf, compOuchFace: true, legacyPriority: 7, fixedPriority: 8));
        Assert.AreEqual(8, MbfOuchFaceCompatibility.ResolveMonsterDamagePriority(
            GameCompatibility.Mbf, compOuchFace: false, legacyPriority: 7, fixedPriority: 8));
        Assert.AreEqual(8, MbfOuchFaceCompatibility.ResolveMonsterDamagePriority(
            GameCompatibility.Mbf21, compOuchFace: true, legacyPriority: 7, fixedPriority: 8));
    }

    [TestMethod]
    public void StatusBarConsumesRuntimeCompOuchFace()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var fixedOptions = new GameOptions { Compatibility = GameCompatibility.Mbf };
        var fixedWorld = new World(content, fixedOptions, null);
        PrepareAlivePlayer(fixedWorld);
        ApplyMonsterDamage(fixedWorld, 30);
        fixedWorld.StatusBar.Update();

        Assert.AreEqual(ExpectedOuchFaceIndex(70), fixedWorld.StatusBar.FaceIndex);

        // The PrBoom fix also raises the monster-damage priority so the OUCH
        // face is not overwritten on the following tic while DamageCount is active.
        fixedWorld.StatusBar.Update();
        Assert.AreEqual(ExpectedOuchFaceIndex(70), fixedWorld.StatusBar.FaceIndex);

        var buggyOptions = new GameOptions { Compatibility = GameCompatibility.Mbf };
        buggyOptions.MbfOptions.CompOuchFace = true;
        var buggyWorld = new World(content, buggyOptions, null);
        PrepareAlivePlayer(buggyWorld);
        ApplyMonsterDamage(buggyWorld, 30);
        buggyWorld.StatusBar.Update();

        Assert.AreNotEqual(ExpectedOuchFaceIndex(70), buggyWorld.StatusBar.FaceIndex);
    }

    [TestMethod]
    public void Mbf21IgnoresCompOuchFaceAndKeepsTheFix()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf21 };
        options.MbfOptions.CompOuchFace = true;

        var world = new World(content, options, null);
        PrepareAlivePlayer(world);
        ApplyMonsterDamage(world, 30);
        world.StatusBar.Update();

        Assert.AreEqual(ExpectedOuchFaceIndex(70), world.StatusBar.FaceIndex);
        world.StatusBar.Update();
        Assert.AreEqual(ExpectedOuchFaceIndex(70), world.StatusBar.FaceIndex);
    }

    private static void PrepareAlivePlayer(World world)
    {
        var player = world.ConsolePlayer;
        player.Health = 100;
        player.Mobj.Health = 100;
        player.DamageCount = 0;
        player.BonusCount = 0;
        player.Attacker = null;

        world.StatusBar.Reset();
        world.StatusBar.Update();
    }

    private static void ApplyMonsterDamage(World world, int damage)
    {
        var player = world.ConsolePlayer;
        var attacker = world.ThingAllocation.SpawnMobj(
            player.Mobj.X + Fixed.FromInt(64),
            player.Mobj.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);

        player.Health -= damage;
        player.Mobj.Health -= damage;
        player.DamageCount = damage;
        player.Attacker = attacker;
    }

    private static int ExpectedOuchFaceIndex(int health)
    {
        var painOffset = StatusBar.Face.Stride *
            (((100 - health) * StatusBar.Face.PainFaceCount) / 101);
        return painOffset + StatusBar.Face.OuchOffset;
    }
}
