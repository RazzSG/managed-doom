using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Friction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomFrictionTest
{
    private const short TestTag = 32000;

    [TestMethod]
    public void TranslatorMatchesBoomLengthFormula()
    {
        var resolved = BoomFrictionTranslator.Resolve(Fixed.FromInt(100), Fixed.Zero);

        Assert.AreEqual(59391, resolved.Friction.Data);
        Assert.AreEqual(255, resolved.MoveFactor.Data);
    }

    [TestMethod]
    public void TranslatorClampsMbfEditFullIceControlLine()
    {
        // MBFEDIT!.WAD MAP01 line 370 runs from (-2904,3048) to
        // (-2512,3056), i.e. delta (392,8). The raw Boom formula exceeds
        // FRACUNIT and produces a negative move factor unless the canonical
        // post-calculation clamps are applied.
        var resolved = BoomFrictionTranslator.Resolve(
            Fixed.FromInt(392),
            Fixed.FromInt(8));

        Assert.AreEqual(Fixed.FracUnit, resolved.Friction.Data);
        Assert.AreEqual(BoomFrictionTranslator.MinimumMoveFactorData, resolved.MoveFactor.Data);
    }

    [TestMethod]
    public void TranslatorClampsExtremeSludgeMoveFactor()
    {
        var resolved = BoomFrictionTranslator.Resolve(Fixed.Zero, Fixed.Zero);

        Assert.AreEqual(0xd000, resolved.Friction.Data);
        Assert.AreEqual(BoomFrictionTranslator.MinimumMoveFactorData, resolved.MoveFactor.Data);
    }

    [TestMethod]
    public void MbfEditFullIcePlayerInputKeepsPositiveThrust()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var player = world.ConsolePlayer;
        var thing = player.Mobj;
        var sector = thing.Subsector.Sector;
        var resolved = BoomFrictionTranslator.Resolve(
            Fixed.FromInt(392),
            Fixed.FromInt(8));

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = resolved.Friction;
        sector.MoveFactor = resolved.MoveFactor;
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.Z = thing.FloorZ = sector.FloorHeight;
        thing.Angle = Angle.Ang0;
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        player.Cmd.Clear();
        player.Cmd.ForwardMove = 10;

        world.PlayerBehavior.MovePlayer(player);

        var expectedMove = resolved.MoveFactor * player.Cmd.ForwardMove;
        var expectedMomX = expectedMove * Trig.Cos(thing.Angle);
        var expectedMomY = expectedMove * Trig.Sin(thing.Angle);

        // DOOM's fine-angle cosine table represents cos(0) as 65535/65536,
        // not an exact FRACUNIT. Therefore a 320-unit fixed thrust becomes
        // 319 after the same fixed-point multiplication used by Thrust().
        Assert.AreEqual(expectedMomX.Data, thing.MomX.Data);
        Assert.AreEqual(expectedMomY.Data, thing.MomY.Data);
        Assert.IsTrue(thing.MomX > Fixed.Zero);
    }

    [TestMethod]
    public void InitializeResolvesTaggedSectorOnceAtMapStartup()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindUnusedLine(world);
        var target = world.Map.Sectors[0];
        var untouched = world.Map.Sectors.First(sector => !ReferenceEquals(sector, target));

        line.Special = (LineSpecial)223;
        line.Tag = TestTag;
        target.Tag = TestTag;
        untouched.Tag = TestTag - 1;
        target.Friction = Fixed.Zero;
        target.MoveFactor = Fixed.Zero;
        untouched.Friction = Fixed.Zero;
        untouched.MoveFactor = Fixed.Zero;
        world.Map.BoomTags.Rebuild();

        BoomSectorFriction.Initialize(world);
        var expected = BoomFrictionTranslator.Resolve(line.Dx, line.Dy);

        Assert.AreEqual(expected.Friction.Data, target.Friction.Data);
        Assert.AreEqual(expected.MoveFactor.Data, target.MoveFactor.Data);
        Assert.AreEqual(BoomFrictionTranslator.OriginalFrictionData, untouched.Friction.Data);
        Assert.AreEqual(BoomFrictionTranslator.OriginalMoveFactorData, untouched.MoveFactor.Data);
    }

    [TestMethod]
    public void FrictionMaskControlsWhetherResolvedSectorFrictionIsActive()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;
        var custom = new Fixed(0xd800);

        thing.Z = sector.FloorHeight;
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        sector.Friction = custom;
        sector.Special = 0;

        Assert.AreEqual(BoomFrictionTranslator.OriginalFrictionData, BoomSectorFriction.GetFriction(thing).Data);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;

        Assert.AreEqual(custom.Data, BoomSectorFriction.GetFriction(thing).Data);
    }

    [TestMethod]
    public void SludgeMoveFactorUsesBoomMomentumThresholds()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        thing.Z = sector.FloorHeight;
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = new Fixed(0xd800);
        sector.MoveFactor = new Fixed(100);

        thing.MomY = Fixed.Zero;

        thing.MomX = new Fixed(BoomFrictionTranslator.MoreFrictionMomentumData);
        Assert.AreEqual(100, BoomSectorFriction.GetMoveFactor(thing).Data);

        thing.MomX = new Fixed(BoomFrictionTranslator.MoreFrictionMomentumData + 1);
        Assert.AreEqual(200, BoomSectorFriction.GetMoveFactor(thing).Data);

        thing.MomX = new Fixed((BoomFrictionTranslator.MoreFrictionMomentumData << 1) + 1);
        Assert.AreEqual(400, BoomSectorFriction.GetMoveFactor(thing).Data);

        thing.MomX = new Fixed((BoomFrictionTranslator.MoreFrictionMomentumData << 2) + 1);
        Assert.AreEqual(800, BoomSectorFriction.GetMoveFactor(thing).Data);
    }

    [TestMethod]
    public void IceMoveFactorDoesNotUseSludgeMomentumScaling()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        thing.Z = sector.FloorHeight;
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.MomX = Fixed.FromInt(20);
        thing.MomY = Fixed.Zero;
        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = new Fixed(0xf000);
        sector.MoveFactor = new Fixed(1000);

        Assert.AreEqual(1000, BoomSectorFriction.GetMoveFactor(thing).Data);
    }

    [TestMethod]
    public void NoClipNoGravityAndAirbornePlayersUseNormalFriction()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = new Fixed(0xd800);
        thing.Z = sector.FloorHeight;

        thing.Flags |= MobjFlags.NoClip;
        Assert.AreEqual(BoomFrictionTranslator.OriginalFrictionData, BoomSectorFriction.GetFriction(thing).Data);

        thing.Flags &= ~MobjFlags.NoClip;
        thing.Flags |= MobjFlags.NoGravity;
        Assert.AreEqual(BoomFrictionTranslator.OriginalFrictionData, BoomSectorFriction.GetFriction(thing).Data);

        thing.Flags &= ~MobjFlags.NoGravity;
        thing.Z = sector.FloorHeight + Fixed.One;
        Assert.AreEqual(BoomFrictionTranslator.OriginalFrictionData, BoomSectorFriction.GetFriction(thing).Data);
    }

    [TestMethod]
    public void ThingMovementUsesBoomSectorFrictionForPlayerCoasting()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;
        var customFriction = new Fixed(0xd800);
        var initialMomX = new Fixed(0x1001);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = customFriction;
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.Z = thing.FloorZ = sector.FloorHeight;
        thing.MomX = initialMomX;
        thing.MomY = Fixed.Zero;
        thing.Player.Cmd.Clear();

        world.ThingMovement.XYMovement(thing);

        Assert.AreEqual((initialMomX * customFriction).Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data);
    }

    [TestMethod]
    public void VanillaThingMovementIgnoresBoomSectorFriction()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var thing = world.ConsolePlayer.Mobj;
        var sector = thing.Subsector.Sector;
        var initialMomX = new Fixed(0x1001);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = new Fixed(0xd800);
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.Z = thing.FloorZ = sector.FloorHeight;
        thing.MomX = initialMomX;
        thing.MomY = Fixed.Zero;
        thing.Player.Cmd.Clear();

        world.ThingMovement.XYMovement(thing);

        Assert.AreEqual((initialMomX * BoomFrictionTranslator.OriginalFriction).Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data);
    }

    [TestMethod]
    public void PlayerMovementUsesResolvedBoomMoveFactor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;
        var thing = player.Mobj;
        var sector = thing.Subsector.Sector;
        var moveFactor = new Fixed(1000);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = new Fixed(0xf000);
        sector.MoveFactor = moveFactor;
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.Z = thing.FloorZ = sector.FloorHeight;
        thing.Angle = Angle.Ang0;
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        player.Cmd.Clear();
        player.Cmd.ForwardMove = 10;

        world.PlayerBehavior.MovePlayer(player);

        var expectedMove = moveFactor * 10;
        Assert.AreEqual((expectedMove * Trig.Cos(Angle.Ang0)).Data, thing.MomX.Data);
        Assert.AreEqual((expectedMove * Trig.Sin(Angle.Ang0)).Data, thing.MomY.Data);
    }

    [TestMethod]
    public void VanillaPlayerMovementKeepsOriginalMoveFactor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var player = world.ConsolePlayer;
        var thing = player.Mobj;
        var sector = thing.Subsector.Sector;

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = new Fixed(0xf000);
        sector.MoveFactor = new Fixed(1000);
        thing.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity);
        thing.Z = thing.FloorZ = sector.FloorHeight;
        thing.Angle = Angle.Ang0;
        thing.MomX = Fixed.Zero;
        thing.MomY = Fixed.Zero;
        player.Cmd.Clear();
        player.Cmd.ForwardMove = 10;

        world.PlayerBehavior.MovePlayer(player);

        var expectedMove = new Fixed(10 * BoomFrictionTranslator.OriginalMoveFactorData);
        Assert.AreEqual((expectedMove * Trig.Cos(Angle.Ang0)).Data, thing.MomX.Data);
        Assert.AreEqual((expectedMove * Trig.Sin(Angle.Ang0)).Data, thing.MomY.Data);
    }

    private static LineDef FindUnusedLine(World world)
    {
        return world.Map.Lines.First(line => line.FrontSide != null && (int)line.Special == 0);
    }
}
