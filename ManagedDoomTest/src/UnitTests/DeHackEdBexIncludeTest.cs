using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdBexIncludeTest
    {
        [TestMethod]
        public void IncludeIsAppliedAtDirectivePositionAndResolvesRelativeQuotedPath()
        {
            var directory = CreateTempDirectory();
            var child = Path.Combine(directory, "child patch.bex");
            var main = Path.Combine(directory, "main.bex");

            WritePatch(child, @"
[PARS]
par 1 22
");

            WritePatch(main, @"
[PARS]
par 1 11

INCLUDE ""child patch.bex""

[PARS]
par 1 33
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                Reset(wad);
                DeHackEd.Initialize(ArgsForPatch(main), wad);

                // The include is processed in place: the later assignment in
                // the parent patch must still win over the included value.
                Assert.AreEqual(33, DoomInfo.ParTimes.Doom2[0]);
            }
            finally
            {
                Reset(wad);
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void IncludeNotextSkipsClassicTextButStillAppliesBexStrings()
        {
            var directory = CreateTempDirectory();
            var child = Path.Combine(directory, "child.bex");
            var main = Path.Combine(directory, "main.bex");

            WritePatch(child, @"
Text 12 12
press a key.classic skip

[STRINGS]
PRESSYN = bex strings still apply
");

            WritePatch(main, @"
include notext child.bex
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                Reset(wad);
                var originalPressKey = DoomInfo.Strings.PRESSKEY.ToString();

                DeHackEd.Initialize(ArgsForPatch(main), wad);

                Assert.AreEqual(originalPressKey, DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual("bex strings still apply", DoomInfo.Strings.PRESSYN.ToString());
            }
            finally
            {
                Reset(wad);
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void NormalIncludeAppliesClassicText()
        {
            var directory = CreateTempDirectory();
            var child = Path.Combine(directory, "child.deh");
            var main = Path.Combine(directory, "main.bex");

            WritePatch(child, @"
Text 12 12
press a key.classic text
");

            WritePatch(main, @"
INCLUDE child.deh
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                Reset(wad);
                DeHackEd.Initialize(ArgsForPatch(main), wad);

                Assert.AreEqual("classic text", DoomInfo.Strings.PRESSKEY.ToString());
            }
            finally
            {
                Reset(wad);
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void NestedIncludeIsIgnoredButLaterIncludedDataStillApplies()
        {
            var directory = CreateTempDirectory();
            var grandchild = Path.Combine(directory, "grandchild.bex");
            var child = Path.Combine(directory, "child.bex");
            var main = Path.Combine(directory, "main.bex");

            WritePatch(grandchild, @"
[PARS]
par 1 999
");

            WritePatch(child, @"
INCLUDE grandchild.bex

[PARS]
par 2 222
");

            WritePatch(main, @"
INCLUDE child.bex
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                Reset(wad);
                var originalMap1 = DoomInfo.ParTimes.Doom2[0];

                DeHackEd.Initialize(ArgsForPatch(main), wad);

                Assert.AreEqual(originalMap1, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(222, DoomInfo.ParTimes.Doom2[1]);
            }
            finally
            {
                Reset(wad);
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void IncludedPatchDoomVersionDoesNotLeakBackIntoParent()
        {
            var directory = CreateTempDirectory();
            var child = Path.Combine(directory, "child.bex");
            var main = Path.Combine(directory, "main.bex");

            WritePatchWithVersion(child, 16, string.Empty);
            WritePatchWithVersion(main, 19,
                "INCLUDE child.bex\n\n" +
                "Sprite " + (int)Sprite.TROO + "\n" +
                "Offset = " + SpriteOffset(19, Sprite.POSS) + "\n");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                Reset(wad);
                DeHackEd.Initialize(ArgsForPatch(main), wad);

                Assert.AreEqual("POSS", DoomInfo.SpriteNames[(int)Sprite.TROO].ToString());
            }
            finally
            {
                Reset(wad);
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void EmbeddedDehackedLumpCannotIncludeExternalFile()
        {
            var directory = CreateTempDirectory();
            var child = Path.Combine(directory, "external.bex");

            WritePatch(child, @"
[PARS]
par 1 999
");

            var lumpText = "INCLUDE \"" + child + "\"\n\n[PARS]\npar 2 333\n";
            var pwad = WriteWad(("DEHACKED", Encoding.ASCII.GetBytes(lumpText)));

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);
                var originalMap1 = DoomInfo.ParTimes.Doom2[0];

                DeHackEd.Initialize(new CommandLineArgs(Array.Empty<string>()), wad);

                Assert.AreEqual(originalMap1, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(333, DoomInfo.ParTimes.Doom2[1]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(pwad);
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void MissingIncludeFailsPredictablyAndRollsBackEarlierChanges()
        {
            var directory = CreateTempDirectory();
            var main = Path.Combine(directory, "main.bex");

            WritePatch(main, @"
[PARS]
par 1 123

INCLUDE missing-child.bex
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                Reset(wad);
                var originalMap1 = DoomInfo.ParTimes.Doom2[0];

                var exception = Assert.ThrowsExactly<Exception>(
                    () => DeHackEd.Initialize(ArgsForPatch(main), wad));

                StringAssert.Contains(exception.ToString(), "Included DeHackEd/BEX patch was not found");
                Assert.AreEqual(originalMap1, DoomInfo.ParTimes.Doom2[0]);
            }
            finally
            {
                Reset(wad);
                Directory.Delete(directory, true);
            }
        }

        private static CommandLineArgs ArgsForPatch(string patchPath)
        {
            return new CommandLineArgs(new[] { "-deh", patchPath, "-nodeh" });
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

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-bex-include-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void WritePatch(string path, string body)
        {
            WritePatchWithVersion(path, 19, body);
        }

        private static void WritePatchWithVersion(string path, int doomVersion, string body)
        {
            File.WriteAllText(
                path,
                "Patch File for DeHackEd v3.0\n" +
                "Doom version = " + doomVersion + "\n" +
                "Patch format = 6\n\n" +
                Normalize(body));
        }

        private static int SpriteOffset(int doomVersion, Sprite sourceSprite)
        {
            int textOffset;
            switch (doomVersion)
            {
                case 16:
                case 17:
                case 20:
                    textOffset = 129044;
                    break;
                case 19:
                    textOffset = 129284;
                    break;
                case 21:
                    textOffset = 129380;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(doomVersion));
            }

            return textOffset + 22044 + (int)sourceSprite * 8;
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n").TrimStart();
        }

        private static string WriteWad(params (string Name, byte[] Data)[] lumps)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-bex-include-" + Guid.NewGuid().ToString("N") + ".wad");

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
