using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfFalloffCompatibilityTest
{
    [TestMethod]
    public void SelectorMatchesVanillaBoomMbfAndMbf21Boundaries()
    {
        Assert.IsFalse(MbfFalloffCompatibility.UsesLedgeTorque(
            GameCompatibility.Vanilla, compFalloff: false));
        Assert.IsFalse(MbfFalloffCompatibility.UsesLedgeTorque(
            GameCompatibility.Boom, compFalloff: false));

        Assert.IsTrue(MbfFalloffCompatibility.UsesLedgeTorque(
            GameCompatibility.Mbf, compFalloff: false));
        Assert.IsFalse(MbfFalloffCompatibility.UsesLedgeTorque(
            GameCompatibility.Mbf, compFalloff: true));

        Assert.IsTrue(MbfFalloffCompatibility.UsesLedgeTorque(
            GameCompatibility.Mbf21, compFalloff: false));
        Assert.IsFalse(MbfFalloffCompatibility.UsesLedgeTorque(
            GameCompatibility.Mbf21, compFalloff: true));
    }
}
