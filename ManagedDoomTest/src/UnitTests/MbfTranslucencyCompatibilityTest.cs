using System;
using System.IO;
using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class MbfTranslucencyCompatibilityTest
{
    [TestMethod]
    public void CompatibilityBoundaryMatchesPrBoomAndMbf21Rules()
    {
        Assert.IsFalse(MbfTranslucencyCompatibility.UsesPredefinedTranslucency(
            GameCompatibility.Vanilla, compTranslucency: false));
        Assert.IsTrue(MbfTranslucencyCompatibility.UsesPredefinedTranslucency(
            GameCompatibility.Boom, compTranslucency: false));
        Assert.IsTrue(MbfTranslucencyCompatibility.UsesPredefinedTranslucency(
            GameCompatibility.Boom, compTranslucency: true));

        Assert.IsTrue(MbfTranslucencyCompatibility.UsesPredefinedTranslucency(
            GameCompatibility.Mbf, compTranslucency: false));
        Assert.IsFalse(MbfTranslucencyCompatibility.UsesPredefinedTranslucency(
            GameCompatibility.Mbf, compTranslucency: true));

        Assert.IsTrue(MbfTranslucencyCompatibility.UsesPredefinedTranslucency(
            GameCompatibility.Mbf21, compTranslucency: false));
        Assert.IsTrue(MbfTranslucencyCompatibility.UsesPredefinedTranslucency(
            GameCompatibility.Mbf21, compTranslucency: true));
    }

    [TestMethod]
    public void PredefinedListMatchesPrBoomSeventeenThings()
    {
        var expected = new[]
        {
            MobjType.Fire,
            MobjType.Smoke,
            MobjType.Fatshot,
            MobjType.Bruisershot,
            MobjType.Spawnfire,
            MobjType.Troopshot,
            MobjType.Headshot,
            MobjType.Plasma,
            MobjType.Bfg,
            MobjType.Arachplaz,
            MobjType.Puff,
            MobjType.Tfog,
            MobjType.Ifog,
            MobjType.Misc12,
            MobjType.Inv,
            MobjType.Ins,
            MobjType.Mega
        };

        CollectionAssert.AreEquivalent(
            expected,
            Enum.GetValues<MobjType>()
                .Where(MbfTranslucencyCompatibility.IsPredefinedTranslucentType)
                .ToArray());
        Assert.AreEqual(17, expected.Length);
    }

    [TestMethod]
    public void ExplicitDeHackEdBitsOverrideAlwaysWins()
    {
        var puffInfo = DoomInfo.MobjInfos[(int)MobjType.Puff];
        var oldTranslucent = puffInfo.Translucent;
        var oldOverride = puffInfo.HasDeHackEdBitsOverride;

        try
        {
            puffInfo.HasDeHackEdBitsOverride = true;
            puffInfo.Translucent = false;

            Assert.IsFalse(MbfTranslucencyCompatibility.ResolveActorTranslucency(
                GameCompatibility.Mbf,
                compTranslucency: false,
                MobjType.Puff,
                puffInfo));

            puffInfo.Translucent = true;

            Assert.IsTrue(MbfTranslucencyCompatibility.ResolveActorTranslucency(
                GameCompatibility.Vanilla,
                compTranslucency: true,
                MobjType.Puff,
                puffInfo));
            Assert.IsTrue(MbfTranslucencyCompatibility.ResolveActorTranslucency(
                GameCompatibility.Mbf,
                compTranslucency: true,
                MobjType.Puff,
                puffInfo));
            Assert.IsTrue(MbfTranslucencyCompatibility.ResolveActorTranslucency(
                GameCompatibility.Mbf21,
                compTranslucency: true,
                MobjType.Puff,
                puffInfo));
        }
        finally
        {
            puffInfo.Translucent = oldTranslucent;
            puffInfo.HasDeHackEdBitsOverride = oldOverride;
        }
    }

    [TestMethod]
    public void SpawnMobjConsumesRuntimeCompTranslucency()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var boomOptions = new GameOptions { Compatibility = GameCompatibility.Boom };
        boomOptions.MbfOptions.CompTranslucency = true;
        var boomWorld = new World(content, boomOptions, null);
        Assert.IsTrue(SpawnPuff(boomWorld).Translucent);

        var fixedOptions = new GameOptions { Compatibility = GameCompatibility.Mbf };
        var fixedWorld = new World(content, fixedOptions, null);
        Assert.IsTrue(SpawnPuff(fixedWorld).Translucent);
        Assert.IsFalse(SpawnBlood(fixedWorld).Translucent);

        var compatibilityOptions = new GameOptions { Compatibility = GameCompatibility.Mbf };
        compatibilityOptions.MbfOptions.CompTranslucency = true;
        var compatibilityWorld = new World(content, compatibilityOptions, null);
        Assert.IsFalse(SpawnPuff(compatibilityWorld).Translucent);

        var mbf21Options = new GameOptions { Compatibility = GameCompatibility.Mbf21 };
        mbf21Options.MbfOptions.CompTranslucency = true;
        var mbf21World = new World(content, mbf21Options, null);
        Assert.IsTrue(SpawnPuff(mbf21World).Translucent);
    }

    [TestMethod]
    public void DeHackEdCanonicalTranslucentBitIsSeparatedFromInternalFriendAndResets()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 38 (Bullet Puff)
