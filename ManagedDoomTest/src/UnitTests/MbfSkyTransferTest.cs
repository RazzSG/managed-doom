using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfSkyTransferTest
{
    private const short TestTag = 31956;

    [TestMethod]
    public void BoomProfileAppliesTransferAndVanillaClearsIt()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Boom };
        var world = new World(content, options, null);
        var line = FindSourceLine(world);
        var target = FindTargetSector(world, line.FrontSector);

        PrepareTransfer(world, line, target, 271, TestTag);

        world.Specials.SpawnSpecials();

        Assert.AreSame(line, target.SkyTransferLine);

        options.Compatibility = GameCompatibility.Vanilla;
        world.Specials.SpawnSpecials();

        Assert.IsNull(target.SkyTransferLine);
    }

    [TestMethod]
    public void LaterTransferLineWinsForSameTaggedSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var first = FindSourceLine(world);
        var second = world.Map.Lines.First(line =>
            line != first && line.FrontSide != null && line.FrontSector != null);
        var target = FindTargetSector(world, first.FrontSector, second.FrontSector);

        ResetSkyTransfers(world);
        first.Special = (LineSpecial)271;
        first.Tag = TestTag;
        second.Special = (LineSpecial)272;
        second.Tag = TestTag;
        target.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        MbfSkyTransferResolver.Apply(world);

        Assert.AreSame(second, target.SkyTransferLine);
    }

    [TestMethod]
    public void ZeroTagTargetsZeroTaggedSectorsOnly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindSourceLine(world);
        var zeroTarget = FindTargetSector(world, line.FrontSector);
        var nonZeroTarget = world.Map.Sectors.First(sector =>
            sector != line.FrontSector && sector != zeroTarget);

        ResetSkyTransfers(world);
        line.Special = (LineSpecial)271;
        line.Tag = 0;
        zeroTarget.Tag = 0;
        nonZeroTarget.Tag = TestTag;
        world.Map.BoomTags.Rebuild();

        MbfSkyTransferResolver.Apply(world);

        Assert.AreSame(line, zeroTarget.SkyTransferLine);
        Assert.IsNull(nonZeroTarget.SkyTransferLine);
    }

    [TestMethod]
    public void RenderStateTracksLiveSideOffsetsAndAnimatedTextureTranslation()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var mapLine = FindSourceLine(world);
        var target = FindTargetSector(world, mapLine.FrontSector);
        var sourceTexture = FindUsableTextureNumber(world, 1);
        var translatedTexture = FindUsableTextureNumber(world, sourceTexture + 1);
        var translation = world.Specials.TextureTranslation;

        // Keep this test independent from MAP01's own linedef specials/scrollers.
        // The integration tests cover real map lines; here we only verify that
        // render state is resolved from the current defining sidedef values.
        var sourceSide = new SideDef(
            Fixed.FromInt(3),
            Fixed.FromInt(40),
            sourceTexture,
            0,
            0,
            mapLine.FrontSector);
        var line = new LineDef(
            mapLine.Vertex1,
            mapLine.Vertex2,
            mapLine.Flags,
            (LineSpecial)271,
            0,
            sourceSide,
            null);

        target.SkyTransferLine = line;
        translation[sourceTexture] = translatedTexture;

        Assert.IsTrue(MbfSkyTransferResolver.TryResolveRenderState(
            target,
            translation,
            world.Map.Textures.Count,
            out var state));

        Assert.AreEqual(translatedTexture, state.TextureNumber);
        Assert.AreEqual(unchecked((uint)Fixed.FromInt(3).Data), state.AngleOffset.Data);
        Assert.AreEqual(Fixed.FromInt(12).Data, state.TextureAlt.Data);
        Assert.IsTrue(state.InvertAngleBits);

        sourceSide.TextureOffset = Fixed.FromInt(9);
        sourceSide.RowOffset = Fixed.FromInt(31);
        translation[sourceTexture] = sourceTexture;

        Assert.IsTrue(MbfSkyTransferResolver.TryResolveRenderState(
            target,
            translation,
            world.Map.Textures.Count,
            out state));

        Assert.AreEqual(sourceTexture, state.TextureNumber);
        Assert.AreEqual(unchecked((uint)Fixed.FromInt(9).Data), state.AngleOffset.Data);
        Assert.AreEqual(Fixed.FromInt(3).Data, state.TextureAlt.Data);
    }

    [TestMethod]
    public void Special272UsesOppositeHorizontalFlipFrom271()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindSourceLine(world);
        var target = FindTargetSector(world, line.FrontSector);
        var texture = FindUsableTextureNumber(world, 1);
        var translation = Enumerable.Range(0, world.Map.Textures.Count).ToArray();

        line.FrontSide.TopTexture = texture;
        line.FrontSide.TextureOffset = Fixed.Zero;
        line.FrontSide.RowOffset = Fixed.Zero;
        target.SkyTransferLine = line;

        line.Special = (LineSpecial)271;
        Assert.IsTrue(MbfSkyTransferResolver.TryResolveRenderState(
            target, translation, world.Map.Textures.Count, out var regular));
        var regularAngle = MbfSkyTransferResolver.ResolveSkyAngle(Angle.Ang0, regular);

        line.Special = (LineSpecial)272;
        Assert.IsTrue(MbfSkyTransferResolver.TryResolveRenderState(
            target, translation, world.Map.Textures.Count, out var flipped));
        var flippedAngle = MbfSkyTransferResolver.ResolveSkyAngle(Angle.Ang0, flipped);

        Assert.AreEqual(uint.MaxValue, regularAngle.Data);
        Assert.AreEqual(0u, flippedAngle.Data);
        Assert.IsTrue(regular.InvertAngleBits);
        Assert.IsFalse(flipped.InvertAngleBits);
    }

    [TestMethod]
    public void MissingUpperTextureFallsBackToNormalLevelSky()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = FindSourceLine(world);
        var target = FindTargetSector(world, line.FrontSector);
        var translation = Enumerable.Range(0, world.Map.Textures.Count).ToArray();

        line.Special = (LineSpecial)271;
        line.FrontSide.TopTexture = 0;
        target.SkyTransferLine = line;

        Assert.IsFalse(MbfSkyTransferResolver.TryResolveRenderState(
            target,
            translation,
            world.Map.Textures.Count,
            out _));
    }

    private static void PrepareTransfer(
        World world,
        LineDef line,
        Sector target,
        int special,
        short tag)
    {
        ResetSkyTransfers(world);
        line.Special = (LineSpecial)special;
        line.Tag = tag;
        target.Tag = tag;
        world.Map.BoomTags.Rebuild();
    }

    private static void ResetSkyTransfers(World world)
    {
        foreach (var sector in world.Map.Sectors)
        {
            sector.SkyTransferLine = null;
            sector.Tag = 0;
        }

        foreach (var line in world.Map.Lines)
        {
            if ((int)line.Special is 271 or 272)
                line.Special = 0;
        }
    }

    private static LineDef FindSourceLine(World world) =>
        world.Map.Lines.First(line => line.FrontSide != null && line.FrontSector != null);

    private static Sector FindTargetSector(World world, params Sector[] excluded) =>
        world.Map.Sectors.First(sector => !excluded.Contains(sector));

    private static int FindUsableTextureNumber(World world, int start)
    {
        for (var i = System.Math.Max(start, 1); i < world.Map.Textures.Count; i++)
        {
            if (world.Map.Textures[i].Width > 0)
                return i;
        }

        Assert.Fail("The dummy DOOM2 texture set did not contain a usable transferred-sky texture.");
        return 1;
    }
}
