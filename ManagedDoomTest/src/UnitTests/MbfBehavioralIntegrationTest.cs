using System;
using System.IO;
using System.Text;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Detection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class MbfBehavioralIntegrationTest
{
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MBF")]
    public void EmbeddedMbfPatchAndOptionsFlowThroughStartupIntoRuntime()
    {
        var pwad = WriteWad(
            ("OPTIONS", Encoding.ASCII.GetBytes("friend_distance 96\nmonster_backing 1\n")),
            ("DEHACKED", PatchBytes(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 2 (Former Human)
Bits = SOLID+SHOOTABLE+FRIEND

Frame 194
Unknown 1 = 90

[CODEPTR]
FRAME 194 = Turn
")));

        try
        {
            var args = new CommandLineArgs(new[]
            {
                "-iwad", WadPath.Doom2,
                "-file", pwad
            });

            using var content = new GameContent(args);
            var options = new GameOptions(args, content);

            Assert.AreEqual(GameCompatibilityMode.Auto, options.CompatibilityMode);
            Assert.AreEqual(GameCompatibility.Mbf, options.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, options.CompatibilitySource);
            Assert.AreEqual(96, options.MbfOptions.FriendDistance);
            Assert.IsTrue(options.MbfOptions.MonsterBacking);

            var possessedInfo = DoomInfo.MobjInfos[(int)MobjType.Possessed];
            Assert.IsTrue((possessedInfo.Flags & MobjFlags.Friend) != 0);
            Assert.AreEqual("Turn", DoomInfo.States[194].MobjAction.Method.Name);
            Assert.AreEqual(90, DoomInfo.States[194].Misc1);

            var world = new World(content, options, null);
            var player = world.ConsolePlayer.Mobj;
            var actor = world.ThingAllocation.SpawnMobj(
                player.X + Fixed.FromInt(64),
                player.Y,
                Mobj.OnFloorZ,
                MobjType.Possessed);

            Assert.IsTrue((actor.Flags & MobjFlags.Friend) != 0);

            actor.Angle = Angle.Ang0;
            Assert.IsTrue(actor.SetState((MobjState)194));
            Assert.AreEqual(Angle.Ang90.Data, actor.Angle.Data);
        }
        finally
        {
            ResetDefinitions();
            File.Delete(pwad);
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("MBF")]
    public void ExternalIncludedBexFlowsThroughDetectionPatchApplicationAndRuntime()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"manageddoom_mbf_integration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        var parent = Path.Combine(directory, "parent.bex");
        var child = Path.Combine(directory, "child patch.bex");

        File.WriteAllText(
            parent,
            "INCLUDE \"child patch.bex\"\n",
            Encoding.ASCII);

        File.WriteAllText(
            child,
            @"Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Frame 194
Unknown 1 = 45

[CODEPTR]
FRAME 194 = Turn
".Replace("\r\n", "\n"),
            Encoding.ASCII);

        try
        {
            var args = new CommandLineArgs(new[]
            {
                "-iwad", WadPath.Doom2,
                "-deh", parent
            });

            using var content = new GameContent(args);
            var options = new GameOptions(args, content);

            Assert.AreEqual(GameCompatibility.Mbf, options.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, options.CompatibilitySource);
            Assert.AreEqual("Turn", DoomInfo.States[194].MobjAction.Method.Name);
            Assert.AreEqual(45, DoomInfo.States[194].Misc1);

            var world = new World(content, options, null);
            var player = world.ConsolePlayer.Mobj;
            var actor = world.ThingAllocation.SpawnMobj(
                player.X + Fixed.FromInt(64),
                player.Y,
                Mobj.OnFloorZ,
                MobjType.Possessed);

            actor.Angle = Angle.Ang45;
            Assert.IsTrue(actor.SetState((MobjState)194));
            Assert.AreEqual(Angle.Ang90.Data, actor.Angle.Data);
        }
        finally
        {
            ResetDefinitions();
            Directory.Delete(directory, recursive: true);
        }
    }

    private static byte[] PatchBytes(string text)
    {
        return Encoding.ASCII.GetBytes(
            text.Replace("\r\n", "\n").TrimStart());
    }

    private static string WriteWad(params (string Name, byte[] Data)[] lumps)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"manageddoom_mbf_integration_{Guid.NewGuid():N}.wad");

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write(Encoding.ASCII.GetBytes("PWAD"));
        writer.Write(lumps.Length);
        writer.Write(0);

        var positions = new int[lumps.Length];

        for (var i = 0; i < lumps.Length; i++)
        {
            positions[i] = (int)stream.Position;
            writer.Write(lumps[i].Data);
        }

        var directoryOffset = (int)stream.Position;

        for (var i = 0; i < lumps.Length; i++)
        {
            writer.Write(positions[i]);
            writer.Write(lumps[i].Data.Length);

            var name = new byte[8];
            Encoding.ASCII.GetBytes(lumps[i].Name).CopyTo(name, 0);
            writer.Write(name);
        }

        stream.Position = 8;
        writer.Write(directoryOffset);

        return path;
    }

    private static void ResetDefinitions()
    {
        using var wad = new Wad(WadPath.Doom2);
        DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
    }
}
