using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfPainElementalCompatibilityTest
{
    [TestMethod]
    public void VanillaKeepsClassicLostSoulLimit()
    {
        Assert.IsTrue(MbfPainElementalCompatibility.EnforcesLostSoulLimit(
            GameCompatibility.Vanilla,
            compPain: false));
    }

    [TestMethod]
    public void BoomKeepsCorrectedUnlimitedBehavior()
    {
        Assert.IsFalse(MbfPainElementalCompatibility.EnforcesLostSoulLimit(
            GameCompatibility.Boom,
            compPain: true));
    }

    [TestMethod]
    public void MbfUsesCompPainOption()
    {
        Assert.IsFalse(MbfPainElementalCompatibility.EnforcesLostSoulLimit(
            GameCompatibility.Mbf,
            compPain: false));

        Assert.IsTrue(MbfPainElementalCompatibility.EnforcesLostSoulLimit(
            GameCompatibility.Mbf,
            compPain: true));
    }

    [TestMethod]
    public void Mbf21InheritsMbfCompPainSelection()
    {
        Assert.IsFalse(MbfPainElementalCompatibility.EnforcesLostSoulLimit(
            GameCompatibility.Mbf21,
            compPain: false));

        Assert.IsTrue(MbfPainElementalCompatibility.EnforcesLostSoulLimit(
            GameCompatibility.Mbf21,
            compPain: true));
    }
}
