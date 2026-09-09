using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfMoveBlockCompatibilityTest
{
    [TestMethod]
    public void SelectorMatchesVanillaBoomMbfAndMbf21Boundaries()
    {
        var halfMaxMove = Fixed.FromInt(15);
        var largeNegative = Fixed.FromInt(-16);

        Assert.IsFalse(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            largeNegative, Fixed.Zero, halfMaxMove,
            GameCompatibility.Vanilla, compMoveBlock: false));

        Assert.IsTrue(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            largeNegative, Fixed.Zero, halfMaxMove,
            GameCompatibility.Boom, compMoveBlock: true));

        Assert.IsTrue(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            largeNegative, Fixed.Zero, halfMaxMove,
            GameCompatibility.Mbf, compMoveBlock: false));

        Assert.IsFalse(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            largeNegative, Fixed.Zero, halfMaxMove,
            GameCompatibility.Mbf, compMoveBlock: true));

        Assert.IsTrue(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            largeNegative, Fixed.Zero, halfMaxMove,
            GameCompatibility.Mbf21, compMoveBlock: false));

        // MBF21 deoptionalizes this fix: an OPTIONS value of 1 cannot restore
        // the old negative-movement bug at MBF21 compatibility.
        Assert.IsTrue(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            largeNegative, Fixed.Zero, halfMaxMove,
            GameCompatibility.Mbf21, compMoveBlock: true));
    }

    [TestMethod]
    public void OnlyStrictlyLargeNegativeDisplacementsUseTheSelector()
    {
        var halfMaxMove = Fixed.FromInt(15);

        Assert.IsFalse(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            -halfMaxMove, Fixed.Zero, halfMaxMove,
            GameCompatibility.Mbf, compMoveBlock: false));

        Assert.IsFalse(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            Fixed.Zero, -halfMaxMove, halfMaxMove,
            GameCompatibility.Mbf, compMoveBlock: false));

        Assert.IsTrue(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            Fixed.FromInt(-16), Fixed.Zero, halfMaxMove,
            GameCompatibility.Mbf, compMoveBlock: false));
    }

    [TestMethod]
    public void PositiveLargeMovementRemainsSplitIndependentlyOfCompMoveBlock()
    {
        // Positive displacement splitting is the original Doom behavior and is
        // still handled by the unconditional positive checks in XYMovement.
        // comp_moveblock only selects whether the matching negative-side fix
        // participates.
        var halfMaxMove = Fixed.FromInt(15);
        var largePositive = Fixed.FromInt(16);

        Assert.IsFalse(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            largePositive, Fixed.Zero, halfMaxMove,
            GameCompatibility.Mbf, compMoveBlock: false));

        Assert.IsFalse(MbfMoveBlockCompatibility.ShouldSplitNegativeDisplacement(
            largePositive, Fixed.Zero, halfMaxMove,
            GameCompatibility.Mbf, compMoveBlock: true));
    }
}
