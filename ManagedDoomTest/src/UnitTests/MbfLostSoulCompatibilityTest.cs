using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfLostSoulCompatibilityTest
{
    [TestMethod]
    public void CompSoulRequiresMbfAndSelectsBuggyOrdering()
    {
        Assert.IsTrue(MbfLostSoulCompatibility.UsesCorrectBounce(
            GameCompatibility.Boom, GameVersion.Version109, compSoul: true));
        Assert.IsFalse(MbfLostSoulCompatibility.UsesCorrectBounce(
            GameCompatibility.Mbf, GameVersion.Version109, compSoul: true));
        Assert.IsTrue(MbfLostSoulCompatibility.UsesCorrectBounce(
            GameCompatibility.Mbf, GameVersion.Version109, compSoul: false));
    }

    [TestMethod]
    public void DefaultMbfKeepsBuggyFloorBounceOrdering()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, compSoul: true);
        var thing = CreateChargingSkull(world);

        thing.Z = Fixed.FromInt(1);
        thing.MomZ = Fixed.FromInt(-2);

        world.ThingMovement.ZMovement(thing);

        Assert.AreEqual(thing.FloorZ.Data, thing.Z.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomZ.Data);
    }

    [TestMethod]
    public void DisabledCompSoulUsesCorrectFloorBounceOrdering()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, compSoul: false);
        var thing = CreateChargingSkull(world);

        thing.Z = Fixed.FromInt(1);
        thing.MomZ = Fixed.FromInt(-2);

        world.ThingMovement.ZMovement(thing);

        Assert.AreEqual(thing.FloorZ.Data, thing.Z.Data);
        Assert.AreEqual(Fixed.FromInt(2).Data, thing.MomZ.Data);
    }

    [TestMethod]
    public void DefaultMbfKeepsBuggyCeilingBounceOrdering()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, compSoul: true);
        var thing = CreateChargingSkull(world);

        thing.Z = thing.CeilingZ - thing.Height - Fixed.One;
        thing.MomZ = Fixed.FromInt(2);

        world.ThingMovement.ZMovement(thing);

        Assert.AreEqual((thing.CeilingZ - thing.Height).Data, thing.Z.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomZ.Data);
    }

    [TestMethod]
    public void DisabledCompSoulUsesCorrectCeilingBounceOrdering()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, compSoul: false);
        var thing = CreateChargingSkull(world);

        thing.Z = thing.CeilingZ - thing.Height - Fixed.One;
        thing.MomZ = Fixed.FromInt(2);

        world.ThingMovement.ZMovement(thing);

        Assert.AreEqual((thing.CeilingZ - thing.Height).Data, thing.Z.Data);
        Assert.AreEqual(Fixed.FromInt(-2).Data, thing.MomZ.Data);
    }

    private static World CreateWorld(GameContent content, bool compSoul)
    {
        var options = new GameOptions
        {
            Compatibility = GameCompatibility.Mbf,
            GameVersion = GameVersion.Version109
        };
        options.MbfOptions.CompSoul = compSoul;
        return new World(content, options, null);
    }

    private static Mobj CreateChargingSkull(World world)
    {
        return new Mobj(world)
        {
            FloorZ = Fixed.Zero,
            CeilingZ = Fixed.FromInt(128),
            Height = Fixed.FromInt(56),
            Flags = MobjFlags.SkullFly | MobjFlags.NoGravity
        };
    }
}