Bits = 2147483648
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            var info = DoomInfo.MobjInfos[(int)MobjType.Puff];
            Assert.IsTrue(info.Translucent);
            Assert.IsTrue(info.HasDeHackEdBitsOverride);
            Assert.AreEqual(0, (int)info.Flags);

            DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);

            Assert.IsFalse(info.Translucent);
            Assert.IsFalse(info.HasDeHackEdBitsOverride);
        }
        finally
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
            File.Delete(patch);
        }
    }

    [TestMethod]
    public void DeHackEdFriendAndTranslucentBitsRemainIndependent()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 38 (Bullet Puff)
Bits = 3221225472
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            var info = DoomInfo.MobjInfos[(int)MobjType.Puff];
            Assert.IsTrue((info.Flags & MobjFlags.Friend) != 0);
            Assert.IsTrue(info.Translucent);
            Assert.IsTrue(info.HasDeHackEdBitsOverride);
        }
        finally
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
            File.Delete(patch);
        }
    }

    [TestMethod]
    public void BexThingBitMnemonicsApplyMbfFlagsAndBoomTranslucency()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 38 (Bullet Puff)
Bits = SOLID+SHOOTABLE+TOUCHY+BOUNCES+FRIEND+TRANSLUCENT
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            var info = DoomInfo.MobjInfos[(int)MobjType.Puff];
            Assert.AreEqual(
                MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.Touchy | MobjFlags.Bounces | MobjFlags.Friend,
                info.Flags);
            Assert.IsTrue(info.Translucent);
            Assert.IsTrue(info.HasDeHackEdBitsOverride);
        }
        finally
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
            File.Delete(patch);
        }
    }

    [TestMethod]
    public void SignedCanonicalTranslucentBitIsAccepted()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 38 (Bullet Puff)
Bits = -2147483648
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            var info = DoomInfo.MobjInfos[(int)MobjType.Puff];
            Assert.AreEqual(0, (int)info.Flags);
            Assert.IsTrue(info.Translucent);
            Assert.IsTrue(info.HasDeHackEdBitsOverride);
        }
        finally
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
            File.Delete(patch);
        }
    }

    [TestMethod]
    public void InvalidDeHackEdBitsDoesNotCreateAnOverride()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 38 (Bullet Puff)
Bits = not-a-number
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            var info = DoomInfo.MobjInfos[(int)MobjType.Puff];
            Assert.IsFalse(info.HasDeHackEdBitsOverride);
            Assert.IsFalse(info.Translucent);
            Assert.IsTrue(MbfTranslucencyCompatibility.ResolveActorTranslucency(
                GameCompatibility.Mbf,
                compTranslucency: false,
                MobjType.Puff,
                info));
        }
        finally
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
            File.Delete(patch);
        }
    }

    private static Mobj SpawnPuff(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(player.X, player.Y, Mobj.OnFloorZ, MobjType.Puff);
    }

    private static Mobj SpawnBlood(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(player.X, player.Y, Mobj.OnFloorZ, MobjType.Blood);
    }

    private static CommandLineArgs ArgsForPatch(string patchPath)
    {
        return new CommandLineArgs(new[] { "-deh", patchPath, "-nodeh" });
    }

    private static string CreatePatch(string text)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "manageddoom-mbf-translucency-" + Guid.NewGuid().ToString("N") + ".deh");
        File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
        return path;
    }
}
