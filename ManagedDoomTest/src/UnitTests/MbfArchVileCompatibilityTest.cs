using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfArchVileCompatibilityTest
{
    [TestMethod]
    public void CompatibilityGatePreservesBoomAndAddsMbfToggle()
    {
        Assert.IsFalse(MbfArchVileCompatibility.UsesFixedResurrection(
            GameCompatibility.Vanilla, compVile: false));
        Assert.IsTrue(MbfArchVileCompatibility.UsesFixedResurrection(
            GameCompatibility.Boom, compVile: true));

        Assert.IsTrue(MbfArchVileCompatibility.UsesFixedResurrection(
            GameCompatibility.Mbf, compVile: false));
        Assert.IsFalse(MbfArchVileCompatibility.UsesFixedResurrection(
            GameCompatibility.Mbf, compVile: true));
        Assert.IsFalse(MbfArchVileCompatibility.UsesFixedResurrection(
            GameCompatibility.Mbf21, compVile: true));
    }

    [TestMethod]
    public void CorrectedMbfFitCheckTemporarilyUsesRealMonsterDimensions()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var corpse = CreateCrushedCorpse(world);
        var info = corpse.Info;

        var state = MbfArchVileCompatibility.PrepareCorpseForFitCheck(
            corpse,
            GameCompatibility.Mbf,
            compVile: false);

        Assert.AreEqual(info.Height.Data, corpse.Height.Data);
        Assert.AreEqual(info.Radius.Data, corpse.Radius.Data);
        Assert.IsTrue((corpse.Flags & MobjFlags.Solid) != 0);

        MbfArchVileCompatibility.RestoreCorpseAfterFitCheck(
            corpse,
            state,
            GameCompatibility.Mbf,
            compVile: false);

        Assert.AreEqual(Fixed.Zero.Data, corpse.Height.Data);
        Assert.AreEqual(Fixed.Zero.Data, corpse.Radius.Data);
        Assert.IsFalse((corpse.Flags & MobjFlags.Solid) != 0);
    }

    [TestMethod]
    public void CompVileKeepsGhostDimensionsAfterResurrection()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var corpse = CreateCrushedCorpse(world);

        MbfArchVileCompatibility.ApplyResurrectionDimensions(
            corpse,
            GameCompatibility.Mbf,
            compVile: true);

        Assert.AreEqual(Fixed.Zero.Data, corpse.Height.Data);
        Assert.AreEqual(Fixed.Zero.Data, corpse.Radius.Data);

        corpse = CreateCrushedCorpse(world);
        MbfArchVileCompatibility.ApplyResurrectionDimensions(
            corpse,
            GameCompatibility.Mbf,
            compVile: false);

        Assert.AreEqual(corpse.Info.Height.Data, corpse.Height.Data);
        Assert.AreEqual(corpse.Info.Radius.Data, corpse.Radius.Data);
    }

    private static Mobj CreateCrushedCorpse(World world)
    {
        var corpse = world.ThingAllocation.SpawnMobj(
            world.ConsolePlayer.Mobj.X,
            world.ConsolePlayer.Mobj.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);

        corpse.Flags |= MobjFlags.Corpse;
        corpse.Flags &= ~MobjFlags.Solid;
        corpse.Health = 0;
        corpse.Height = Fixed.Zero;
        corpse.Radius = Fixed.Zero;
        corpse.Tics = -1;
        return corpse;
    }
}
