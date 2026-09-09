using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfMaskedAnimationCompatibilityTest
{
    [TestMethod]
    public void CompatibilityBoundaryMatchesPrBoomAndMbf21Rules()
    {
        Assert.IsTrue(MbfMaskedAnimationCompatibility.AnimatesTwoSidedMiddleTextures(
            GameCompatibility.Vanilla, true));
        Assert.IsTrue(MbfMaskedAnimationCompatibility.AnimatesTwoSidedMiddleTextures(
            GameCompatibility.Boom, true));

        Assert.IsTrue(MbfMaskedAnimationCompatibility.AnimatesTwoSidedMiddleTextures(
            GameCompatibility.Mbf, false));
        Assert.IsFalse(MbfMaskedAnimationCompatibility.AnimatesTwoSidedMiddleTextures(
            GameCompatibility.Mbf, true));

        Assert.IsTrue(MbfMaskedAnimationCompatibility.AnimatesTwoSidedMiddleTextures(
            GameCompatibility.Mbf21, false));
        Assert.IsTrue(MbfMaskedAnimationCompatibility.AnimatesTwoSidedMiddleTextures(
            GameCompatibility.Mbf21, true));
    }

    [TestMethod]
    public void ResolverUsesTranslatedTextureOnlyWhenMaskedAnimationIsEnabled()
    {
        var translation = new[] { 0, 4, 5, 6, 1, 2, 3 };

        Assert.AreEqual(4, MbfMaskedAnimationCompatibility.ResolveTwoSidedMiddleTexture(
            GameCompatibility.Vanilla, true, 1, translation));
        Assert.AreEqual(4, MbfMaskedAnimationCompatibility.ResolveTwoSidedMiddleTexture(
            GameCompatibility.Boom, true, 1, translation));
        Assert.AreEqual(4, MbfMaskedAnimationCompatibility.ResolveTwoSidedMiddleTexture(
            GameCompatibility.Mbf, false, 1, translation));
        Assert.AreEqual(1, MbfMaskedAnimationCompatibility.ResolveTwoSidedMiddleTexture(
            GameCompatibility.Mbf, true, 1, translation));
        Assert.AreEqual(4, MbfMaskedAnimationCompatibility.ResolveTwoSidedMiddleTexture(
            GameCompatibility.Mbf21, true, 1, translation));
    }
}
