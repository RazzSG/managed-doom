using System;
using System.IO;
using System.Text;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class MbfExtendedDeHackEdNamespaceTest
{
    [TestMethod]
    public void ClassicMbfNamespaceIncludesBetaThingsStatesSpritesAndSounds()
    {
        using var wad = new Wad(WadPath.Doom2);
        Reset(wad);

        Assert.AreEqual(144, DoomInfo.MobjInfos.Length);
        Assert.AreEqual(1076, DoomInfo.States.Length);
        Assert.AreEqual(144, (int)Sprite.Count);
        Assert.AreEqual(144, DoomInfo.SpriteNames.Length);
        Assert.AreEqual(114, Enum.GetValues<Sfx>().Length);
        Assert.AreEqual(114, DoomInfo.SfxNames.Length);
        Assert.AreEqual(114, DoomInfo.DeHackEdSoundInfos.Length);

        Assert.AreEqual(140, (int)MobjType.MbfBetaPlasma1);
        Assert.AreEqual(143, (int)MobjType.MbfBetaBible);
        Assert.AreEqual(999, (int)MobjState.MbfBetaState999);
        Assert.AreEqual(1054, (int)MobjState.MbfBetaSceptre);
        Assert.AreEqual(1055, (int)MobjState.MbfBetaBible);
        Assert.AreEqual(1075, (int)MobjState.MbfMushroom);

        Assert.AreEqual("PLS1", DoomInfo.SpriteNames[(int)Sprite.PLS1].ToString());
        Assert.AreEqual("PLS2", DoomInfo.SpriteNames[(int)Sprite.PLS2].ToString());
        Assert.AreEqual("BON3", DoomInfo.SpriteNames[(int)Sprite.BON3].ToString());
        Assert.AreEqual("BON4", DoomInfo.SpriteNames[(int)Sprite.BON4].ToString());

        Assert.AreEqual("dgsit", DoomInfo.SfxNames[(int)Sfx.DGSIT].ToString());
        Assert.AreEqual("dgpain", DoomInfo.SfxNames[(int)Sfx.DGPAIN].ToString());

        Assert.IsNotNull(DoomInfo.States[1062].MobjAction);
        Assert.AreEqual("BetaSkullAttack", DoomInfo.States[1062].MobjAction.Method.Name);
        Assert.IsNotNull(DoomInfo.States[1074].MobjAction);
        Assert.AreEqual("Stop", DoomInfo.States[1074].MobjAction.Method.Name);
        Assert.IsNotNull(DoomInfo.States[1075].MobjAction);
        Assert.AreEqual("Mushroom", DoomInfo.States[1075].MobjAction.Method.Name);
    }


    [TestMethod]
    public void ClassicMbfBetaSkullStatesMatchPrBoomBaseline()
    {
        using var wad = new Wad(WadPath.Doom2);
        Reset(wad);

        AssertState(1056, Sprite.SKUL, 0, 10, "Look", 1056);
        AssertState(1057, Sprite.SKUL, 1, 5, "Chase", 1058);
        AssertState(1058, Sprite.SKUL, 2, 5, "Chase", 1059);
        AssertState(1059, Sprite.SKUL, 3, 5, "Chase", 1060);
        AssertState(1060, Sprite.SKUL, 0, 5, "Chase", 1057);
        AssertState(1061, Sprite.SKUL, 4, 4, "FaceTarget", 1062);
        AssertState(1062, Sprite.SKUL, 5, 5, "BetaSkullAttack", 1063);
        AssertState(1063, Sprite.SKUL, 5, 4, null, 1057);
        AssertState(1064, Sprite.SKUL, 6, 4, null, 1065);
        AssertState(1065, Sprite.SKUL, 7, 2, "Pain", 1057);
        AssertState(1066, Sprite.SKUL, 8, 4, null, 1057);
        AssertState(1067, Sprite.SKUL, 9, 5, null, 1068);
        AssertState(1068, Sprite.SKUL, 10, 5, null, 1069);
        AssertState(1069, Sprite.SKUL, 11, 5, null, 1070);
        AssertState(1070, Sprite.SKUL, 12, 5, null, 1071);
        AssertState(1071, Sprite.SKUL, 13, 5, "Scream", 1072);
        AssertState(1072, Sprite.SKUL, 14, 5, null, 1073);
        AssertState(1073, Sprite.SKUL, 15, 5, "Fall", 1074);
        AssertState(1074, Sprite.SKUL, 16, 5, "Stop", 1074);
        AssertState(1075, Sprite.MISL, 32769, 8, "Mushroom", (int)MobjState.Explode2);
    }

    [TestMethod]
    public void FrozenHeartPartialBetaSkullOverridePreservesCanonicalChaseFrame()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 21
Patch format = 6

Frame 1057
Sprite number = 133
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-deh", patch, "-nodeh" }), wad);

            var state = DoomInfo.States[1057];
            Assert.AreEqual((Sprite)133, state.Sprite);
            Assert.AreEqual(1, state.Frame);
            Assert.AreEqual(5, state.Tics);
            Assert.AreEqual((MobjState)1058, state.Next);
            Assert.IsNotNull(state.MobjAction);
            Assert.AreEqual("Chase", state.MobjAction.Method.Name);
        }
        finally
        {
            Reset(wad);
            File.Delete(patch);
        }
    }

    [TestMethod]
    public void FrozenHeartStyleHighNumberBlocksAreAppliedInsteadOfIgnored()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 21
