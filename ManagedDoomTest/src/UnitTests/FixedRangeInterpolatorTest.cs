using ManagedDoom;
using ManagedDoom.Video;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class FixedRangeInterpolatorTest
{
    [TestMethod]
    public void DistributesNegativeSubLsbSlopeAcrossWideWall()
    {
        // Runtime reproduction from the jumping 90-degree wall corner:
        // scale 25491 -> 24857 across 845 screen-space steps. A single Fixed
        // step truncates to zero, while the interpolator must still reach scale2.
        var interpolator = new FixedRangeInterpolator(
            new Fixed(25491),
            new Fixed(24857),
            845);

        Assert.AreEqual(25491, interpolator.Value.Data);

        var previous = interpolator.Value.Data;
        for (var i = 0; i < 845; i++)
        {
            interpolator.Advance();
            Assert.IsTrue(interpolator.Value.Data <= previous);
            previous = interpolator.Value.Data;
        }

        Assert.AreEqual(24857, interpolator.Value.Data);
    }

    [TestMethod]
    public void DistributesPositiveSubLsbSlopeAndReachesEndpoint()
    {
        var interpolator = new FixedRangeInterpolator(
            new Fixed(19958),
            new Fixed(27189),
            1001);

        for (var i = 0; i < 1001; i++)
            interpolator.Advance();

        Assert.AreEqual(27189, interpolator.Value.Data);
    }

    [TestMethod]
    public void OffsetStartsAtSameValueAsSequentialAdvance()
    {
        const int start = 24234;
        const int end = 70889;
        const int steps = 880;
        const int offset = 317;

        var sequential = new FixedRangeInterpolator(
            new Fixed(start),
            new Fixed(end),
            steps);

        for (var i = 0; i < offset; i++)
            sequential.Advance();

        var offsetInterpolator = new FixedRangeInterpolator(
            new Fixed(start),
            new Fixed(end),
            steps,
            offset);

        Assert.AreEqual(sequential.Value.Data, offsetInterpolator.Value.Data);

        for (var i = offset; i < steps; i++)
        {
            sequential.Advance();
            offsetInterpolator.Advance();
            Assert.AreEqual(sequential.Value.Data, offsetInterpolator.Value.Data);
        }
    }

    [TestMethod]
    public void ZeroLengthRangeKeepsStartValue()
    {
        var interpolator = new FixedRangeInterpolator(
            new Fixed(12345),
            new Fixed(54321),
            0);

        interpolator.Advance();

        Assert.AreEqual(12345, interpolator.Value.Data);
    }
}
