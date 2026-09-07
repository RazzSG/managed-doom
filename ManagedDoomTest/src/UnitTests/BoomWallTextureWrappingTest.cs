using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomWallTextureWrappingTest
{
    [TestMethod]
    public void VanillaKeepsClassic128RowWallWrap()
    {
        Assert.AreEqual(
            128,
            BoomWallTextureWrapping.ResolveWrapHeight(16, GameCompatibility.Vanilla));
    }

    [TestMethod]
    public void BoomUsesActualHeightForShortWallTextures()
    {
        Assert.AreEqual(
            16,
            BoomWallTextureWrapping.ResolveWrapHeight(16, GameCompatibility.Boom));
    }

    [TestMethod]
    public void BoomSupportsArbitraryNonPowerOfTwoWallTextureHeights()
    {
        var height = BoomWallTextureWrapping.ResolveWrapHeight(72, GameCompatibility.Boom);

        Assert.AreEqual(72, height);
        Assert.AreEqual(0, BoomWallTextureWrapping.WrapNonPowerOfTwoRow(72, height));
        Assert.AreEqual(1, BoomWallTextureWrapping.WrapNonPowerOfTwoRow(73, height));
        Assert.AreEqual(71, BoomWallTextureWrapping.WrapNonPowerOfTwoRow(143, height));
        Assert.AreEqual(0, BoomWallTextureWrapping.WrapNonPowerOfTwoRow(144, height));
        Assert.AreEqual(71, BoomWallTextureWrapping.WrapNonPowerOfTwoRow(-1, height));
    }

    [TestMethod]
    public void LaterCompatibilityLevelsInheritBoomWallWrapping()
    {
        Assert.AreEqual(
            16,
            BoomWallTextureWrapping.ResolveWrapHeight(16, GameCompatibility.Mbf));
        Assert.AreEqual(
            16,
            BoomWallTextureWrapping.ResolveWrapHeight(16, GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void Classic128HighTexturesAreUnchanged()
    {
        foreach (var compatibility in new[]
                 {
                     GameCompatibility.Vanilla,
                     GameCompatibility.Boom,
                     GameCompatibility.Mbf,
                     GameCompatibility.Mbf21
                 })
        {
            Assert.AreEqual(
                128,
                BoomWallTextureWrapping.ResolveWrapHeight(128, compatibility));
        }
    }
}
