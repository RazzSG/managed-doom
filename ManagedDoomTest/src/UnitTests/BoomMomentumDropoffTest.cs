using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Movement;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomMomentumDropoffTest
{
    [TestMethod]
    public void BoomBaselineAllowsRequestedPhysicalMomentumDropoff()
    {
        Assert.IsFalse(AllowsRuntimeDropoff(
            GameCompatibility.Vanilla,
            compDropoff: false,
            requested: true));
        Assert.IsFalse(AllowsRuntimeDropoff(
            GameCompatibility.Boom,
            compDropoff: false,
            requested: false));
        Assert.IsTrue(AllowsRuntimeDropoff(
            GameCompatibility.Boom,
            compDropoff: false,
            requested: true));
    }

    [TestMethod]
    public void MbfCompDropoffOverridesInheritedBoomMomentumPermission()
    {
        Assert.IsTrue(AllowsRuntimeDropoff(
            GameCompatibility.Mbf,
            compDropoff: false,
            requested: true));
        Assert.IsFalse(AllowsRuntimeDropoff(
            GameCompatibility.Mbf,
            compDropoff: true,
            requested: true));
        Assert.IsTrue(AllowsRuntimeDropoff(
            GameCompatibility.Mbf21,
            compDropoff: false,
            requested: true));
        Assert.IsFalse(AllowsRuntimeDropoff(
            GameCompatibility.Mbf21,
            compDropoff: true,
            requested: true));
    }

    private static bool AllowsRuntimeDropoff(
        GameCompatibility compatibility,
        bool compDropoff,
        bool requested)
    {
        return BoomMomentumDropoff.Allows(compatibility, requested) ||
               MbfDropoffCompatibility.AllowsExternalMomentumDropoff(
                   compatibility,
                   compDropoff,
                   requested);
    }
}
