using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdParserSyntaxTest
    {
        [TestMethod]
        public void ClassicParserAcceptsCaseInsensitiveBlocksWhitespaceAndIndentedComments()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

    # Leading whitespace before a comment is legal.
    tHiNg	2 (Former Human)
    hIt PoInTs	 = 1234 # atoi-style trailing text is ignored
    rEaCtIoN TiMe = -7 trailing text

	fRaMe    10
    dUrAtIoN = 9

    aMmO	0
    mAx AmMo = 321
    pEr AmMo = 17
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var info = DoomInfo.MobjInfos[(int)MobjType.Possessed];
                Assert.AreEqual(1234, info.SpawnHealth);
                Assert.AreEqual(-7, info.ReactionTime);
                Assert.AreEqual(9, DoomInfo.States[10].Tics);
                Assert.AreEqual(321, DoomInfo.AmmoInfos.Max[0]);
                Assert.AreEqual(17, DoomInfo.AmmoInfos.Clip[0]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void BexSectionsAndMnemonicsAreCaseInsensitiveAndAcceptTabs()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

    [strings]
    presskey = parser works

	[pars]
	PAR	1	111 # inline comments after par values are tolerated

    [codeptr]
	frame	174	=	chase
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("parser works", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(111, DoomInfo.ParTimes.Doom2[0]);
                Assert.IsNotNull(DoomInfo.States[174].MobjAction);
                Assert.AreEqual("Chase", DoomInfo.States[174].MobjAction.Method.Name);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicTextPayloadDoesNotTreatHashPrefixedLinesAsComments()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Text 12 12
press a key.Thing 2
#abc
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual("Thing 2\n#abc", DoomInfo.Strings.PRESSKEY.ToString());
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void BexStringsAcceptEmptyValuesAndContinuationLines()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[STRINGS]
PRESSKEY = first \
    second
PRESSYN =
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("first second", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(string.Empty, DoomInfo.Strings.PRESSYN.ToString());
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void IncompleteClassicTextBlockReportsItsHeaderAndRollsBack()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 2
Hit points = 1234

Text 12 20
press a key.too short
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                var exception = Assert.ThrowsExactly<Exception>(() => DeHackEd.Initialize(ArgsForPatch(patch), wad));
                var details = exception.ToString();

                StringAssert.Contains(details, "Failed to process block: Text (line");
                StringAssert.Contains(details, "Unexpected end of DeHackEd Text block");
                Assert.AreEqual(20, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                Assert.AreEqual("press a key.", DoomInfo.Strings.PRESSKEY.ToString());
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

        private static string CreatePatch(string text)
        {
            var path = Path.Combine(Path.GetTempPath(), "manageddoom-deh-parser-" + Guid.NewGuid().ToString("N") + ".deh");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
