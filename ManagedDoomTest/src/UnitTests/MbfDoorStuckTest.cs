using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Doors;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfDoorStuckTest
{
    [TestMethod]
    public void FeatureGateStartsAtMbf()
    {
        Assert.IsFalse(GameCompatibilityFeatures.SupportsMbfDoorStuckCompatibility(GameCompatibility.Vanilla));
        Assert.IsFalse(GameCompatibilityFeatures.SupportsMbfDoorStuckCompatibility(GameCompatibility.Boom));
        Assert.IsTrue(GameCompatibilityFeatures.SupportsMbfDoorStuckCompatibility(GameCompatibility.Mbf));
        Assert.IsTrue(GameCompatibilityFeatures.SupportsMbfDoorStuckCompatibility(GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void EnabledFlagRestoresClassicSuccessForAnyActivatedSpecial()
    {
        var blocking = BoomDoorCompatibility.BlockingLineActivated;
        var other = BoomDoorCompatibility.OtherLineActivated;

        Assert.IsTrue(MbfDoorStuck.UsesClassicBlockedDoorResult(
            GameCompatibility.Mbf, true, blocking));
        Assert.IsTrue(MbfDoorStuck.UsesClassicBlockedDoorResult(
            GameCompatibility.Mbf, true, other));
        Assert.IsTrue(MbfDoorStuck.UsesClassicBlockedDoorResult(
            GameCompatibility.Mbf, true, blocking | other));
    }

    [TestMethod]
    public void DisabledFlagLeavesCorrectedMbfDoorTrackHeuristicActive()
    {
        var blocking = BoomDoorCompatibility.BlockingLineActivated;
        var other = BoomDoorCompatibility.OtherLineActivated;

        Assert.IsFalse(MbfDoorStuck.UsesClassicBlockedDoorResult(
            GameCompatibility.Mbf, false, blocking));

        Assert.IsTrue(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Mbf, blocking, 229));
        Assert.IsFalse(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Mbf, blocking, 230));
        Assert.IsFalse(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Mbf, other, 229));
        Assert.IsTrue(BoomDoorCompatibility.ShouldContinueAfterBlockedDoorAttempt(
            GameCompatibility.Mbf, other, 230));
    }

    [TestMethod]
    public void FlagDoesNotAffectBoomOrEmptyActivationSet()
    {
        var blocking = BoomDoorCompatibility.BlockingLineActivated;

        Assert.IsFalse(MbfDoorStuck.UsesClassicBlockedDoorResult(
            GameCompatibility.Boom, true, blocking));
        Assert.IsFalse(MbfDoorStuck.UsesClassicBlockedDoorResult(
            GameCompatibility.Mbf, true, 0));
    }
}
