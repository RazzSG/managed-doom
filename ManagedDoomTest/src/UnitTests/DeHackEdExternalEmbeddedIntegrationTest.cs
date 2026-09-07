using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdExternalEmbeddedIntegrationTest
    {
        [TestMethod]
        public void GameContentAppliesEmbeddedThenExternalAndPreservesIndependentChanges()
        {
            var pwad = WriteWad(("DEHACKED", PatchBytes(@"
Thing 2 (Former Human)
Hit points = 111

[STRINGS]
PRESSKEY = embedded value

[PARS]
par 1 101
")));

            var external = CreatePatch(@"
Thing 2 (Former Human)
Hit points = 222

[STRINGS]
PRESSKEY = external value

[PARS]
par 2 202
", ".bex");

            try
            {
                var args = new CommandLineArgs(new[]
                {
                    "-iwad", WadPath.Doom2,
                    "-file", pwad,
                    "-deh", external
                });

                using var content = new GameContent(args);

                Assert.AreEqual(222, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth,
                    "The explicit external patch must override the conflicting embedded assignment.");
                Assert.AreEqual("external value", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(101, DoomInfo.ParTimes.Doom2[0],
                    "Independent data from the embedded patch must survive the later external patch.");
                Assert.AreEqual(202, DoomInfo.ParTimes.Doom2[1]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(external);
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void NodehSuppressesEmbeddedPatchButStillAppliesExplicitExternalPatchThroughGameContent()
        {
            var pwad = WriteWad(("DEHACKED", PatchBytes(@"
Thing 2 (Former Human)
Hit points = 333

[PARS]
par 1 303
")));

            var external = CreatePatch(@"
Misc 0
Initial Health = 77

[STRINGS]
PRESSKEY = explicit external
", ".deh");

            try
            {
                var args = new CommandLineArgs(new[]
                {
                    "-iwad", WadPath.Doom2,
                    "-file", pwad,
                    "-deh", external,
                    "-nodeh"
                });

                using var content = new GameContent(args);

                Assert.AreEqual(20, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth,
                    "-nodeh must suppress embedded DEHACKED data.");
                Assert.AreEqual(30, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(77, DoomInfo.DeHackEdConst.InitialHealth,
                    "-nodeh must not suppress an explicitly supplied -deh file.");
                Assert.AreEqual("explicit external", DoomInfo.Strings.PRESSKEY.ToString());
            }
            finally
            {
                ResetGlobalState();
                File.Delete(external);
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void GameContentPreservesCommandLineOrderAcrossMultipleExternalDehAndBexFiles()
        {
            var pwad = WriteWad(("DEHACKED", PatchBytes(@"
[PARS]
par 1 111
")));

            var first = CreatePatch(@"
[STRINGS]
PRESSKEY = first external

[PARS]
par 2 222
", ".deh");

            var second = CreatePatch(@"
[STRINGS]
PRESSKEY = second external

[PARS]
par 3 333
", ".bex");

            try
            {
                var args = new CommandLineArgs(new[]
                {
                    "-iwad", WadPath.Doom2,
                    "-file", pwad,
                    "-deh", first, second
                });

                using var content = new GameContent(args);

                Assert.AreEqual("second external", DoomInfo.Strings.PRESSKEY.ToString(),
                    "The last valid external assignment must win.");
                Assert.AreEqual(111, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(222, DoomInfo.ParTimes.Doom2[1]);
                Assert.AreEqual(333, DoomInfo.ParTimes.Doom2[2]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(second);
                File.Delete(first);
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void GameContentFailureInLateExternalPatchRollsBackEmbeddedAndEarlierExternalChanges()
        {
            var pwad = WriteWad(("DEHACKED", PatchBytes(@"
Thing 2 (Former Human)
Hit points = 444

[PARS]
par 1 444
")));

            var first = CreatePatch(@"
Misc 0
Initial Health = 55

[STRINGS]
PRESSKEY = temporary external
", ".deh");

            var failing = CreatePatch(@"
Text 12 20
press a key.too short
", ".bex");

            try
            {
                var args = new CommandLineArgs(new[]
                {
                    "-iwad", WadPath.Doom2,
                    "-file", pwad,
                    "-deh", first, failing
                });

                Assert.ThrowsExactly<Exception>(() =>
                {
                    using var content = new GameContent(args);
                });

                Assert.AreEqual(20, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth,
                    "A failed GameContent load must restore the pristine Thing baseline.");
                Assert.AreEqual(100, DoomInfo.DeHackEdConst.InitialHealth,
                    "A failed GameContent load must roll back earlier external assignments.");
                Assert.AreEqual("press a key.", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(30, DoomInfo.ParTimes.Doom2[0]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(failing);
                File.Delete(first);
                File.Delete(pwad);
            }
        }

        private static string CreatePatch(string body, string extension)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-integration-" + Guid.NewGuid().ToString("N") + extension);

            File.WriteAllText(path, PatchText(body), Encoding.ASCII);
            return path;
        }

        private static byte[] PatchBytes(string body)
        {
            return Encoding.ASCII.GetBytes(PatchText(body));
        }

        private static string PatchText(string body)
        {
            return
                "Patch File for DeHackEd v3.0\n" +
                "Doom version = 19\n" +
                "Patch format = 6\n\n" +
                Normalize(body);
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n").TrimStart();
        }

        private static void ResetGlobalState()
        {
            using var wad = new Wad(WadPath.Doom2);
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
        }

        private static string WriteWad(params (string Name, byte[] Data)[] lumps)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-integration-" + Guid.NewGuid().ToString("N") + ".wad");

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
}
