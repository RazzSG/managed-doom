using System;
using System.IO;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdParserContractTest
    {
        [TestMethod]
        public void BlankLineTerminatesClassicSectionAndDetachedAssignmentsAreIgnored()
        {
            var patch = CreatePatch(@"
Thing 2
Hit points = 111

Hit points = 999

Thing 4
Hit points = 333
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(111, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                Assert.AreEqual(333, DoomInfo.MobjInfos[(int)MobjType.Vile].SpawnHealth);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void BlankLineTerminatesBexParsSection()
        {
            var patch = CreatePatch(@"
[PARS]
par 1 111

par 1 999

[PARS]
par 2 222
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(111, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(222, DoomInfo.ParTimes.Doom2[1]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicParserAcceptsCaseWhitespaceAndLeadingWhitespaceComments()
        {
            var patch = CreatePatch(@"
   # this entire line is a comment

   tHiNg   2   (Former Human)
   hIt PoInTs    =    4321
   ReAcTiOn TiMe =    17
   # comment inside the section
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var info = DoomInfo.MobjInfos[(int)MobjType.Possessed];
                Assert.AreEqual(4321, info.SpawnHealth);
                Assert.AreEqual(17, info.ReactionTime);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicIntegerDecoderAcceptsSignedPrefixAndTrailingText()
        {
            var patch = CreatePatch(@"
Thing 2
Hit points = +1234 trailing text
Reaction time = -7 ignored suffix
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var info = DoomInfo.MobjInfos[(int)MobjType.Possessed];
                Assert.AreEqual(1234, info.SpawnHealth);
                Assert.AreEqual(-7, info.ReactionTime);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicTextPayloadTreatsCommentAndSectionLookingLinesAsRawCharacters()
        {
            var original = "parser-contract-" + Guid.NewGuid().ToString("N");
            const string replacement = "# literal comment\nThing 2\nFrame 10";
            var target = new DoomString(original);
            var patch = CreateClassicTextPatch(original, replacement);

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual(replacement, target.ToString());
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void DuplicateClassicAssignmentsUseLastValueWithinTheSection()
        {
            var patch = CreatePatch(@"
Thing 2
Hit points = 111
Hit points = 222
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual(222, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void MalformedRecognizedTextHeaderReportsSourceBlockAndRollsBack()
        {
            var patch = CreatePatch(@"
Thing 2
Hit points = 9876

Text nope 5
abcde
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                var exception = Assert.ThrowsExactly<Exception>(
                    () => DeHackEd.Initialize(ArgsForPatch(patch), wad));
                var details = exception.ToString();

                StringAssert.Contains(details, Path.GetFileName(patch));
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

        [TestMethod]
        public void UnknownTopLevelLinesDoNotAffectFollowingKnownSection()
        {
            var patch = CreatePatch(@"
MANAGEDDOOM_UNKNOWN_SECTION 123
Hit points = 9999

Thing 2
Hit points = 2468
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual(2468, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
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
                "manageddoom-deh-parser-contract-" + Guid.NewGuid().ToString("N") + ".deh");

            var text =
                "Patch File for DeHackEd v3.0\n" +
                "Doom version = 19\n" +
                "Patch format = 6\n\n" +
                body.Replace("\r\n", "\n").TrimStart();

            File.WriteAllText(path, text);
            return path;
        }

        private static string CreateClassicTextPatch(string original, string replacement)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-parser-text-" + Guid.NewGuid().ToString("N") + ".deh");

            var text =
                "Patch File for DeHackEd v3.0\n" +
                "Doom version = 19\n" +
                "Patch format = 6\n\n" +
                "Text " + original.Length + " " + replacement.Length + "\n" +
                original + replacement + "\n";

            File.WriteAllText(path, text);
            return path;
        }
    }
}
