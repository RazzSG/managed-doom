using System;
using System.IO;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdTextBlockTest
    {
        [TestMethod]
        public void ClassicTextReplacementIsConsumedByFinaleAtRuntime()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var original = DoomInfo.Strings.C1TEXT.ToString();
            const string replacement = "Classic Text runtime replacement.";
            var patch = CreateTextPatch(original, replacement);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var options = new GameOptions
                {
                    GameMode = GameMode.Commercial,
                    MissionPack = MissionPack.Doom2,
                    Map = 6
                };
                var finale = new Finale(options);

                Assert.AreEqual(replacement, DoomInfo.Strings.C1TEXT.ToString());
                Assert.AreEqual(replacement, finale.Text);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementUpdatesAllMatchingRegisteredStrings()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var original = "manageddoom duplicate " + Guid.NewGuid().ToString("N");
            var replacement = "replacement " + Guid.NewGuid().ToString("N");
            var first = new DoomString(original);
            var second = new DoomString(original);
            var patch = CreateTextPatch(original, replacement);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(replacement, first.ToString());
                Assert.AreEqual(replacement, second.ToString());
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void NegativeTextLengthReportsTextBlockAndRollsBackEarlierChanges()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 2
Hit points = 1234

Text -1 5
abcde
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                var exception = Assert.ThrowsExactly<Exception>(
                    () => DeHackEd.Initialize(ArgsForPatch(patch), wad));
                var details = exception.ToString();

                StringAssert.Contains(details, "Failed to process block: Text (line");
                StringAssert.Contains(details, "Malformed Text block header");
                Assert.AreEqual(20, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
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

        private static string CreateTextPatch(string original, string replacement)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-text-" + Guid.NewGuid().ToString("N") + ".deh");

            var text =
                "Patch File for DeHackEd v3.0\n" +
                "Doom version = 19\n" +
                "Patch format = 6\n\n" +
                "Text " + original.Length + " " + replacement.Length + "\n" +
                original + replacement + "\n";

            File.WriteAllText(path, text);
            return path;
        }

        private static string CreatePatch(string text)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-text-" + Guid.NewGuid().ToString("N") + ".deh");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
