using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdBexMergeOrderTest
    {
        [TestMethod]
        public void MultipleEmbeddedLumpsAcrossLoadedPwadsMergeInWadOrder()
        {
            var first = WriteWad(("DEHACKED", PatchBytes(@"
[STRINGS]
PRESSKEY = first embedded

[PARS]
par 1 111
")));

            var second = WriteWad(("DEHACKED", PatchBytes(@"
[STRINGS]
PRESSKEY = second embedded

[PARS]
par 2 222
")));

            try
            {
                using var wad = new Wad(WadPath.Doom2, first, second);
                Reset(wad);

                DeHackEd.Initialize(new CommandLineArgs(Array.Empty<string>()), wad);

                Assert.AreEqual("second embedded", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(111, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(222, DoomInfo.ParTimes.Doom2[1]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(first);
                File.Delete(second);
            }
        }

        [TestMethod]
        public void MultipleEmbeddedLumpsInsideOnePwadMergeInDirectoryOrder()
        {
            var pwad = WriteWad(
                ("DEHACKED", PatchBytes(@"
[STRINGS]
PRESSKEY = first lump

[PARS]
par 3 333
")),
                ("DEHACKED", PatchBytes(@"
[STRINGS]
PRESSKEY = second lump

[PARS]
par 4 444
")));

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);

                DeHackEd.Initialize(new CommandLineArgs(Array.Empty<string>()), wad);

                Assert.AreEqual("second lump", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(333, DoomInfo.ParTimes.Doom2[2]);
                Assert.AreEqual(444, DoomInfo.ParTimes.Doom2[3]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void ExternalPatchesApplyAfterEmbeddedPatchesAndIndependentChangesSurvive()
        {
            var external = CreatePatch(@"
[STRINGS]
PRESSKEY = external value

[PARS]
par 1 111
");

            var pwad = WriteWad(("DEHACKED", PatchBytes(@"
[STRINGS]
PRESSKEY = embedded value

[PARS]
par 2 222
")));

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);

                DeHackEd.Initialize(new CommandLineArgs(new[] { "-deh", external }), wad);

                Assert.AreEqual("external value", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(111, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(222, DoomInfo.ParTimes.Doom2[1]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(external);
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void MixedClassicDehAndBexRespectExternalCommandLineOrder()
        {
            var classic = CreatePatch(@"
Text 12 11
press a key.classic-deh
");

            var bex = CreatePatch(@"
[STRINGS]
PRESSKEY = bex-value
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                Reset(wad);

                DeHackEd.Initialize(
                    new CommandLineArgs(new[] { "-deh", classic, bex, "-nodeh" }),
                    wad);
                Assert.AreEqual("bex-value", DoomInfo.Strings.PRESSKEY.ToString());

                DeHackEd.Initialize(
                    new CommandLineArgs(new[] { "-deh", bex, classic, "-nodeh" }),
                    wad);
                Assert.AreEqual("classic-deh", DoomInfo.Strings.PRESSKEY.ToString());
            }
            finally
            {
                Reset(wad);
                File.Delete(classic);
                File.Delete(bex);
            }
        }

        [TestMethod]
        public void NodehSuppressesAllEmbeddedDehackedLumps()
        {
            var pwad = WriteWad(
                ("DEHACKED", PatchBytes(@"
[STRINGS]
PRESSKEY = should not apply one

[PARS]
par 1 111
")),
                ("DEHACKED", PatchBytes(@"
[STRINGS]
PRESSKEY = should not apply two

[PARS]
par 2 222
")));

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);

                var originalPressKey = DoomInfo.Strings.PRESSKEY.ToString();
                var originalMap1 = DoomInfo.ParTimes.Doom2[0];
                var originalMap2 = DoomInfo.ParTimes.Doom2[1];

                DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);

                Assert.AreEqual(originalPressKey, DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(originalMap1, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(originalMap2, DoomInfo.ParTimes.Doom2[1]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void FailureInLaterExternalPatchRollsBackEarlierEmbeddedAndExternalChanges()
        {
            var firstExternal = CreatePatch(@"
[PARS]
par 2 222
");

            var failingExternal = CreatePatch(@"
Text 12 20
press a key.too short
");

            var pwad = WriteWad(("DEHACKED", PatchBytes(@"
[PARS]
par 1 111
")));

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);

                var originalMap1 = DoomInfo.ParTimes.Doom2[0];
                var originalMap2 = DoomInfo.ParTimes.Doom2[1];
                var originalPressKey = DoomInfo.Strings.PRESSKEY.ToString();

                Assert.ThrowsExactly<Exception>(() =>
                    DeHackEd.Initialize(
                        new CommandLineArgs(new[] { "-deh", firstExternal, failingExternal }),
                        wad));

                Assert.AreEqual(originalMap1, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(originalMap2, DoomInfo.ParTimes.Doom2[1]);
                Assert.AreEqual(originalPressKey, DoomInfo.Strings.PRESSKEY.ToString());
            }
            finally
            {
                ResetGlobalState();
                File.Delete(firstExternal);
                File.Delete(failingExternal);
                File.Delete(pwad);
            }
        }

        private static string CreatePatch(string body)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-bex-order-" + Guid.NewGuid().ToString("N") + ".bex");

            File.WriteAllText(path, PatchText(body));
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

        private static void Reset(Wad wad)
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
        }

        private static void ResetGlobalState()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);
        }

        private static string WriteWad(params (string Name, byte[] Data)[] lumps)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-bex-order-" + Guid.NewGuid().ToString("N") + ".wad");

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
