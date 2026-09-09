using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Friction;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfPlayerBobbingTest
{
    [TestMethod]
    public void MbfEditFullIceUsesNormalEffortForBobButMinimumPhysicalThrust()
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
        player.BobMomX = Fixed.Zero;
        player.BobMomY = Fixed.Zero;
        player.Cmd.Clear();
        player.Cmd.ForwardMove = 10;

        world.PlayerBehavior.MovePlayer(player);

        var expectedPhysicalMove = resolved.MoveFactor * player.Cmd.ForwardMove;
        var expectedPhysicalX = expectedPhysicalMove * Trig.Cos(thing.Angle);
        var expectedBobMove = BoomFrictionTranslator.OriginalMoveFactor * player.Cmd.ForwardMove;
        var expectedBobX = expectedBobMove * Trig.Cos(thing.Angle);

        Assert.AreEqual(expectedPhysicalX.Data, thing.MomX.Data);
        Assert.AreEqual(expectedBobX.Data, player.BobMomX.Data);
        Assert.IsTrue(player.BobMomX > thing.MomX);
    }

    [TestMethod]
    public void MbfCalcHeightUsesIndependentPlayerAppliedMomentum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var player = world.ConsolePlayer;

        player.Mobj.MomX = Fixed.Zero;
        player.Mobj.MomY = Fixed.Zero;
        player.BobMomX = Fixed.FromInt(2);
        player.BobMomY = Fixed.Zero;

        world.PlayerBehavior.CalcHeight(player);

        var expected = (player.BobMomX * player.BobMomX) >> 2;
        Assert.AreEqual(expected.Data, player.Bob.Data);
        Assert.IsTrue(player.Bob > Fixed.Zero);
    }

    [TestMethod]
    public void MbfEditIceMovementMovesReadyPspriteEvenWithTinyPhysicalThrust()
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
        player.BobMomX = Fixed.Zero;
        player.BobMomY = Fixed.Zero;
        player.Cmd.Clear();
        player.Cmd.ForwardMove = 10;

        world.PlayerBehavior.MovePlayer(player);
        world.PlayerBehavior.CalcHeight(player);

        var psp = player.PlayerSprites[(int)PlayerSprite.Weapon];
        player.PendingWeapon = WeaponType.NoChange;
        player.Health = 100;
        world.LevelTime = 0;
        world.WeaponBehavior.WeaponReady(player, psp);

        Assert.IsTrue(player.Bob > Fixed.Zero);
        Assert.AreNotEqual(Fixed.One.Data, psp.Sx.Data);
    }

    [TestMethod]
    public void MbfAndLaterBobMomentumAlwaysUsesOriginalFrictionForCoastingDecay()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        foreach (var compatibility in new[]
                 {
                     GameCompatibility.Mbf,
                     GameCompatibility.Mbf21
                 })
        {
            var world = new World(content, new GameOptions { Compatibility = compatibility }, null);
            var player = world.ConsolePlayer;

            player.BobMomX = Fixed.FromInt(4);
            player.BobMomY = Fixed.FromInt(-2);

            var expectedX = player.BobMomX * BoomFrictionTranslator.OriginalFriction;
            var expectedY = player.BobMomY * BoomFrictionTranslator.OriginalFriction;

            MbfPlayerBobbing.ApplyOriginalFriction(player, compatibility);

            Assert.AreEqual(expectedX.Data, player.BobMomX.Data, compatibility.ToString());
            Assert.AreEqual(expectedY.Data, player.BobMomY.Data, compatibility.ToString());
        }
    }

    [TestMethod]
    public void BoomProfileUsesPhysicalMomentumForBobbing()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;

        player.Mobj.MomX = Fixed.FromInt(2);
        player.Mobj.MomY = Fixed.Zero;
        player.BobMomX = Fixed.FromInt(20);
        player.BobMomY = Fixed.Zero;

        world.PlayerBehavior.CalcHeight(player);

        var expected = (player.Mobj.MomX * player.Mobj.MomX) >> 2;
        Assert.AreEqual(expected.Data, player.Bob.Data);
    }

    [TestMethod]
    public void BoomStationaryPhysicalMomentumHasNoBobEvenWithStoredInputBobMomentum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer;

        player.Mobj.MomX = Fixed.Zero;
        player.Mobj.MomY = Fixed.Zero;
        player.BobMomX = Fixed.FromInt(20);
        player.BobMomY = Fixed.Zero;

        world.PlayerBehavior.CalcHeight(player);

        Assert.AreEqual(Fixed.Zero.Data, player.Bob.Data);
    }

    [TestMethod]
    public void BoomEditFullIceDoesNotCreateIndependentInputBobMomentum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
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
        player.BobMomX = Fixed.Zero;
        player.BobMomY = Fixed.Zero;
        player.Cmd.Clear();
        player.Cmd.ForwardMove = 10;

        world.PlayerBehavior.MovePlayer(player);
        world.PlayerBehavior.CalcHeight(player);

        var expectedPhysicalMove = resolved.MoveFactor * player.Cmd.ForwardMove;
        var expectedPhysicalX = expectedPhysicalMove * Trig.Cos(thing.Angle);
        var expectedBob = (thing.MomX * thing.MomX + thing.MomY * thing.MomY) >> 2;

        Assert.AreEqual(expectedPhysicalX.Data, thing.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, player.BobMomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, player.BobMomY.Data);
        Assert.AreEqual(expectedBob.Data, player.Bob.Data);
    }

    [TestMethod]
    public void NormalTeleportClearsMbfBobMomentumDuringReactionFreeze()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        foreach (var compatibility in new[]
                 {
                     GameCompatibility.Mbf,
                     GameCompatibility.Mbf21
                 })
        {
            var world = new World(content, new GameOptions { Compatibility = compatibility }, null);
            var player = world.ConsolePlayer;
            var thing = player.Mobj;
            var line = world.Map.Lines[0];
            var destinationSector = thing.Subsector.Sector;

            foreach (var sector in world.Map.Sectors)
                sector.Tag = 0;

            line.Special = (LineSpecial)97;
            line.Tag = 30000;
            destinationSector.Tag = 30000;
            world.Map.BoomTags.Rebuild();

            var destination = world.ThingAllocation.SpawnMobj(
                thing.X,
                thing.Y,
                Mobj.OnFloorZ,
                MobjType.Teleportman);
            destination.Angle = Angle.Ang90;

            thing.MomX = Fixed.FromInt(3);
            thing.MomY = Fixed.FromInt(-2);
            player.BobMomX = Fixed.FromInt(12);
            player.BobMomY = Fixed.FromInt(-7);
            player.Cmd.Clear();
            player.Cmd.ForwardMove = (sbyte)PlayerBehavior.ForwardMove[1];

            Assert.IsTrue(world.SectorAction.Teleport(line, 0, thing), compatibility.ToString());

            Assert.AreEqual(Fixed.Zero.Data, thing.MomX.Data, compatibility.ToString());
            Assert.AreEqual(Fixed.Zero.Data, thing.MomY.Data, compatibility.ToString());
            Assert.AreEqual(Fixed.Zero.Data, player.BobMomX.Data, compatibility.ToString());
            Assert.AreEqual(Fixed.Zero.Data, player.BobMomY.Data, compatibility.ToString());
            Assert.AreEqual(18, thing.ReactionTime, compatibility.ToString());

            // Keep the movement key held exactly like the MBFEDIT reproduction.
            // ReactionTime suppresses MovePlayer, so no fresh bob momentum may be
            // created during the post-teleport freeze.
            world.PlayerBehavior.PlayerThink(player);

            Assert.AreEqual(17, thing.ReactionTime, compatibility.ToString());
            Assert.AreEqual(Fixed.Zero.Data, player.BobMomX.Data, compatibility.ToString());
            Assert.AreEqual(Fixed.Zero.Data, player.BobMomY.Data, compatibility.ToString());
            Assert.AreEqual(Fixed.Zero.Data, player.Bob.Data, compatibility.ToString());
        }
    }

    [TestMethod]
    public void VanillaProfileStillUsesPhysicalMomentumForBobbing()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var player = world.ConsolePlayer;

        player.Mobj.MomX = Fixed.FromInt(2);
        player.Mobj.MomY = Fixed.Zero;
        player.BobMomX = Fixed.FromInt(20);
        player.BobMomY = Fixed.Zero;

        world.PlayerBehavior.CalcHeight(player);

        var expected = (player.Mobj.MomX * player.Mobj.MomX) >> 2;
        Assert.AreEqual(expected.Data, player.Bob.Data);
    }
}
