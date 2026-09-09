using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfDropoffCompatibilityTest
{
    [TestMethod]
    public void ClassicBlockingStartsAtMbfAndFollowsCompFlag()
    {
        Assert.IsFalse(MbfDropoffCompatibility.UsesClassicBlocking(
            GameCompatibility.Boom, true));
        Assert.IsFalse(MbfDropoffCompatibility.UsesClassicBlocking(
            GameCompatibility.Mbf, false));
        Assert.IsTrue(MbfDropoffCompatibility.UsesClassicBlocking(
            GameCompatibility.Mbf, true));
        Assert.IsTrue(MbfDropoffCompatibility.UsesClassicBlocking(
            GameCompatibility.Mbf21, true));
    }

    [TestMethod]
    public void DefaultMbfProfileAllowsTorqueMomentumToCrossTallDropoffs()
    {
        var options = new MbfOptions();

        Assert.IsFalse(options.CompDropoff);
        Assert.IsTrue(MbfDropoffCompatibility.AllowsExternalMomentumDropoff(
            GameCompatibility.Mbf,
            options.CompDropoff,
            requested: true));
    }

    [TestMethod]
    public void ExternalMomentumPermissionRequiresCorrectedMbfMode()
    {
        Assert.IsFalse(MbfDropoffCompatibility.AllowsExternalMomentumDropoff(
            GameCompatibility.Boom, false, true));
        Assert.IsFalse(MbfDropoffCompatibility.AllowsExternalMomentumDropoff(
            GameCompatibility.Mbf, true, true));
        Assert.IsFalse(MbfDropoffCompatibility.AllowsExternalMomentumDropoff(
            GameCompatibility.Mbf, false, false));
        Assert.IsTrue(MbfDropoffCompatibility.AllowsExternalMomentumDropoff(
            GameCompatibility.Mbf, false, true));
    }

    [TestMethod]
    public void RecoveryRequiresTallGroundedDropoffAndCorrectedMode()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);
        var actor = SpawnActor(world);

        actor.FloorZ = Fixed.FromInt(64);
        actor.DropoffZ = Fixed.FromInt(39);
        actor.Z = actor.FloorZ;

        Assert.IsTrue(MbfDropoffCompatibility.ShouldRecoverFromDropoff(
            GameCompatibility.Mbf, false, actor));
        Assert.IsFalse(MbfDropoffCompatibility.ShouldRecoverFromDropoff(
            GameCompatibility.Mbf, true, actor));
        Assert.IsFalse(MbfDropoffCompatibility.ShouldRecoverFromDropoff(
            GameCompatibility.Boom, false, actor));

        actor.DropoffZ = Fixed.FromInt(40);
        Assert.IsFalse(MbfDropoffCompatibility.ShouldRecoverFromDropoff(
            GameCompatibility.Mbf, false, actor));

        actor.DropoffZ = Fixed.FromInt(39);
        actor.Flags |= MobjFlags.DropOff;
        Assert.IsFalse(MbfDropoffCompatibility.ShouldRecoverFromDropoff(
            GameCompatibility.Mbf, false, actor));
    }

    [TestMethod]
    public void HorizontalDropoffLineProducesDoomFineAngleEscapeVector()
    {
        var low = CreateSector(0, 0);
        var high = CreateSector(1, 64);
        var line = CreateHorizontalLine(low, high);

        var contributes = MbfDropoffCompatibility.TryGetLineAvoidanceDelta(
            line,
            Fixed.FromInt(64),
            Fixed.FromInt(-16),
            Fixed.FromInt(16),
            Fixed.FromInt(-16),
            Fixed.FromInt(16),
            out var deltaX,
            out var deltaY);

        Assert.IsTrue(contributes);

        // DOOM's finesine table is midpoint-sampled: fineSine[0] is 25,
        // not an exact mathematical zero, and fineCosine[0] is 65535.
        // P_AvoidDropoff therefore produces (-800, 2097120) in 16.16
        // fixed-point for this nominally vertical 32-unit escape vector.
        Assert.AreEqual(-(Trig.Sin(Angle.Ang0) * 32).Data, deltaX.Data);
        Assert.AreEqual((Trig.Cos(Angle.Ang0) * 32).Data, deltaY.Data);
    }

    [TestMethod]
    public void ExactlyTwentyFourUnitEdgeDoesNotRequestRecovery()
    {
        var low = CreateSector(0, 40);
        var high = CreateSector(1, 64);
        var line = CreateHorizontalLine(low, high);

        Assert.IsFalse(MbfDropoffCompatibility.TryGetLineAvoidanceDelta(
            line,
            Fixed.FromInt(64),
            Fixed.FromInt(-16),
            Fixed.FromInt(16),
            Fixed.FromInt(-16),
            Fixed.FromInt(16),
            out _,
            out _));
    }

    private static Sector CreateSector(int number, int floorHeight)
    {
        return new Sector(
            number,
            Fixed.FromInt(floorHeight),
            Fixed.FromInt(128),
            0,
            0,
            160,
            SectorSpecial.Normal,
            0);
    }

    private static LineDef CreateHorizontalLine(Sector front, Sector back)
    {
        var frontSide = new SideDef(
            Fixed.Zero,
            Fixed.Zero,
            0,
            0,
            0,
            front);
        var backSide = new SideDef(
            Fixed.Zero,
            Fixed.Zero,
            0,
            0,
            0,
            back);

        return new LineDef(
            new Vertex(Fixed.FromInt(-64), Fixed.Zero),
            new Vertex(Fixed.FromInt(64), Fixed.Zero),
            LineFlags.TwoSided,
            LineSpecial.Normal,
            0,
            frontSide,
            backSide);
    }

    private static Mobj SpawnActor(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            player.Z,
            MobjType.Troop);
    }
}
