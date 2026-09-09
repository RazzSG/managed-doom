using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfStayOnLiftTest
{
    [TestMethod]
    public void DefaultMbfCandidateTracksLivingTargetOnSameLiftTag()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var actor = SpawnActor(world);
        var target = world.ConsolePlayer.Mobj;
        actor.Target = target;

        var sector = actor.Subsector.Sector;
        var targetSector = target.Subsector.Sector;
        var oldFloorData = sector.FloorData;
        var oldActorTag = sector.Tag;
        var oldTargetTag = targetSector.Tag;

        try
        {
            sector.Tag = 417;
            targetSector.Tag = 417;
            sector.FloorData = new Platform(world) { Sector = sector };

            Assert.IsTrue(MbfStayOnLift.ShouldTrackBeforeMove(
                world,
                compStayLift: false,
                actor: actor));
        }
        finally
        {
            sector.FloorData = oldFloorData;
            sector.Tag = oldActorTag;
            targetSector.Tag = oldTargetTag;
        }
    }

    [TestMethod]
    public void CompatibilityFlagDisablesSmartStayOnLiftBehavior()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var actor = SpawnActor(world);
        actor.Target = world.ConsolePlayer.Mobj;
        var sector = actor.Subsector.Sector;
        var oldFloorData = sector.FloorData;

        try
        {
            sector.FloorData = new Platform(world) { Sector = sector };

            Assert.IsFalse(MbfStayOnLift.ShouldTrackBeforeMove(
                world,
                compStayLift: true,
                actor: actor));
        }
        finally
        {
            sector.FloorData = oldFloorData;
        }
    }

    [TestMethod]
    public void BoomDoesNotUseMbfStayOnLiftBehavior()
    {
        Assert.IsFalse(MbfStayOnLift.ShouldAbandonDirectionAfterMove(
            GameCompatibility.Boom,
            compStayLift: false,
            trackedBeforeMove: true,
            randomByte: 0,
            isOnLiftAfterMove: false));
    }

    [TestMethod]
    public void CandidateRequiresLiveTargetMatchingTagAndLift()
    {
        Assert.IsFalse(MbfStayOnLift.ShouldTrackBeforeMove(
            GameCompatibility.Mbf,
            compStayLift: false,
            targetAlive: false,
            sameSectorTag: true,
            actorOnLift: true));

        Assert.IsFalse(MbfStayOnLift.ShouldTrackBeforeMove(
            GameCompatibility.Mbf,
            compStayLift: false,
            targetAlive: true,
            sameSectorTag: false,
            actorOnLift: true));

        Assert.IsFalse(MbfStayOnLift.ShouldTrackBeforeMove(
            GameCompatibility.Mbf,
            compStayLift: false,
            targetAlive: true,
            sameSectorTag: true,
            actorOnLift: false));

        Assert.IsTrue(MbfStayOnLift.ShouldTrackBeforeMove(
            GameCompatibility.Mbf,
            compStayLift: false,
            targetAlive: true,
            sameSectorTag: true,
            actorOnLift: true));
    }

    [TestMethod]
    public void RandomBoundaryMatchesOriginalMbfThreshold()
    {
        Assert.AreEqual(230, MbfStayOnLift.StayRandomThreshold);

        Assert.IsTrue(MbfStayOnLift.ShouldAbandonDirectionAfterMove(
            GameCompatibility.Mbf,
            compStayLift: false,
            trackedBeforeMove: true,
            randomByte: 229,
            isOnLiftAfterMove: false));

        Assert.IsFalse(MbfStayOnLift.ShouldAbandonDirectionAfterMove(
            GameCompatibility.Mbf,
            compStayLift: false,
            trackedBeforeMove: true,
            randomByte: 230,
            isOnLiftAfterMove: false));
    }

    [TestMethod]
    public void RemainingOnLiftNeverAbandonsDirection()
    {
        Assert.IsFalse(MbfStayOnLift.ShouldAbandonDirectionAfterMove(
            GameCompatibility.Mbf,
            compStayLift: false,
            trackedBeforeMove: true,
            randomByte: 0,
            isOnLiftAfterMove: true));
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility)
    {
        var options = new GameOptions { Compatibility = compatibility };
        options.Players[0].Reborn();
        return new World(content, options, null);
    }

    private static Mobj SpawnActor(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        var actor = world.ThingAllocation.SpawnMobj(
            player.X + Fixed.FromInt(64),
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);
        actor.MoveDir = Direction.East;
        return actor;
    }
}
