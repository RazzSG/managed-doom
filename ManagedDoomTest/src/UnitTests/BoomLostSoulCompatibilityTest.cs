using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomLostSoulCompatibilityTest
{
    [TestMethod]
    public void BoomAndDescendantsAlwaysUseCorrectLostSoulBounce()
    {
        Assert.IsFalse(BoomLostSoulCompatibility.UsesCorrectBounce(
            GameCompatibility.Vanilla, GameVersion.Version109));
        Assert.IsTrue(BoomLostSoulCompatibility.UsesCorrectBounce(
            GameCompatibility.Vanilla, GameVersion.Ultimate));

        Assert.IsTrue(BoomLostSoulCompatibility.UsesCorrectBounce(
            GameCompatibility.Boom, GameVersion.Version109));
        Assert.IsTrue(BoomLostSoulCompatibility.UsesCorrectBounce(
            GameCompatibility.Mbf, GameVersion.Version109));
        Assert.IsTrue(BoomLostSoulCompatibility.UsesCorrectBounce(
            GameCompatibility.Mbf21, GameVersion.Version109));
    }

    [TestMethod]
    public void BoomVersion109BouncesChargingSkullOffFloor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom, GameVersion.Version109);
        var thing = CreateChargingSkull(world);

        thing.Z = Fixed.FromInt(1);
        thing.MomZ = Fixed.FromInt(-2);

        world.ThingMovement.ZMovement(thing);

        Assert.AreEqual(thing.FloorZ.Data, thing.Z.Data);
        Assert.AreEqual(Fixed.FromInt(2).Data, thing.MomZ.Data);
    }

    [TestMethod]
    public void VanillaVersion109KeepsBuggyFloorBounceOrdering()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Vanilla, GameVersion.Version109);
        var thing = CreateChargingSkull(world);

        thing.Z = Fixed.FromInt(1);
        thing.MomZ = Fixed.FromInt(-2);

        world.ThingMovement.ZMovement(thing);

        Assert.AreEqual(thing.FloorZ.Data, thing.Z.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomZ.Data);
    }

    [TestMethod]
    public void BoomVersion109BouncesChargingSkullOffCeiling()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom, GameVersion.Version109);
        var thing = CreateChargingSkull(world);

        thing.Z = thing.CeilingZ - thing.Height - Fixed.One;
        thing.MomZ = Fixed.FromInt(2);

        world.ThingMovement.ZMovement(thing);

        Assert.AreEqual((thing.CeilingZ - thing.Height).Data, thing.Z.Data);
        Assert.AreEqual(Fixed.FromInt(-2).Data, thing.MomZ.Data);
    }

    [TestMethod]
    public void VanillaVersion109KeepsBuggyCeilingBounceOrdering()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Vanilla, GameVersion.Version109);
        var thing = CreateChargingSkull(world);

        thing.Z = thing.CeilingZ - thing.Height - Fixed.One;
        thing.MomZ = Fixed.FromInt(2);

        world.ThingMovement.ZMovement(thing);

        Assert.AreEqual((thing.CeilingZ - thing.Height).Data, thing.Z.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomZ.Data);
    }

    [TestMethod]
    public void VanillaVersion109CanReverseDownwardMomentumWhenCeilingMovesOntoSkull()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Vanilla, GameVersion.Version109);
        var thing = CreateChargingSkull(world);

        thing.Z = thing.CeilingZ - thing.Height + Fixed.FromInt(4);
        thing.MomZ = Fixed.FromInt(-1);

        world.ThingMovement.ZMovement(thing);

        Assert.AreEqual((thing.CeilingZ - thing.Height).Data, thing.Z.Data);
        Assert.AreEqual(Fixed.FromInt(1).Data, thing.MomZ.Data);
    }

    [TestMethod]
    public void VanillaUltimateUsesCorrectCeilingBounceOrdering()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Vanilla, GameVersion.Ultimate);
        var thing = CreateChargingSkull(world);

        thing.Z = thing.CeilingZ - thing.Height - Fixed.One;
        thing.MomZ = Fixed.FromInt(2);

        world.ThingMovement.ZMovement(thing);

        Assert.AreEqual(Fixed.FromInt(-2).Data, thing.MomZ.Data);
    }

    private static World CreateWorld(
        GameContent content,
        GameCompatibility compatibility,
        GameVersion gameVersion)
    {
        return new World(content, new GameOptions
        {
            Compatibility = compatibility,
            GameVersion = gameVersion
        }, null);
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
