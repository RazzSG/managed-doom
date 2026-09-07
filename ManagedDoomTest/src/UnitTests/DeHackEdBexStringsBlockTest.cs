using System;
using System.IO;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdBexStringsBlockTest
    {
        [TestMethod]
        public void BexStringsReplacementIsConsumedByFinaleAtRuntime()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            const string replacement = "Boom BEX runtime finale text.";
            var patch = CreatePatch(
                "[STRINGS]\n" +
                "C1TEXT = " + replacement + "\n");

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
        public void BexStringsExtendedReaderJoinsLinesAndDoesNotStealSectionLikeText()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "[STRINGS]\n" +
                "C1TEXT = first part \\\n" +
                "        Frame 7 \\\n" +
                "        final part\\nnext line\n" +
                "\n" +
                "Frame 10\n" +
                "Duration = 9\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(
                    "first part Frame 7 final part\nnext line",
                    DoomInfo.Strings.C1TEXT.ToString());
                Assert.AreEqual(
                    9,
                    DoomInfo.States[10].Tics,
                    "The real Frame block after the blank line must still be parsed.");
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void BexStringsBackgroundAliasIsConsumedByFinaleAtRuntime()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "[STRINGS]\n" +
                "BGFLAT06 = RROCK14\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("RROCK14", DoomString.Resolve("SLIME16"));

                var options = new GameOptions
                {
                    GameMode = GameMode.Commercial,
                    MissionPack = MissionPack.Doom2,
                    Map = 6
                };
                var finale = new Finale(options);

                Assert.AreEqual("RROCK14", finale.Flat);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void UnknownBexStringMnemonicIsIgnoredAndLaterKnownAssignmentStillApplies()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "[STRINGS]\n" +
                "MANAGEDDOOM_UNKNOWN = ignored\n" +
                "PRESSKEY = known replacement\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("known replacement", DoomInfo.Strings.PRESSKEY.ToString());
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void BexNamedReplacementUsesOriginalStringSubstitutionSemantics()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var duplicate = new DoomString("press a key.");
            var patch = CreatePatch(
                "[STRINGS]\n" +
                "PRESSKEY = named only\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("named only", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(
                    "named only",
                    duplicate.ToString(),
                    "BEX mnemonics resolve to the original executable string, so equal originals share the substitution.");
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void BexStringsReplacementsAreResetBetweenContentLoads()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "[STRINGS]\n" +
                "PRESSKEY = temporary prompt\n" +
                "BGFLAT06 = RROCK14\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual("temporary prompt", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual("RROCK14", DoomString.Resolve("SLIME16"));

                Reset(wad);

                Assert.AreEqual("press a key.", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual("SLIME16", DoomString.Resolve("SLIME16"));
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

        private static string CreatePatch(string body)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-bex-strings-" + Guid.NewGuid().ToString("N") + ".bex");

            var text =
                "Patch File for DeHackEd v3.0\n" +
                "Doom version = 19\n" +
                "Patch format = 6\n\n" +
                body;

            File.WriteAllText(path, text);
            return path;
        }
    }
}
