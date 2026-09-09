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
public sealed class MbfLineAndSoundCodePointersTest
{
    [TestMethod]
    public void CompatibilityBoundaryKeepsPointersMbfOnly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var boomSound = new RecordingSound();
        var boomWorld = NewWorld(content, GameCompatibility.Boom, boomSound);
        var boomActor = SpawnActor(boomWorld);
        boomActor.State = State((int)Sfx.ITEMUP, 0);

        Assert.IsFalse(MbfLineAndSoundCodePointers.PlaySoundFromState(boomWorld, boomActor));
        Assert.AreEqual(0, boomSound.Positional.Count);
        Assert.AreEqual(0, boomSound.Global.Count);

        boomActor.State = State(11, 0);
        Assert.IsFalse(MbfLineAndSoundCodePointers.LineEffectFromState(boomWorld, boomActor));
        Assert.AreEqual(11, boomActor.State.Misc1);

        var mbfSound = new RecordingSound();
        var mbfWorld = NewWorld(content, GameCompatibility.Mbf, mbfSound);
        var mbfActor = SpawnActor(mbfWorld);
        mbfActor.State = State((int)Sfx.ITEMUP, 0);

        Assert.IsTrue(MbfLineAndSoundCodePointers.PlaySoundFromState(mbfWorld, mbfActor));
        Assert.AreEqual(1, mbfSound.Positional.Count);
    }

    [TestMethod]
    public void PlaySoundUsesMisc2ToSelectPositionalOrGlobalSound()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var sound = new RecordingSound();
        var world = NewWorld(content, GameCompatibility.Mbf, sound);
        var actor = SpawnActor(world);

        actor.State = State((int)Sfx.NONE, 0);
        Assert.IsTrue(MbfLineAndSoundCodePointers.PlaySoundFromState(world, actor));
        Assert.AreEqual(1, sound.Positional.Count);
        Assert.AreSame(actor, sound.Positional[0].Source);
        Assert.AreEqual(Sfx.NONE, sound.Positional[0].Sfx);
        Assert.AreEqual(0, sound.Global.Count);

        actor.State = State((int)Sfx.ITEMUP, 1);
        Assert.IsTrue(MbfLineAndSoundCodePointers.PlaySoundFromState(world, actor));
        Assert.AreEqual(1, sound.Positional.Count);
        Assert.AreEqual(1, sound.Global.Count);
        Assert.AreEqual(Sfx.ITEMUP, sound.Global[0]);
    }

    [TestMethod]
    public void LineEffectWritesOneShotSpecialBackToSharedStateAndRestoresPlayer()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewWorld(content, GameCompatibility.Mbf);
        var actor = SpawnActor(world);
        var sharedState = State(11, 0);
        actor.State = sharedState;

        var firstLine = world.Map.Lines[0];
        var firstLineSpecial = firstLine.Special;

        Assert.IsNull(actor.Player);
        Assert.IsTrue(MbfLineAndSoundCodePointers.LineEffectFromState(world, actor));

        // Special 11 is S1. ChangeSwitchTexture clears the synthetic line's
        // special even when no switch texture is present, and MBF writes that
        // value back to the state itself.
        Assert.AreEqual(0, sharedState.Misc1);
        Assert.IsNull(actor.Player);

        // Only the synthetic copy's special is cleared. The real first line is
        // still the template and must keep its own line special.
        Assert.AreEqual(firstLineSpecial, firstLine.Special);

        var secondActor = SpawnActor(world);
        secondActor.State = sharedState;
        Assert.IsTrue(MbfLineAndSoundCodePointers.LineEffectFromState(world, secondActor));
        Assert.AreEqual(0, sharedState.Misc1);
        Assert.IsNull(secondActor.Player);
    }

    [TestMethod]
    public void LineEffectPreservesPrBoomFirstSidedefSwitchTextureQuirk()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewWorld(content, GameCompatibility.Mbf);
        world.Options.MbfOptions.CompZeroTags = true;

        var firstLine = world.Map.Lines[0];
        Assert.IsNotNull(firstLine.FrontSide);
        Assert.IsTrue(world.Map.Textures.SwitchList.Length >= 2);

        var frontSide = firstLine.FrontSide;
        var switches = world.Map.Textures.SwitchList;
        var firstLineSpecial = firstLine.Special;
        frontSide.MiddleTexture = switches[0];

        var actor = SpawnActor(world);
        actor.State = State(138, 0); // SR light-on: keeps its special.

        Assert.IsTrue(MbfLineAndSoundCodePointers.LineEffectFromState(world, actor));

        // A_LineEffect shallow-copies the first linedef in PrBoom. Its copied
        // sidenum[0] still addresses the real first sidedef, so changing the
        // synthetic switch texture changes that sidedef too.
        Assert.AreEqual(switches[1], frontSide.MiddleTexture);
        Assert.AreEqual(firstLineSpecial, firstLine.Special);
        Assert.AreEqual(138, actor.State.Misc1);
    }

    [TestMethod]
    public void LineEffectRestoresExistingPlayerReference()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewWorld(content, GameCompatibility.Mbf);
        var actor = world.ConsolePlayer.Mobj;
        var player = actor.Player;
        actor.State = State(11, 0);

        Assert.IsNotNull(player);
        Assert.IsTrue(MbfLineAndSoundCodePointers.LineEffectFromState(world, actor));
        Assert.AreSame(player, actor.Player);
        Assert.AreEqual(0, actor.State.Misc1);
    }

    [TestMethod]
    public void BexRegistersPlaySoundAndLineEffectAndExecutesBoth()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 189 = PlaySound
