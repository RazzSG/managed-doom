using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfActorPhysicsFlagsTest
{
    [TestMethod]
    public void ActorPhysicsFlagsStartAtMbf()
    {
        Assert.IsFalse(GameCompatibilityFeatures.SupportsMbfActorPhysicsFlags(GameCompatibility.Boom));
        Assert.IsTrue(GameCompatibilityFeatures.SupportsMbfActorPhysicsFlags(GameCompatibility.Mbf));
        Assert.IsTrue(GameCompatibilityFeatures.SupportsMbfActorPhysicsFlags(GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void RestingNonSentientActorArmsOnlyInMbf()
    {
        using var boomContent = GameContent.CreateDummy(WadPath.Doom2);
        var boom = CreateWorld(boomContent, GameCompatibility.Boom);
        var boomActor = SpawnBarrel(boom, Fixed.Zero);
        boomActor.Flags |= MobjFlags.Touchy;
        boomActor.Run();
        Assert.IsFalse(boomActor.MbfTouchyArmed);

        using var mbfContent = GameContent.CreateDummy(WadPath.Doom2);
        var mbf = CreateWorld(mbfContent, GameCompatibility.Mbf);
        var mbfActor = SpawnBarrel(mbf, Fixed.Zero);
        mbfActor.Flags |= MobjFlags.Touchy;
        mbfActor.Run();
        Assert.IsTrue(mbfActor.MbfTouchyArmed);
    }

    [TestMethod]
    public void TouchyThingContactRequiresArmedOrSentientActor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var mover = world.ConsolePlayer.Mobj;
        var touchy = SpawnBarrel(world, Fixed.Zero);
        touchy.Flags |= MobjFlags.Touchy | MobjFlags.Shootable | MobjFlags.Solid;

        touchy.MbfTouchyArmed = false;
        Assert.IsFalse(MbfTouchyCompatibility.ShouldActivateOnThingContact(
            GameCompatibility.Mbf,
            touchy,
            mover));

        touchy.MbfTouchyArmed = true;
        Assert.IsTrue(MbfTouchyCompatibility.ShouldActivateOnThingContact(
            GameCompatibility.Mbf,
            touchy,
            mover));
    }

    [TestMethod]
    public void RuntimeCheckPositionConsumesArmedTouchyFlag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var mover = world.ConsolePlayer.Mobj;
        var touchy = world.ThingAllocation.SpawnMobj(
            mover.X,
            mover.Y,
            Mobj.OnFloorZ,
            MobjType.Barrel);

        touchy.Flags |= MobjFlags.Touchy | MobjFlags.Shootable | MobjFlags.Solid;
        touchy.MbfTouchyArmed = true;
        touchy.Health = 20;

        world.ThingMovement.CheckPosition(mover, mover.X, mover.Y);

        Assert.IsTrue(touchy.Health <= 0);
    }

    [DataTestMethod]
    [DataRow(GameCompatibility.Vanilla)]
    [DataRow(GameCompatibility.Boom)]
    public void TouchyOnlyActorDoesNotEnterSharedCollisionPathBeforeMbf(
        GameCompatibility compatibility)
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, compatibility);
        var mover = world.ConsolePlayer.Mobj;
        var touchy = world.ThingAllocation.SpawnMobj(
            mover.X,
            mover.Y,
            Mobj.OnFloorZ,
            MobjType.Barrel);

        // A plain TOUCHY bit is MBF metadata. With an older forced profile it
        // must not be enough to make an otherwise inert actor participate in
        // PIT_CheckThing. SkullFly is intentional here: the shared Doom branch
        // makes the leak observable by consuming the mover's SkullFly state.
        touchy.Flags = MobjFlags.Touchy;
        mover.Flags |= MobjFlags.SkullFly;
        mover.MomX = Fixed.FromInt(3);

        var allowed = world.ThingMovement.CheckPosition(mover, mover.X, mover.Y);

        Assert.IsTrue(allowed, compatibility.ToString());
        Assert.IsTrue((mover.Flags & MobjFlags.SkullFly) != 0, compatibility.ToString());
        Assert.AreEqual(Fixed.FromInt(3).Data, mover.MomX.Data, compatibility.ToString());
    }

    [TestMethod]
    public void TouchyCollisionFastPathStartsAtMbf()
    {
        var actor = new Mobj(null)
        {
            Flags = MobjFlags.Touchy
        };

        Assert.IsFalse(MbfTouchyCompatibility.RequiresThingCollisionCheck(
            GameCompatibility.Vanilla, actor));
        Assert.IsFalse(MbfTouchyCompatibility.RequiresThingCollisionCheck(
            GameCompatibility.Boom, actor));
        Assert.IsTrue(MbfTouchyCompatibility.RequiresThingCollisionCheck(
            GameCompatibility.Mbf, actor));
        Assert.IsTrue(MbfTouchyCompatibility.RequiresThingCollisionCheck(
            GameCompatibility.Mbf21, actor));
    }

    [TestMethod]
    public void FloorImpactUsesTouchyOnlyInMbf()
    {
        using var boomContent = GameContent.CreateDummy(WadPath.Doom2);
        var boom = CreateWorld(boomContent, GameCompatibility.Boom);
        var boomActor = SpawnBarrel(boom, Fixed.Zero);
        PrepareTouchyFall(boomActor);
        boom.ThingMovement.ZMovement(boomActor);
        Assert.IsTrue(boomActor.Health > 0);

        using var mbfContent = GameContent.CreateDummy(WadPath.Doom2);
        var mbf = CreateWorld(mbfContent, GameCompatibility.Mbf);
        var mbfActor = SpawnBarrel(mbf, Fixed.Zero);
        PrepareTouchyFall(mbfActor);
        mbf.ThingMovement.ZMovement(mbfActor);
        Assert.IsTrue(mbfActor.Health <= 0);
    }

    [TestMethod]
    public void BouncerFloorImpactUsesMbfDecay()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var actor = SpawnBarrel(world, Fixed.Zero);
        actor.Flags |= MobjFlags.Bounces;
        actor.Flags &= ~MobjFlags.NoGravity;
        actor.Z = actor.FloorZ + Fixed.One;
        actor.MomZ = -Fixed.FromInt(10);

        world.ThingMovement.ZMovement(actor);

        var expected = Fixed.FromInt(10) * Fixed.FromDouble(0.45);
        Assert.AreEqual(expected.Data, actor.MomZ.Data);
        Assert.AreEqual(actor.FloorZ.Data, actor.Z.Data);
    }

    [TestMethod]
    public void BoomDoesNotConsumeBouncesFlag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var actor = SpawnBarrel(world, Fixed.Zero);
        actor.Flags |= MobjFlags.Bounces;
        actor.Z = actor.FloorZ + Fixed.One;
        actor.MomZ = -Fixed.FromInt(10);

        world.ThingMovement.ZMovement(actor);

        Assert.AreEqual(Fixed.Zero.Data, actor.MomZ.Data);
        Assert.AreEqual(actor.FloorZ.Data, actor.Z.Data);
    }

    [TestMethod]
    public void BouncerFreeFallUsesMassScaledMbfGravity()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var actor = SpawnBarrel(world, Fixed.Zero);
        actor.Flags |= MobjFlags.Bounces;
        actor.Flags &= ~MobjFlags.NoGravity;
        actor.Z = actor.FloorZ + Fixed.FromInt(8);
        actor.MomZ = Fixed.One;

        var gravityStep = MbfBounceCompatibility.GetVerticalGravityStep(actor, Fixed.One);
        world.ThingMovement.ZMovement(actor);

        Assert.AreEqual((Fixed.One - gravityStep).Data, actor.MomZ.Data);
    }

    [TestMethod]
    public void WallReflectionDampsOnlyPerpendicularMomentumUnderGravity()
    {
        var actor = new Mobj(null)
        {
            MomX = Fixed.FromInt(2),
            MomY = -Fixed.FromInt(4),
            Flags = MobjFlags.Bounces
        };
        var line = HorizontalLine();

        MbfBounceCompatibility.BounceFromLine(actor, line);

        Assert.AreEqual(Fixed.FromInt(2).Data, actor.MomX.Data);
        Assert.AreEqual(Fixed.FromInt(2).Data, actor.MomY.Data);
    }

    [TestMethod]
    public void NoGravityWallReflectionKeepsFullReflectedMomentum()
    {
        var actor = new Mobj(null)
        {
            MomX = Fixed.FromInt(2),
            MomY = -Fixed.FromInt(4),
            Flags = MobjFlags.Bounces | MobjFlags.NoGravity
        };
        var line = HorizontalLine();

        MbfBounceCompatibility.BounceFromLine(actor, line);

        Assert.AreEqual(Fixed.FromInt(2).Data, actor.MomX.Data);
        Assert.AreEqual(Fixed.FromInt(4).Data, actor.MomY.Data);
    }

    [TestMethod]
    public void RuntimeNonSolidBouncerReversesAgainstSolidActor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var player = world.ConsolePlayer.Mobj;
        var actor = world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Barrel);

        actor.Flags |= MobjFlags.Bounces;
        actor.Flags &= ~(MobjFlags.Solid | MobjFlags.Missile | MobjFlags.NoGravity);
        actor.MomX = Fixed.FromInt(4);
        actor.MomY = Fixed.Zero;

        var allowed = world.ThingMovement.CheckPosition(actor, actor.X, actor.Y);

        Assert.IsFalse(allowed);
        Assert.AreEqual((-Fixed.One).Data, actor.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, actor.MomY.Data);
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility)
    {
        return new World(
            content,
            new GameOptions { Compatibility = compatibility },
            null);
    }

    private static Mobj SpawnBarrel(World world, Fixed xOffset)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(
            player.X + xOffset,
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Barrel);
    }

    private static void PrepareTouchyFall(Mobj actor)
    {
        actor.Flags |= MobjFlags.Touchy | MobjFlags.Shootable;
        actor.MbfTouchyArmed = true;
        actor.Health = 20;
        actor.Z = actor.FloorZ + Fixed.One;
        actor.MomZ = -Fixed.FromInt(2);
    }

    private static LineDef HorizontalLine()
    {
        return new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.FromInt(128), Fixed.Zero),
            (LineFlags)0,
            LineSpecial.Normal,
            0,
            null,
            null);
    }
}
