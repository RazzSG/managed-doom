using System;
using System.IO;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class DeHackEdMaxHealthOverrideTest
{
    [TestMethod]
    public void ExplicitMaxHealthIsTrackedEvenWhenItEqualsTheLegacyDefault()
    {
        using var wad = new Wad(WadPath.Doom2);
        Reset(wad);

        var patch = CreatePatch("Misc 0\nMax Health = 200\n");

        try
        {
            Assert.IsFalse(DoomInfo.DeHackEdConst.HasMaxHealthOverride);

            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            Assert.AreEqual(200, DoomInfo.DeHackEdConst.MaxHealth);
            Assert.IsTrue(DoomInfo.DeHackEdConst.HasMaxHealthOverride);

            Reset(wad);

            Assert.AreEqual(200, DoomInfo.DeHackEdConst.MaxHealth);
            Assert.IsFalse(DoomInfo.DeHackEdConst.HasMaxHealthOverride);
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
            "manageddoom-deh-maxhealth-" + Guid.NewGuid().ToString("N") + ".deh");

        var text =
            "Patch File for DeHackEd v3.0\n" +
            "Doom version = 19\n" +
            "Patch format = 6\n\n" +
            body;

        File.WriteAllText(path, text);
        return path;
    }
}
