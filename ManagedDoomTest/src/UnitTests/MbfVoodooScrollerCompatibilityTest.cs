using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfVoodooScrollerCompatibilityTest
{
    private static readonly Fixed SlowMomentum = new Fixed(0x0800);

    [TestMethod]
    public void ProfileDefaultsAndExplicitOverridesMatchPrBoom()
    {
        Assert.IsFalse(MbfVoodooScrollerCompatibility.IsLegacyBugEnabled(
            GameCompatibility.Boom, true, false));

        Assert.IsTrue(MbfVoodooScrollerCompatibility.IsLegacyBugEnabled(
            GameCompatibility.Mbf, true, false));
        Assert.IsFalse(MbfVoodooScrollerCompatibility.IsLegacyBugEnabled(
            GameCompatibility.Mbf, false, true));

        Assert.IsFalse(MbfVoodooScrollerCompatibility.IsLegacyBugEnabled(
            GameCompatibility.Mbf21, true, false));
        Assert.IsTrue(MbfVoodooScrollerCompatibility.IsLegacyBugEnabled(
            GameCompatibility.Mbf21, true, true));
        Assert.IsFalse(MbfVoodooScrollerCompatibility.IsLegacyBugEnabled(
            GameCompatibility.Mbf21, false, true));
    }

    [TestMethod]
    public void MbfDefaultKeepsLegacySlowVoodooScrollerBug()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf);
        var doll = SpawnVoodooDoll(world);

        PrepareSlowScrollingMove(world, doll);
        world.ThingMovement.XYMovement(doll);

        Assert.AreEqual(Fixed.Zero.Data, doll.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, doll.MomY.Data);
    }

    [TestMethod]
    public void MbfExplicitFixPreservesSlowScrollingMomentum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.MbfOptions.CompVoodooScroller = false;
        var world = new World(content, options, null);
        var doll = SpawnVoodooDoll(world);

        PrepareSlowScrollingMove(world, doll);
        world.ThingMovement.XYMovement(doll);

        Assert.AreNotEqual(Fixed.Zero.Data, doll.MomX.Data);
    }

    [TestMethod]
    public void Mbf21DefaultsToFixedVoodooScrollerBehavior()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Mbf21);
        var doll = SpawnVoodooDoll(world);

        PrepareSlowScrollingMove(world, doll);
        world.ThingMovement.XYMovement(doll);

        Assert.AreNotEqual(Fixed.Zero.Data, doll.MomX.Data);
    }

    [TestMethod]
    public void Mbf21ExplicitLegacyModeRestoresSlowScrollerBug()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf21 };
        options.MbfOptions.CompVoodooScroller = true;
        var world = new World(content, options, null);
        var doll = SpawnVoodooDoll(world);

        PrepareSlowScrollingMove(world, doll);
        world.ThingMovement.XYMovement(doll);

        Assert.AreEqual(Fixed.Zero.Data, doll.MomX.Data);
    }

    [TestMethod]
    public void FixedModeStillStopsNonScrollingVoodooDoll()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf21 };
        options.MbfOptions.CompVoodooScroller = false;
        var world = new World(content, options, null);
        var doll = SpawnVoodooDoll(world);

        world.ConsolePlayer.Cmd.ForwardMove = 1;
        world.ConsolePlayer.Cmd.SideMove = 0;
        doll.MbfScrollingMovement = false;
        doll.MomX = SlowMomentum;
        doll.MomY = Fixed.Zero;

        world.ThingMovement.XYMovement(doll);

        Assert.AreEqual(Fixed.Zero.Data, doll.MomX.Data);
    }

    [TestMethod]
    public void ParserAndClonePreserveExplicitFalseOverride()
    {
        var parsed = MbfOptionsReader.Parse("comp_voodooscroller 0");

        Assert.IsFalse(parsed.CompVoodooScroller);
        Assert.IsTrue(parsed.HasCompVoodooScrollerOverride);

        var clone = parsed.Clone();
        Assert.IsFalse(clone.CompVoodooScroller);
        Assert.IsTrue(clone.HasCompVoodooScrollerOverride);
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility)
    {
        return new World(
            content,
            new GameOptions { Compatibility = compatibility },
            null);
    }

    private static Mobj SpawnVoodooDoll(World world)
    {
        var realPlayer = world.ConsolePlayer.Mobj;
        var doll = world.ThingAllocation.SpawnMobj(
            realPlayer.X,
            realPlayer.Y,
            realPlayer.Z,
            MobjType.Player);

        // Keep world.ConsolePlayer.Mobj pointing at the real player. Sharing the
        // Player reference while player.Mobj != doll is the engine's voodoo-
        // doll identity test, matching player->mo != mo in PrBoom.
        doll.Player = world.ConsolePlayer;
        doll.Health = world.ConsolePlayer.Health;
        doll.Flags |= MobjFlags.NoClip;

        Assert.AreNotSame(world.ConsolePlayer.Mobj, doll);
        return doll;
    }

    private static void PrepareSlowScrollingMove(World world, Mobj doll)
    {
        // The historical LXDOOM branch matters when the real player has input:
        // it independently treats a voodoo doll as stoppable. The fixed path
        // exempts a doll whose momentum came from a scroller.
        world.ConsolePlayer.Cmd.ForwardMove = 1;
        world.ConsolePlayer.Cmd.SideMove = 0;
        doll.MbfScrollingMovement = true;
        doll.MomX = SlowMomentum;
        doll.MomY = Fixed.Zero;
    }
}
