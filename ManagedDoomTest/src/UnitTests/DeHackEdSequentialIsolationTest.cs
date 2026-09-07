using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdSequentialIsolationTest
    {
        [TestMethod]
        public void SecondPatchedWadSetStartsFromPristineDefinitions()
        {
            var firstPwad = WriteWad(("DEHACKED", PatchBytes(@"
Thing 2 (Former Human)
Hit points = 1234

Frame 10
Duration = 9

Misc 0
Initial Health = 77

[STRINGS]
PRESSKEY = first wad

[PARS]
par 1 111

Text 6 7
F_SKY1SLIME16
")));

            var secondPwad = WriteWad(("DEHACKED", PatchBytes(@"
Thing 2 (Former Human)
Hit points = 2222

[PARS]
par 2 222
")));

            try
            {
                using (var firstWad = new Wad(WadPath.Doom2, firstPwad))
                {
                    DeHackEd.Initialize(new CommandLineArgs(Array.Empty<string>()), firstWad);

                    Assert.AreEqual(1234, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                    Assert.AreEqual(9, DoomInfo.States[10].Tics);
                    Assert.AreEqual(77, DoomInfo.DeHackEdConst.InitialHealth);
                    Assert.AreEqual("first wad", DoomInfo.Strings.PRESSKEY.ToString());
                    Assert.AreEqual(111, DoomInfo.ParTimes.Doom2[0]);
                    Assert.AreEqual("SLIME16", DoomString.Resolve("F_SKY1"));
                }

                using (var secondWad = new Wad(WadPath.Doom2, secondPwad))
                {
                    DeHackEd.Initialize(new CommandLineArgs(Array.Empty<string>()), secondWad);

                    Assert.AreEqual(2222, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                    Assert.AreEqual(1, DoomInfo.States[10].Tics,
                        "Frame data from the previous WAD set leaked into the next load.");
                    Assert.AreEqual(100, DoomInfo.DeHackEdConst.InitialHealth,
                        "Misc data from the previous WAD set leaked into the next load.");
                    Assert.AreEqual("press a key.", DoomInfo.Strings.PRESSKEY.ToString(),
                        "BEX string data from the previous WAD set leaked into the next load.");
                    Assert.AreEqual(30, DoomInfo.ParTimes.Doom2[0],
                        "Par time from the previous WAD set leaked into the next load.");
                    Assert.AreEqual(222, DoomInfo.ParTimes.Doom2[1]);
                    Assert.AreEqual("F_SKY1", DoomString.Resolve("F_SKY1"),
                        "Hardcoded Text replacement from the previous WAD set leaked into the next load.");
                }
            }
            finally
            {
                ResetGlobalState();
                File.Delete(firstPwad);
                File.Delete(secondPwad);
            }
        }

        [TestMethod]
        public void SwitchingFromPatchedWadToCleanWadRestoresPristineDefinitions()
        {
            var pwad = WriteWad(("DEHACKED", PatchBytes(@"
Thing 2 (Former Human)
Hit points = 777

Frame 10
Duration = 8

Misc 0
Initial Health = 66

[STRINGS]
PRESSKEY = temporary value

[PARS]
par 1 444

Text 4 4
TROOPOSS
")));

            try
            {
                using (var patchedWad = new Wad(WadPath.Doom2, pwad))
                {
                    DeHackEd.Initialize(new CommandLineArgs(Array.Empty<string>()), patchedWad);

                    Assert.AreEqual(777, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                    Assert.AreEqual(8, DoomInfo.States[10].Tics);
                    Assert.AreEqual(66, DoomInfo.DeHackEdConst.InitialHealth);
                    Assert.AreEqual("temporary value", DoomInfo.Strings.PRESSKEY.ToString());
                    Assert.AreEqual(444, DoomInfo.ParTimes.Doom2[0]);
                    Assert.AreEqual("POSS", DoomInfo.SpriteNames[0].ToString());
                }

                using (var cleanWad = new Wad(WadPath.Doom2))
                {
                    DeHackEd.Initialize(new CommandLineArgs(Array.Empty<string>()), cleanWad);

                    Assert.AreEqual(20, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                    Assert.AreEqual(1, DoomInfo.States[10].Tics);
                    Assert.AreEqual(100, DoomInfo.DeHackEdConst.InitialHealth);
                    Assert.AreEqual("press a key.", DoomInfo.Strings.PRESSKEY.ToString());
                    Assert.AreEqual(30, DoomInfo.ParTimes.Doom2[0]);
                    Assert.AreEqual("TROO", DoomInfo.SpriteNames[0].ToString());
                }
            }
            finally
            {
                ResetGlobalState();
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void FailedSecondWadSetRollsBackToPristineInsteadOfPreviousWadState()
        {
            var firstPwad = WriteWad(("DEHACKED", PatchBytes(@"
Thing 2 (Former Human)
Hit points = 999

Misc 0
Initial Health = 55

[STRINGS]
PRESSKEY = previous wad

[PARS]
par 1 333
")));

            var failingPwad = WriteWad(("DEHACKED", PatchBytes(@"
Thing 2 (Former Human)
Hit points = 888

Text 12 20
press a key.too short
")));

            try
            {
                using (var firstWad = new Wad(WadPath.Doom2, firstPwad))
                {
                    DeHackEd.Initialize(new CommandLineArgs(Array.Empty<string>()), firstWad);
                    Assert.AreEqual(999, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                    Assert.AreEqual(55, DoomInfo.DeHackEdConst.InitialHealth);
                    Assert.AreEqual("previous wad", DoomInfo.Strings.PRESSKEY.ToString());
                    Assert.AreEqual(333, DoomInfo.ParTimes.Doom2[0]);
                }

                using (var failingWad = new Wad(WadPath.Doom2, failingPwad))
                {
                    Assert.ThrowsExactly<Exception>(() =>
                        DeHackEd.Initialize(new CommandLineArgs(Array.Empty<string>()), failingWad));
                }

                Assert.AreEqual(20, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth,
                    "A failed new WAD set restored the previous patched Thing state instead of pristine definitions.");
                Assert.AreEqual(100, DoomInfo.DeHackEdConst.InitialHealth);
                Assert.AreEqual("press a key.", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(30, DoomInfo.ParTimes.Doom2[0]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(firstPwad);
                File.Delete(failingPwad);
            }
        }

        [TestMethod]
        public void NodehOnLaterLoadClearsEarlierEmbeddedDefinitionState()
        {
            var pwad = WriteWad(("DEHACKED", PatchBytes(@"
Thing 2 (Former Human)
Hit points = 654

Frame 10
Duration = 7

Misc 0
Initial Health = 88

[STRINGS]
PRESSKEY = embedded value

[PARS]
par 1 555
")));

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);

                DeHackEd.Initialize(new CommandLineArgs(Array.Empty<string>()), wad);
                Assert.AreEqual(654, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                Assert.AreEqual(7, DoomInfo.States[10].Tics);
                Assert.AreEqual(88, DoomInfo.DeHackEdConst.InitialHealth);
                Assert.AreEqual("embedded value", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(555, DoomInfo.ParTimes.Doom2[0]);

                DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);

                Assert.AreEqual(20, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                Assert.AreEqual(1, DoomInfo.States[10].Tics);
                Assert.AreEqual(100, DoomInfo.DeHackEdConst.InitialHealth);
                Assert.AreEqual("press a key.", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(30, DoomInfo.ParTimes.Doom2[0]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(pwad);
            }
        }

        private static byte[] PatchBytes(string body)
        {
            return Encoding.ASCII.GetBytes(
                "Patch File for DeHackEd v3.0\n" +
                "Doom version = 19\n" +
                "Patch format = 6\n\n" +
                Normalize(body));
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
                "manageddoom-deh-isolation-" + Guid.NewGuid().ToString("N") + ".wad");

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

            return path;
        }
    }
}