Patch format = 6

Thing 141
ID # = 302
Initial frame = 13
Bits = TRANSLUCENT+NOGRAVITY

Thing 144
Width = 1048576
Height = 3932160
Bits = SOLID

Frame 999
Sprite number = 139
Sprite subnumber = 8
Duration = 5

Frame 1075
Sprite number = 42
Sprite subnumber = 8
Next frame = 543
Unknown 1 = 205
Unknown 2 = 10069

Sound 109
Zero/One = 1
Value = 98
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-deh", patch, "-nodeh" }), wad);

            var thing141 = DoomInfo.MobjInfos[140];
            Assert.AreEqual(302, thing141.DoomEdNum);
            Assert.AreEqual((MobjState)13, thing141.SpawnState);
            Assert.IsTrue(thing141.Translucent);
            Assert.IsTrue((thing141.Flags & MobjFlags.NoGravity) != 0);

            var thing144 = DoomInfo.MobjInfos[143];
            Assert.AreEqual(Fixed.FromInt(16).Data, thing144.Radius.Data);
            Assert.AreEqual(Fixed.FromInt(60).Data, thing144.Height.Data);
            Assert.AreEqual(MobjFlags.Solid, thing144.Flags);

            var firstHighState = DoomInfo.States[999];
            Assert.AreEqual(Sprite.DOGS, firstHighState.Sprite);
            Assert.AreEqual(8, firstHighState.Frame);
            Assert.AreEqual(5, firstHighState.Tics);

            var finalHighState = DoomInfo.States[1075];
            Assert.AreEqual((Sprite)42, finalHighState.Sprite);
            Assert.AreEqual(8, finalHighState.Frame);
            Assert.AreEqual((MobjState)543, finalHighState.Next);
            Assert.AreEqual(205, finalHighState.Misc1);
            Assert.AreEqual(10069, finalHighState.Misc2);

            var sound = DoomInfo.DeHackEdSoundInfos[109];
            Assert.AreEqual(1, sound.Singularity.Value);
            Assert.AreEqual(98, sound.Priority.Value);
        }
        finally
        {
            Reset(wad);
            File.Delete(patch);
        }
    }

    [TestMethod]
    public void SparseSpriteFramesRemainNullButDoNotAbortLookupConstruction()
    {
        byte[] patchData;
        using (var source = new Wad(WadPath.Doom2))
        {
            patchData = (byte[])source.ReadLump("TROOA1").Clone();
        }

        var pwad = WriteWad(
            ("S_START", Array.Empty<byte>()),
            ("BON3A0", patchData),
            ("BON3C0", patchData),
            ("S_END", Array.Empty<byte>()));

        try
        {
            using var wad = new Wad(WadPath.Doom2, pwad);

            var real = new SpriteLookup(wad)[Sprite.BON3];
            Assert.AreEqual(3, real.Frames.Length);
            Assert.IsTrue(real.TryGetFrame(0, out _));
            Assert.IsFalse(real.TryGetFrame(1, out _));
            Assert.IsTrue(real.TryGetFrame(2, out _));

            var dummy = new DummySpriteLookup(wad)[Sprite.BON3];
            Assert.AreEqual(3, dummy.Frames.Length);
            Assert.IsTrue(dummy.TryGetFrame(0, out _));
            Assert.IsFalse(dummy.TryGetFrame(1, out _));
            Assert.IsTrue(dummy.TryGetFrame(2, out _));
        }
        finally
        {
            File.Delete(pwad);
        }
    }

    [TestMethod]
    public void ExistingButIncompleteRotationSetStillFailsValidation()
    {
        byte[] patchData;
        using (var source = new Wad(WadPath.Doom2))
        {
            patchData = (byte[])source.ReadLump("TROOA1").Clone();
        }

        var pwad = WriteWad(
            ("S_START", Array.Empty<byte>()),
            ("BON4A1", patchData),
            ("S_END", Array.Empty<byte>()));

        try
        {
            using var wad = new Wad(WadPath.Doom2, pwad);
            AssertMissingSprite(() => new SpriteLookup(wad));
            AssertMissingSprite(() => new DummySpriteLookup(wad));
        }
        finally
        {
            File.Delete(pwad);
        }
    }


    private static void AssertState(int index, Sprite sprite, int frame, int tics, string actionName, int next)
    {
        var state = DoomInfo.States[index];
        Assert.AreEqual(sprite, state.Sprite);
        Assert.AreEqual(frame, state.Frame);
        Assert.AreEqual(tics, state.Tics);
        Assert.AreEqual((MobjState)next, state.Next);

        if (actionName == null)
        {
            Assert.IsNull(state.MobjAction);
        }
        else
        {
            Assert.IsNotNull(state.MobjAction);
            Assert.AreEqual(actionName, state.MobjAction.Method.Name);
        }
    }

    private static void AssertMissingSprite(Action action)
    {
        try
        {
            action();
            Assert.Fail("An incomplete rotation set must still be rejected.");
        }
        catch (Exception e)
        {
            StringAssert.Contains(e.Message, "Missing sprite!");
        }
    }

    private static void Reset(Wad wad)
    {
        DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
    }

    private static string CreatePatch(string text)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "manageddoom-mbf-extended-namespace-" + Guid.NewGuid().ToString("N") + ".deh");
        File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
        return path;
    }

    private static string WriteWad(params (string Name, byte[] Data)[] lumps)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "manageddoom-mbf-sparse-sprite-" + Guid.NewGuid().ToString("N") + ".wad");

        var positions = new int[lumps.Length];

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("PWAD"));
        writer.Write(lumps.Length);
        writer.Write(0);

        for (var i = 0; i < lumps.Length; i++)
        {
            positions[i] = checked((int)stream.Position);
            writer.Write(lumps[i].Data);
        }

        var directoryOffset = checked((int)stream.Position);

        for (var i = 0; i < lumps.Length; i++)
        {
            writer.Write(positions[i]);
            writer.Write(lumps[i].Data.Length);

            var name = new byte[8];
            var encoded = Encoding.ASCII.GetBytes(lumps[i].Name);
            Buffer.BlockCopy(encoded, 0, name, 0, Math.Min(encoded.Length, name.Length));
            writer.Write(name);
        }

        stream.Position = 8;
        writer.Write(directoryOffset);
        writer.Flush();

        return path;
    }
}
