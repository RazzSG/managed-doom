using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Collision;
using ManagedDoom.Compatibility.Boom.Doors;
using ManagedDoom.Compatibility.Boom.Gameplay;
using ManagedDoom.Compatibility.Boom.Lines;
using ManagedDoom.Compatibility.Boom.Movement;
using ManagedDoom.Compatibility.Boom.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

/// <summary>
/// Representative compatibility contracts for the Phase 12 Boom polish work.
/// Individual feature tests cover the detailed behavior; these tests make sure
/// the core Boom boundary remains coherent and is inherited by MBF/MBF21.
/// </summary>
[TestClass]
public sealed class BoomRepresentativeCompatibilityContractTest
{
    private static readonly GameCompatibility[] CompatibilityLevels =
    {
        GameCompatibility.Vanilla,
        GameCompatibility.Boom,
        GameCompatibility.Mbf,
        GameCompatibility.Mbf21
    };

    [TestMethod]
    public void Phase61Through66CoreContractMatchesCompatibilityMatrix()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var monster = new Mobj(world);
        var zeroTagLine = CreateLine(tag: 0, special: 5);
        var halfMaxMove = Fixed.FromInt(15);
        var largeNegativeMove = Fixed.FromInt(-16);

        foreach (var compatibility in CompatibilityLevels)
        {
            var boom = GameCompatibilityFeatures.SupportsBoom(compatibility);

            // Phase 4.61 — movement quirks.
            Assert.AreEqual(
                boom,
                BoomMovementQuirks.ShouldSplitNegativeDisplacement(
                    largeNegativeMove, Fixed.Zero, halfMaxMove, compatibility),
                $"Phase 4.61 movement contract / {compatibility}");

            // Phase 4.62 — collision quirks.
            Assert.AreEqual(
                !boom,
                BoomCollisionQuirks.BlocksGenericThing(
                    (MobjFlags)0, MobjFlags.Solid, compatibility),
                $"Phase 4.62 collision contract / {compatibility}");

            // Phase 4.63 — sector movement behavior.
            Assert.AreEqual(
                !boom,
                BoomSectorMovementQuirks.ShouldRestoreIntermediateLoweringFloorAfterNoFit(compatibility),
                $"Phase 4.63 sector-movement contract / {compatibility}");

            // Phase 4.64 — boss-spawn teleport semantics.
            Assert.AreEqual(
                boom,
                BoomTeleportQuirks.CanTelefragAtDestination(
                    monster, bossTeleport: true, mapNumber: 1, compatibility),
                $"Phase 4.64 teleport contract / {compatibility}");

            // Phase 4.65 — P_CheckTag zero-tag behavior.
            Assert.AreEqual(
                !boom,
                BoomTagRules.CanActivate(zeroTagLine, compatibility),
                $"Phase 4.65 zero-tag contract / {compatibility}");

            // Phase 4.66 — representative compatibility bug fixes.
            Assert.AreEqual(
                boom,
                BoomGameplayBugFixes.UsesFixedArchVileResurrection(compatibility),
                $"Phase 4.66 Arch-vile contract / {compatibility}");
            Assert.AreEqual(
                !boom,
                BoomGameplayBugFixes.EnforcesPainElementalLostSoulLimit(compatibility),
                $"Phase 4.66 Pain Elemental contract / {compatibility}");
            Assert.AreEqual(
                boom,
                BoomGameplayBugFixes.ShouldIgnorePlayerDamage(
                    compatibility, damage: 1000, godMode: true, invulnerable: false),
                $"Phase 4.66 god-mode contract / {compatibility}");
            Assert.AreEqual(
                boom,
                BoomDoorCompatibility.FixesBlazingDoorSounds(compatibility),
                $"Phase 4.66 door contract / {compatibility}");
            Assert.AreEqual(
                boom,
                BoomSectorModelCompatibility.UsesFixedModelSemantics(compatibility),
                $"Phase 4.66 model-sector contract / {compatibility}");
            Assert.AreEqual(
                boom,
                BoomClassicMoverCompatibility.UsesFixedMultiTaggedStairScan(compatibility),
                $"Phase 4.66 stair contract / {compatibility}");
            Assert.AreEqual(
                boom,
                BoomClassicMoverCompatibility.RemovesBouncedPureRaisePlatform(compatibility),
                $"Phase 4.66 floor/platform contract / {compatibility}");
            Assert.AreEqual(
                boom,
                BoomLostSoulCompatibility.UsesCorrectBounce(compatibility, GameVersion.Version109),
                $"Phase 4.66 Lost Soul contract / {compatibility}");
        }
    }

    [TestMethod]
    public void MbfOnlyDoorLightingUpgradeDoesNotLeakIntoBoom()
    {
        Assert.IsFalse(BoomDoorCompatibility.UsesGradualDoorLighting(GameCompatibility.Vanilla));
        Assert.IsFalse(BoomDoorCompatibility.UsesGradualDoorLighting(GameCompatibility.Boom));
        Assert.IsTrue(BoomDoorCompatibility.UsesGradualDoorLighting(GameCompatibility.Mbf));
        Assert.IsTrue(BoomDoorCompatibility.UsesGradualDoorLighting(GameCompatibility.Mbf21));

        // The underlying tagged-door lighting is still a Boom feature and must
        // remain inherited by both later compatibility levels.
        Assert.IsFalse(BoomDoorCompatibility.UsesTaggedManualDoorLighting(GameCompatibility.Vanilla));
        Assert.IsTrue(BoomDoorCompatibility.UsesTaggedManualDoorLighting(GameCompatibility.Boom));
        Assert.IsTrue(BoomDoorCompatibility.UsesTaggedManualDoorLighting(GameCompatibility.Mbf));
        Assert.IsTrue(BoomDoorCompatibility.UsesTaggedManualDoorLighting(GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void VanillaExecutableVersionExceptionDoesNotEnableUnrelatedBoomFixes()
    {
        // Ultimate/Final Doom already contain the corrected Lost Soul bounce,
        // but selecting that executable must not silently opt Vanilla into the
        // unrelated Boom compatibility fixes from Phase 12.
        Assert.IsTrue(BoomLostSoulCompatibility.UsesCorrectBounce(
            GameCompatibility.Vanilla, GameVersion.Ultimate));

        Assert.IsFalse(BoomSectorModelCompatibility.UsesFixedModelSemantics(
            GameCompatibility.Vanilla));
        Assert.IsFalse(BoomDoorCompatibility.FixesBlazingDoorSounds(
            GameCompatibility.Vanilla));
        Assert.IsFalse(BoomGameplayBugFixes.ShouldIgnorePlayerDamage(
            GameCompatibility.Vanilla, damage: 1000, godMode: true, invulnerable: false));
        Assert.IsTrue(BoomSectorMovementQuirks.ShouldRestoreIntermediateLoweringFloorAfterNoFit(
            GameCompatibility.Vanilla));
    }

    [TestMethod]
    public void RepresentativeRuntimeHooksFollowTheSameCompatibilityBoundary()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        foreach (var compatibility in CompatibilityLevels)
        {
            var boom = GameCompatibilityFeatures.SupportsBoom(compatibility);
            var world = new World(content, new GameOptions
            {
                Compatibility = compatibility,
                GameVersion = GameVersion.Version109
            }, null);

            // Collision call-site: a non-solid mover only passes an ordinary
            // solid thing once Boom compatibility is active.
            var mover = world.ConsolePlayer.Mobj;
            mover.Flags &= ~MobjFlags.Solid;
            var obstacle = world.ThingAllocation.SpawnMobj(
                mover.X, mover.Y, Mobj.OnFloorZ, MobjType.Barrel);

            Assert.AreEqual(
                boom,
                world.ThingMovement.CheckPosition(mover, obstacle.X, obstacle.Y),
                $"Runtime collision hook / {compatibility}");

            // ZMovement call-site: Doom II 1.9 keeps the old zeroed impulse in
            // Vanilla. Boom fixes the bounce, while MBF/MBF21 default comp_soul=1
            // deliberately restores the original buggy ordering.
            var skull = new Mobj(world)
            {
                FloorZ = Fixed.Zero,
                CeilingZ = Fixed.FromInt(128),
                Height = Fixed.FromInt(56),
                Flags = MobjFlags.SkullFly | MobjFlags.NoGravity,
                Z = Fixed.FromInt(1),
                MomZ = Fixed.FromInt(-2)
            };

            world.ThingMovement.ZMovement(skull);

            var correctedLostSoulBounce = compatibility == GameCompatibility.Boom;

            Assert.AreEqual(
                correctedLostSoulBounce ? Fixed.FromInt(2).Data : Fixed.Zero.Data,
                skull.MomZ.Data,
                $"Runtime Lost Soul hook / {compatibility}");
        }
    }

    private static LineDef CreateLine(short tag, int special)
    {
        return new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.One, Fixed.Zero),
            (LineFlags)0,
            (LineSpecial)special,
            tag,
            null,
            null);
    }
}
