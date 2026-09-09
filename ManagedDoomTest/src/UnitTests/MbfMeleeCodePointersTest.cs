using System;
using System.Collections.Generic;
using System.IO;
using ManagedDoom;
using ManagedDoom.Audio;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Things;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class MbfMeleeCodePointersTest
{
    [TestMethod]
    public void CompatibilityBoundaryKeepsPointersMbfOnly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var boomSound = new RecordingSound();
        var boomWorld = NewWorld(content, GameCompatibility.Boom, boomSound);
        var boomActor = SpawnActor(boomWorld, MobjType.Possessed, Fixed.Zero);
        var boomTarget = SpawnActor(boomWorld, MobjType.Troop, Fixed.FromInt(32));
        boomActor.Target = boomTarget;
        boomActor.State = State(7, (int)Sfx.CLAW);
        var boomHealth = boomTarget.Health;

        Assert.IsFalse(MbfMeleeCodePointers.ScratchFromState(boomWorld, boomActor));
        Assert.AreEqual(boomHealth, boomTarget.Health);
        Assert.AreEqual(0, boomSound.Positional.Count);

        var mbfSound = new RecordingSound();
        var mbfWorld = NewWorld(content, GameCompatibility.Mbf, mbfSound);
        var mbfActor = SpawnActor(mbfWorld, MobjType.Possessed, Fixed.Zero);
        var mbfTarget = SpawnActor(mbfWorld, MobjType.Troop, Fixed.FromInt(32));
        mbfActor.Target = mbfTarget;
        mbfActor.State = State(7, (int)Sfx.CLAW);
        var mbfHealth = mbfTarget.Health;

        Assert.IsTrue(MbfMeleeCodePointers.ScratchFromState(mbfWorld, mbfActor));
        Assert.AreEqual(mbfHealth - 7, mbfTarget.Health);
        Assert.AreEqual(1, mbfSound.Positional.Count);
    }

    [TestMethod]
    public void ScratchFacesTargetUsesMiscDamageAndOptionalSound()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var sound = new RecordingSound();
        var world = NewWorld(content, GameCompatibility.Mbf, sound);
        var actor = SpawnActor(world, MobjType.Possessed, Fixed.Zero);
        var target = SpawnActor(world, MobjType.Troop, Fixed.FromInt(32));

        actor.Target = target;
        actor.Angle = Angle.Ang180;
        actor.State = State(11, (int)Sfx.CLAW);
        var before = target.Health;

        Assert.IsTrue(MbfMeleeCodePointers.ScratchFromState(world, actor));

        Assert.AreEqual(before - 11, target.Health);
        Assert.AreEqual(Angle.Ang0.Data, actor.Angle.Data);
        Assert.AreEqual(1, sound.Positional.Count);
        Assert.AreSame(actor, sound.Positional[0].Source);
        Assert.AreEqual(Sfx.CLAW, sound.Positional[0].Sfx);

        sound.Reset();
        target.Health = before;
        actor.State = State(5, 0);

        Assert.IsTrue(MbfMeleeCodePointers.ScratchFromState(world, actor));
        Assert.AreEqual(before - 5, target.Health);
        Assert.AreEqual(0, sound.Positional.Count);
    }

    [TestMethod]
    public void ScratchDoesNothingOutsideMeleeRange()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var sound = new RecordingSound();
        var world = NewWorld(content, GameCompatibility.Mbf, sound);
        var actor = SpawnActor(world, MobjType.Possessed, Fixed.Zero);
        var target = SpawnActor(world, MobjType.Troop, Fixed.FromInt(256));

        actor.Target = target;
        actor.State = State(20, (int)Sfx.CLAW);
        var before = target.Health;

        Assert.IsTrue(MbfMeleeCodePointers.ScratchFromState(world, actor));

        Assert.AreEqual(before, target.Health);
        Assert.AreEqual(0, sound.Positional.Count);
    }

    [TestMethod]
    public void BetaSkullAttackUsesInfoDamageAndDoomRandom()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var sound = new RecordingSound();
        var world = NewWorld(content, GameCompatibility.Mbf21, sound);
        var actor = SpawnActor(world, MobjType.Skull, Fixed.Zero);
        var target = SpawnActor(world, MobjType.Troop, Fixed.FromInt(32));

        actor.Target = target;
        actor.Angle = Angle.Ang180;
        world.Random.Clear();

        var before = target.Health;
        var expectedDamage = (8 % 8 + 1) * actor.Info.Damage;

        Assert.IsTrue(actor.Info.Damage > 0);
        Assert.IsTrue(MbfMeleeCodePointers.BetaSkullAttack(world, actor));

        Assert.AreEqual(before - expectedDamage, target.Health);
        Assert.AreEqual(Angle.Ang0.Data, actor.Angle.Data);
        Assert.AreEqual(1, sound.Positional.Count);
        Assert.AreEqual(actor.Info.AttackSound, sound.Positional[0].Sfx);
    }

    [TestMethod]
    public void BetaSkullAttackIgnoresLostSoulTarget()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var sound = new RecordingSound();
        var world = NewWorld(content, GameCompatibility.Mbf, sound);
        var actor = SpawnActor(world, MobjType.Skull, Fixed.Zero);
        var target = SpawnActor(world, MobjType.Skull, Fixed.FromInt(32));

        actor.Target = target;
        var before = target.Health;

        Assert.IsTrue(MbfMeleeCodePointers.BetaSkullAttack(world, actor));

        Assert.AreEqual(before, target.Health);
        Assert.AreEqual(0, sound.Positional.Count);
    }

    [TestMethod]
    public void BexRegistersScratchAndBetaSkullAttackAndExecutesScratch()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 191 = Scratch
