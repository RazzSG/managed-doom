using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomStatusBarKeysTest
{
    [TestMethod]
    public void VanillaKeepsTraditionalSingleKeyDisplay()
    {
        Assert.AreEqual(3, BoomStatusBarKeys.ResolvePatchIndex(0, true, true, GameCompatibility.Vanilla));
        Assert.AreEqual(4, BoomStatusBarKeys.ResolvePatchIndex(1, true, true, GameCompatibility.Vanilla));
        Assert.AreEqual(5, BoomStatusBarKeys.ResolvePatchIndex(2, true, true, GameCompatibility.Vanilla));
    }

    [TestMethod]
    public void BoomUsesCombinedPatchesWhenBothKeysAreOwned()
    {
        Assert.AreEqual(6, BoomStatusBarKeys.ResolvePatchIndex(0, true, true, GameCompatibility.Boom));
        Assert.AreEqual(7, BoomStatusBarKeys.ResolvePatchIndex(1, true, true, GameCompatibility.Boom));
        Assert.AreEqual(8, BoomStatusBarKeys.ResolvePatchIndex(2, true, true, GameCompatibility.Boom));
    }

    [TestMethod]
    public void BoomStillUsesClassicPatchesForSingleKeys()
    {
        Assert.AreEqual(0, BoomStatusBarKeys.ResolvePatchIndex(0, true, false, GameCompatibility.Boom));
        Assert.AreEqual(4, BoomStatusBarKeys.ResolvePatchIndex(1, false, true, GameCompatibility.Boom));
        Assert.AreEqual(-1, BoomStatusBarKeys.ResolvePatchIndex(2, false, false, GameCompatibility.Boom));
    }

    [TestMethod]
    public void LaterCompatibilityLevelsInheritBoomCombinedKeys()
    {
        Assert.AreEqual(6, BoomStatusBarKeys.ResolvePatchIndex(0, true, true, GameCompatibility.Mbf));
        Assert.AreEqual(6, BoomStatusBarKeys.ResolvePatchIndex(0, true, true, GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void EmbeddedBoomCombinedKeyPatchesArePresentAndValid()
    {
        for (var i = BoomStatusBarKeys.CombinedPatchBase; i < BoomStatusBarKeys.PatchCount; i++)
        {
            var patch = BoomStatusBarKeyResources.LoadPatch(i);

            Assert.AreEqual("STKEYS" + i, patch.Name);
            Assert.AreEqual(7, patch.Width);
            Assert.AreEqual(7, patch.Height);
            Assert.AreEqual(0, patch.LeftOffset);
            Assert.AreEqual(0, patch.TopOffset);
        }
    }
}
