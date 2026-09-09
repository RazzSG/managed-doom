using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfSkyMapCompatibilityTest
{
    [TestMethod]
    public void VanillaAndBoomAlwaysKeepSkyFullbright()
    {
        Assert.AreEqual(0, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
            GameCompatibility.Vanilla, compSkyMap: false, ColorMap.Inverse));
        Assert.AreEqual(0, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
            GameCompatibility.Vanilla, compSkyMap: true, ColorMap.Inverse));

        Assert.AreEqual(0, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
            GameCompatibility.Boom, compSkyMap: false, ColorMap.Inverse));
        Assert.AreEqual(0, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
            GameCompatibility.Boom, compSkyMap: true, ColorMap.Inverse));
    }

    [TestMethod]
    public void MbfDefaultLetsFixedColorMapAffectSky()
    {
        Assert.AreEqual(ColorMap.Inverse, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
            GameCompatibility.Mbf, compSkyMap: false, ColorMap.Inverse));
        Assert.AreEqual(ColorMap.Inverse, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
            GameCompatibility.Mbf21, compSkyMap: false, ColorMap.Inverse));

        // Fixed colormaps are generic renderer state; infrared uses index 1.
        Assert.AreEqual(1, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
            GameCompatibility.Mbf, compSkyMap: false, fixedColorMap: 1));
    }

    [TestMethod]
    public void CompSkyMapRestoresDoomFullbrightSky()
    {
        Assert.AreEqual(0, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
            GameCompatibility.Mbf, compSkyMap: true, ColorMap.Inverse));
        Assert.AreEqual(0, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
            GameCompatibility.Mbf21, compSkyMap: true, ColorMap.Inverse));
    }

    [TestMethod]
    public void NoFixedColorMapAlwaysUsesNormalFullbrightMap()
    {
        foreach (var compatibility in new[]
                 {
                     GameCompatibility.Vanilla,
                     GameCompatibility.Boom,
                     GameCompatibility.Mbf,
                     GameCompatibility.Mbf21
                 })
        {
            Assert.AreEqual(0, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
                compatibility, compSkyMap: false, fixedColorMap: 0));
            Assert.AreEqual(0, MbfSkyMapCompatibility.ResolveSkyColorMapIndex(
                compatibility, compSkyMap: true, fixedColorMap: 0));
        }
    }
}