FRAME 192 = BetaSkullAttack
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            Assert.AreEqual("Scratch", DoomInfo.States[191].MobjAction.Method.Name);
            Assert.AreEqual("BetaSkullAttack", DoomInfo.States[192].MobjAction.Method.Name);

            using var content = GameContent.CreateDummy(WadPath.Doom2);
            var sound = new RecordingSound();
            var world = NewWorld(content, GameCompatibility.Mbf, sound);
            var actor = SpawnActor(world, MobjType.Possessed, Fixed.Zero);
            var target = SpawnActor(world, MobjType.Troop, Fixed.FromInt(32));
            actor.Target = target;

            DoomInfo.States[191].Misc1 = 9;
            DoomInfo.States[191].Misc2 = (int)Sfx.CLAW;
            var before = target.Health;

            Assert.IsTrue(actor.SetState((MobjState)191));
            Assert.AreEqual(before - 9, target.Health);
            Assert.AreEqual(1, sound.Positional.Count);
        }
        finally
        {
            Reset(wad);
            File.Delete(patch);
        }
    }

    private static World NewWorld(
        GameContent content,
        GameCompatibility compatibility,
        ISound sound)
    {
        return new World(
            content,
            new GameOptions
            {
                Compatibility = compatibility,
                Sound = sound
            },
            null);
    }

    private static Mobj SpawnActor(World world, MobjType type, Fixed xOffset)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(
            player.X + xOffset,
            player.Y,
            Mobj.OnFloorZ,
            type);
    }

    private static MobjStateDef State(int misc1, int misc2)
    {
        return new MobjStateDef(
            -1,
            Sprite.POSS,
            0,
            1,
            null,
            null,
            MobjState.Null,
            misc1,
            misc2);
    }

    private static CommandLineArgs ArgsForPatch(string patchPath)
    {
        return new CommandLineArgs(new[] { "-deh", patchPath, "-nodeh" });
    }

    private static void Reset(Wad wad)
    {
        DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
    }

    private static string CreatePatch(string text)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "manageddoom-mbf-melee-" + Guid.NewGuid().ToString("N") + ".bex");
        File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
        return path;
    }

    private sealed class RecordingSound : ISound
    {
        public List<(Mobj Source, Sfx Sfx)> Positional { get; } = new();

        public int MaxVolume => 15;
        public int Volume { get; set; }

        public void SetListener(Mobj listener) { }
        public void Update() { }
        public void StartSound(Sfx sfx) { }
        public void StartSound(Mobj mobj, Sfx sfx, SfxType type) => Positional.Add((mobj, sfx));
        public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume) => Positional.Add((mobj, sfx));
        public void StopSound(Mobj mobj) { }
        public void Reset() => Positional.Clear();
        public void Pause() { }
        public void Resume() { }
    }
}