FRAME 190 = LineEffect
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            Assert.AreEqual("PlaySound", DoomInfo.States[189].MobjAction.Method.Name);
            Assert.AreEqual("LineEffect", DoomInfo.States[190].MobjAction.Method.Name);

            using var content = GameContent.CreateDummy(WadPath.Doom2);
            var sound = new RecordingSound();
            var world = NewWorld(content, GameCompatibility.Mbf, sound);
            var actor = SpawnActor(world);

            DoomInfo.States[189].Misc1 = (int)Sfx.ITEMUP;
            DoomInfo.States[189].Misc2 = 1;
            Assert.IsTrue(actor.SetState((MobjState)189));
            Assert.AreEqual(1, sound.Global.Count);
            Assert.AreEqual(Sfx.ITEMUP, sound.Global[0]);

            DoomInfo.States[190].Misc1 = 11;
            DoomInfo.States[190].Misc2 = 0;
            Assert.IsTrue(actor.SetState((MobjState)190));
            Assert.AreEqual(0, DoomInfo.States[190].Misc1);
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
        ISound sound = null)
    {
        var options = new GameOptions { Compatibility = compatibility };
        if (sound != null)
            options.Sound = sound;

        return new World(content, options, null);
    }

    private static Mobj SpawnActor(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Possessed);
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
            "manageddoom-mbf-line-sound-" + Guid.NewGuid().ToString("N") + ".bex");
        File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
        return path;
    }

    private sealed class RecordingSound : ISound
    {
        public List<(Mobj Source, Sfx Sfx)> Positional { get; } = new();
        public List<Sfx> Global { get; } = new();

        public int MaxVolume => 15;
        public int Volume { get; set; }

        public void SetListener(Mobj listener) { }
        public void Update() { }
        public void StartSound(Sfx sfx) => Global.Add(sfx);
        public void StartSound(Mobj mobj, Sfx sfx, SfxType type) => Positional.Add((mobj, sfx));
        public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume) => Positional.Add((mobj, sfx));
        public void StopSound(Mobj mobj) { }
        public void Reset()
        {
            Positional.Clear();
            Global.Clear();
        }
        public void Pause() { }
        public void Resume() { }
    }
}
