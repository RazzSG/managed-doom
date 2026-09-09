using System;
using System.IO;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Things;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class MbfStateControlCodePointersTest
{
    [TestMethod]
    public void CompatibilityBoundaryKeepsStateControlPointersMbfOnly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var boomWorld = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Boom },
            null);
        var boomActor = SpawnActor(boomWorld);
        boomActor.Angle = Angle.Ang45;
        boomActor.State = State(90, 0);

        Assert.IsFalse(MbfStateControlCodePointers.TurnFromState(boomWorld, boomActor));
        Assert.AreEqual(Angle.Ang45.Data, boomActor.Angle.Data);

        var mbfWorld = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);
        var mbfActor = SpawnActor(mbfWorld);
        mbfActor.Angle = Angle.Ang45;
        mbfActor.State = State(90, 0);

        Assert.IsTrue(MbfStateControlCodePointers.TurnFromState(mbfWorld, mbfActor));
        Assert.AreEqual((Angle.Ang45 + Angle.Ang90).Data, mbfActor.Angle.Data);
    }

    [TestMethod]
    public void TurnUsesRelativeIntegerTruncatedMbfAngle()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewMbfWorld(content);
        var actor = SpawnActor(world);
        actor.Angle = Angle.Ang45;
        actor.State = State(1, 0);

        Assert.IsTrue(MbfStateControlCodePointers.TurnFromState(world, actor));

        var expectedDelta = MbfDegrees(1);
        Assert.AreEqual((Angle.Ang45 + expectedDelta).Data, actor.Angle.Data);
        Assert.AreNotEqual(Angle.FromDegree(1).Data, expectedDelta.Data);
    }

    [TestMethod]
    public void FaceUsesAbsoluteIntegerTruncatedMbfAngle()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewMbfWorld(content);
        var actor = SpawnActor(world);
        actor.Angle = Angle.Ang45;
        actor.State = State(270, 0);

        Assert.IsTrue(MbfStateControlCodePointers.FaceFromState(world, actor));
        Assert.AreEqual(Angle.Ang270.Data, actor.Angle.Data);
    }

    [TestMethod]
    public void RandomJumpChanceZeroNeverChangesState()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewMbfWorld(content);
        var actor = SpawnActor(world);
        var target = FindSafeTargetState();
        var original = State(target.Number, 0);
        actor.State = original;

        Assert.IsFalse(MbfStateControlCodePointers.RandomJumpFromState(world, actor));
        Assert.AreSame(original, actor.State);
    }

    [TestMethod]
    public void RandomJumpChance256AlwaysChangesState()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewMbfWorld(content);
        var actor = SpawnActor(world);
        var target = FindSafeTargetState();
        actor.State = State(target.Number, 256);

        Assert.IsTrue(MbfStateControlCodePointers.RandomJumpFromState(world, actor));
        Assert.AreSame(target, actor.State);
    }

    [TestMethod]
    public void RandomJumpRejectsInvalidTargetWithoutCrashing()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewMbfWorld(content);
        var actor = SpawnActor(world);
        var original = State(DoomInfo.States.Length, 256);
        actor.State = original;

        Assert.IsFalse(MbfStateControlCodePointers.RandomJumpFromState(world, actor));
        Assert.AreSame(original, actor.State);
    }

    [TestMethod]
    public void BexRegistersTurnFaceAndRandomJumpAndExecutesFace()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Frame 186
Unknown 1 = 90

Frame 187
Unknown 1 = 180

Frame 188
Unknown 1 = 186
Unknown 2 = 256

[CODEPTR]
FRAME 186 = Turn
FRAME 187 = Face
FRAME 188 = RandomJump
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            Assert.AreEqual("Turn", DoomInfo.States[186].MobjAction.Method.Name);
            Assert.AreEqual("Face", DoomInfo.States[187].MobjAction.Method.Name);
            Assert.AreEqual("RandomJump", DoomInfo.States[188].MobjAction.Method.Name);

            using var content = GameContent.CreateDummy(WadPath.Doom2);
            var world = NewMbfWorld(content);
            var actor = SpawnActor(world);
            actor.Angle = Angle.Ang45;

            Assert.IsTrue(actor.SetState((MobjState)187));
            Assert.AreEqual(Angle.Ang180.Data, actor.Angle.Data);
        }
        finally
        {
            Reset(wad);
            File.Delete(patch);
        }
    }

    private static World NewMbfWorld(GameContent content)
    {
        return new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);
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

    private static MobjStateDef FindSafeTargetState()
    {
        foreach (var state in DoomInfo.States)
        {
            if (state.Number > 0 &&
                state.Tics > 0 &&
                state.MobjAction == null &&
                state.Next != MobjState.Null)
            {
                return state;
            }
        }

        Assert.Fail("No safe non-action mobj state was found for RandomJump test.");
        return null;
    }

    private static Angle MbfDegrees(int degrees)
    {
        var numerator = unchecked((ulong)(uint)degrees << 32);
        return new Angle((uint)(numerator / 360UL));
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
            "manageddoom-mbf-state-control-" + Guid.NewGuid().ToString("N") + ".bex");
        File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
        return path;
    }
}
