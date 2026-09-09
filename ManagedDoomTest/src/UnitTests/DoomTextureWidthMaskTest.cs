using ManagedDoom.Video;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class DoomTextureWidthMaskTest
{
    [TestMethod]
    public void PowerOfTwoWidthsKeepClassicWidthMinusOneMask()
    {
        Assert.AreEqual(0, DoomTextureWidthMask.Resolve(1));
        Assert.AreEqual(31, DoomTextureWidthMask.Resolve(32));
        Assert.AreEqual(63, DoomTextureWidthMask.Resolve(64));
        Assert.AreEqual(127, DoomTextureWidthMask.Resolve(128));
    }

    [TestMethod]
    public void NonPowerOfTwoWidthsUseLargestLowerPowerOfTwoMask()
    {
        Assert.AreEqual(31, DoomTextureWidthMask.Resolve(36));
        Assert.AreEqual(63, DoomTextureWidthMask.Resolve(72));
        Assert.AreEqual(127, DoomTextureWidthMask.Resolve(255));
    }

    [TestMethod]
    public void DaifaWidthMatchesPrBoomMask()
    {
        const int width = 36;
        var mask = DoomTextureWidthMask.Resolve(width);

        Assert.AreEqual(31, mask);

        for (var column = 0; column < 32; column++)
            Assert.AreEqual(column, column & mask);

        Assert.AreEqual(0, 32 & mask);
        Assert.AreEqual(1, 33 & mask);
        Assert.AreEqual(2, 34 & mask);
        Assert.AreEqual(3, 35 & mask);
    }
}
