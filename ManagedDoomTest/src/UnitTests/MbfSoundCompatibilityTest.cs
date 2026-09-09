using System.Collections.Generic;
using System.Linq;
using ManagedDoom;
using ManagedDoom.Audio;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Audio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfSoundCompatibilityTest
{
    [TestMethod]
    public void SelectorMatchesVanillaBoomMbfAndMbf21Boundaries()
    {
        Assert.IsTrue(MbfSoundCompatibility.UsesDoomSoundQuirks(
            GameCompatibility.Vanilla, compSound: false));
        Assert.IsTrue(MbfSoundCompatibility.UsesDoomSoundQuirks(
            GameCompatibility.Vanilla, compSound: true));

        Assert.IsFalse(MbfSoundCompatibility.UsesDoomSoundQuirks(
            GameCompatibility.Boom, compSound: false));
        Assert.IsFalse(MbfSoundCompatibility.UsesDoomSoundQuirks(
            GameCompatibility.Boom, compSound: true));

        Assert.IsFalse(MbfSoundCompatibility.UsesDoomSoundQuirks(
            GameCompatibility.Mbf, compSound: false));
        Assert.IsTrue(MbfSoundCompatibility.UsesDoomSoundQuirks(
            GameCompatibility.Mbf, compSound: true));

        Assert.IsFalse(MbfSoundCompatibility.UsesDoomSoundQuirks(
            GameCompatibility.Mbf21, compSound: false));
        Assert.IsFalse(MbfSoundCompatibility.UsesDoomSoundQuirks(
            GameCompatibility.Mbf21, compSound: true));
    }

    [TestMethod]
    public void CorrectedSemanticsCoverUseLandingAndPickupSoundFixes()
    {
        Assert.IsTrue(MbfSoundCompatibility.UsesTwoSidedUseNoWayFix(
            GameCompatibility.Mbf, compSound: false));
        Assert.IsFalse(MbfSoundCompatibility.UsesTwoSidedUseNoWayFix(
            GameCompatibility.Mbf, compSound: true));

        Assert.IsFalse(MbfSoundCompatibility.ShouldPlayHardLandingSound(
            GameCompatibility.Mbf, compSound: false, health: 0));
        Assert.IsTrue(MbfSoundCompatibility.ShouldPlayHardLandingSound(
            GameCompatibility.Mbf, compSound: false, health: 1));
        Assert.IsTrue(MbfSoundCompatibility.ShouldPlayHardLandingSound(
            GameCompatibility.Mbf, compSound: true, health: 0));

        Assert.IsTrue(MbfSoundCompatibility.ShouldPlayPickupSound(
            GameCompatibility.Mbf, compSound: false, isDisplayPlayer: false));
        Assert.IsFalse(MbfSoundCompatibility.ShouldPlayPickupSound(
            GameCompatibility.Mbf, compSound: true, isDisplayPlayer: false));
        Assert.IsTrue(MbfSoundCompatibility.ShouldPlayPickupSound(
            GameCompatibility.Mbf, compSound: true, isDisplayPlayer: true));

        Assert.IsFalse(MbfSoundCompatibility.ShouldReplaceSameOriginChannel(
            GameCompatibility.Mbf,
            compSound: false,
            existingIsPickup: true,
            newIsPickup: false));
        Assert.IsTrue(MbfSoundCompatibility.ShouldReplaceSameOriginChannel(
            GameCompatibility.Mbf,
            compSound: true,
            existingIsPickup: true,
            newIsPickup: false));
        Assert.IsTrue(MbfSoundCompatibility.ShouldReplaceSameOriginChannel(
            GameCompatibility.Mbf,
            compSound: false,
            existingIsPickup: true,
            newIsPickup: true));
        Assert.IsFalse(MbfSoundCompatibility.ShouldReplaceSameOriginChannel(
            GameCompatibility.Mbf21,
            compSound: true,
            existingIsPickup: true,
            newIsPickup: false));
    }

    [TestMethod]
    public void HardLandingRuntimeConsumesCompSound()
    {
        Assert.AreEqual(0, CountDeadHardLandingSounds(
            GameCompatibility.Boom, compSound: true));
        Assert.AreEqual(0, CountDeadHardLandingSounds(
            GameCompatibility.Mbf, compSound: false));
        Assert.AreEqual(1, CountDeadHardLandingSounds(
            GameCompatibility.Mbf, compSound: true));
        Assert.AreEqual(0, CountDeadHardLandingSounds(
            GameCompatibility.Mbf21, compSound: true));
    }

    private static int CountDeadHardLandingSounds(
        GameCompatibility compatibility,
        bool compSound)
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var sound = new RecordingSound();
        var options = new GameOptions { Compatibility = compatibility };
        options.MbfOptions.CompSound = compSound;
        options.Sound = sound;

        var world = new World(content, options, null);
        sound.Started.Clear();

        var actor = world.ConsolePlayer.Mobj;
        actor.Health = 0;
        actor.FloorZ = Fixed.Zero;
        actor.CeilingZ = Fixed.FromInt(128);
        actor.Z = Fixed.FromInt(16);
        actor.MomZ = Fixed.FromInt(-16);

        world.ThingMovement.ZMovement(actor);

        return sound.Started.Count(sfx => sfx == Sfx.OOF);
    }

    private sealed class RecordingSound : ISound
    {
        public List<Sfx> Started { get; } = new();

        public int MaxVolume => 15;

        public int Volume { get; set; }

        public void SetListener(Mobj listener) { }

        public void Update() { }

        public void StartSound(Sfx sfx) => Started.Add(sfx);

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type) => Started.Add(sfx);

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume) => Started.Add(sfx);

        public void StopSound(Mobj mobj) { }

        public void Reset() => Started.Clear();

        public void Pause() { }

        public void Resume() { }
    }
}
