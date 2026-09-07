using System;
using System.IO;
using System.Reflection;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdBexParsBlockTest
    {
        [TestMethod]
        public void Doom2ParIsConsumedByDoomGameAtRuntime()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "[PARS]\n" +
                "par 1 123\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(123, DoomInfo.ParTimes.Doom2[0]);

                var options = new GameOptions
                {
                    GameMode = GameMode.Commercial,
                    Map = 1
                };

                Assert.AreEqual(123 * 35, GetRuntimeParTime(options));
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void Doom1EpisodeParIsConsumedByDoomGameAtRuntime()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "[PARS]\n" +
                "par 2 3 222\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(222, DoomInfo.ParTimes.Doom1[1][2]);

                var options = new GameOptions
                {
                    GameMode = GameMode.Registered,
                    Episode = 2,
                    Map = 3
                };

                Assert.AreEqual(222 * 35, GetRuntimeParTime(options));
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ZeroSecondParIsValid()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "[PARS]\n" +
                "par 1 0\n" +
                "par 1 1 0\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(0, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(0, DoomInfo.ParTimes.Doom1[0][0]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void InvalidParIndicesAreIgnoredAndLaterValidEntriesStillApply()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "[PARS]\n" +
                "par 0 111\n" +
                "par -1 112\n" +
                "par 33 113\n" +
                "par 0 1 211\n" +
                "par 1 0 212\n" +
                "par 5 1 213\n" +
                "par 1 10 214\n" +
                "par 2 345\n" +
                "par 3 4 456\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(345, DoomInfo.ParTimes.Doom2[1]);
                Assert.AreEqual(456, DoomInfo.ParTimes.Doom1[2][3]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void MalformedParLinesAreIgnoredAndLastValidAssignmentWins()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "[PARS]\n" +
                "par nope 100\n" +
                "par 4 nope\n" +
                "notpar 4 111\n" +
                "par 4 222 # first valid value\n" +
                "par 4 333 # later BEX assignment wins\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(333, DoomInfo.ParTimes.Doom2[3]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ParValuesAreResetBetweenContentLoads()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "[PARS]\n" +
                "par 1 999\n" +
                "par 1 1 888\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual(999, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(888, DoomInfo.ParTimes.Doom1[0][0]);

                Reset(wad);

                Assert.AreEqual(30, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual(30, DoomInfo.ParTimes.Doom1[0][0]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        private static int GetRuntimeParTime(GameOptions options)
        {
            var method = typeof(DoomGame).GetMethod(
                "GetParTime",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(method, "DoomGame.GetParTime runtime consumer was not found.");
            return (int)method.Invoke(null, new object[] { options });
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
                "manageddoom-bex-pars-" + Guid.NewGuid().ToString("N") + ".bex");

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
