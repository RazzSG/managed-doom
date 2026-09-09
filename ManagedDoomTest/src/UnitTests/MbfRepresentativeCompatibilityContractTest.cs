using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.AI;
using ManagedDoom.Compatibility.Mbf.Gameplay;
using ManagedDoom.Compatibility.Mbf.Movement;
using ManagedDoom.Compatibility.Mbf.Things;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

/// <summary>
/// Representative Phase 19 compatibility contracts. Detailed MBF feature tests
/// cover individual algorithms; this suite protects the Vanilla/Boom boundary
/// and verifies that MBF21 continues to inherit the MBF layer.
/// </summary>
[TestClass]
public sealed class MbfRepresentativeCompatibilityContractTest
{
    private static readonly GameCompatibility[] PreMbfLevels =
    {
        GameCompatibility.Vanilla,
        GameCompatibility.Boom
    };

    private static readonly GameCompatibility[] MbfLevels =
    {
        GameCompatibility.Mbf,
        GameCompatibility.Mbf21
    };

    [TestMethod]
    public void MbfActorBitsAreMetadataOnlyBeforeMbf()
    {
        var actor = new Mobj(null)
        {
            Flags = MobjFlags.Touchy | MobjFlags.Bounces | MobjFlags.Friend
        };

        foreach (var compatibility in PreMbfLevels)
        {
            Assert.IsFalse(
                MbfTouchyCompatibility.RequiresThingCollisionCheck(compatibility, actor),
                $"TOUCHY / {compatibility}");
            Assert.IsFalse(
                MbfBounceCompatibility.IsBouncer(compatibility, actor),
                $"BOUNCES / {compatibility}");
            Assert.IsFalse(
                MbfFriendTargeting.IsFriendly(compatibility, actor),
                $"FRIEND / {compatibility}");
        }

        foreach (var compatibility in MbfLevels)
        {
            Assert.IsTrue(
                MbfTouchyCompatibility.RequiresThingCollisionCheck(compatibility, actor),
                $"TOUCHY / {compatibility}");
            Assert.IsTrue(
                MbfBounceCompatibility.IsBouncer(compatibility, actor),
                $"BOUNCES / {compatibility}");
            Assert.IsTrue(
                MbfFriendTargeting.IsFriendly(compatibility, actor),
                $"FRIEND / {compatibility}");
        }
    }

    [TestMethod]
    public void MbfCodePointerRuntimeDoesNotLeakIntoVanillaOrBoom()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        foreach (var compatibility in PreMbfLevels)
        {
            var world = CreateWorld(content, compatibility);
            var actor = new Mobj(world)
            {
                MomX = Fixed.FromInt(3),
                MomY = Fixed.FromInt(-2),
                MomZ = Fixed.One
            };

            Assert.IsFalse(
                MbfActorUtilityCodePointers.Stop(world, actor),
                compatibility.ToString());
            Assert.AreEqual(Fixed.FromInt(3).Data, actor.MomX.Data, compatibility.ToString());
            Assert.AreEqual(Fixed.FromInt(-2).Data, actor.MomY.Data, compatibility.ToString());
            Assert.AreEqual(Fixed.One.Data, actor.MomZ.Data, compatibility.ToString());
        }

        foreach (var compatibility in MbfLevels)
        {
            var world = CreateWorld(content, compatibility);
            var actor = new Mobj(world)
            {
                MomX = Fixed.FromInt(3),
                MomY = Fixed.FromInt(-2),
                MomZ = Fixed.One
            };

            Assert.IsTrue(
                MbfActorUtilityCodePointers.Stop(world, actor),
                compatibility.ToString());
            Assert.AreEqual(Fixed.Zero.Data, actor.MomX.Data, compatibility.ToString());
            Assert.AreEqual(Fixed.Zero.Data, actor.MomY.Data, compatibility.ToString());
            Assert.AreEqual(Fixed.Zero.Data, actor.MomZ.Data, compatibility.ToString());
        }
    }

    [TestMethod]
    public void MbfOptionsCannotUpgradeBoomBehaviorByThemselves()
    {
        var options = new ManagedDoom.Compatibility.Mbf.MbfOptions();

        // A clean MBF profile defaults comp_dropoff to false. Even if a WAD or
        // caller explicitly turns it on, a forced Boom profile must still keep
        // the MBF selector inactive.
        Assert.IsFalse(options.CompDropoff);
        Assert.IsFalse(options.CompInfCheat);

        options.CompDropoff = true;
        Assert.IsFalse(MbfDropoffCompatibility.UsesClassicBlocking(
            GameCompatibility.Boom,
            options.CompDropoff));

        const int normalPowerDuration = 30 * 35;
        Assert.AreEqual(
            normalPowerDuration,
            MbfPowerupCheatCompatibility.ResolveActivatedPowerValue(
                GameCompatibility.Boom,
                options.CompInfCheat,
                PowerType.Invulnerability,
                normalPowerDuration));

        Assert.AreEqual(
            MbfPowerupCheatCompatibility.InfiniteDuration,
            MbfPowerupCheatCompatibility.ResolveActivatedPowerValue(
                GameCompatibility.Mbf,
                options.CompInfCheat,
                PowerType.Invulnerability,
                normalPowerDuration));
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility)
    {
        return new World(
            content,
            new GameOptions { Compatibility = compatibility },
            null);
    }
}
