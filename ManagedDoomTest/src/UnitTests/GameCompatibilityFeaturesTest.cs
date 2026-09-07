using System;
using ManagedDoom;
using ManagedDoom.Compatibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class GameCompatibilityFeaturesTest
{
    [TestMethod]
    public void CompatibilityLevelsInheritPreviousFeatures()
    {
        foreach (GameCompatibility compatibility in Enum.GetValues(typeof(GameCompatibility)))
        {
            Assert.AreEqual((int)compatibility >= (int)GameCompatibility.Boom, GameCompatibilityFeatures.SupportsBoom(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsBoom(compatibility), GameCompatibilityFeatures.SupportsBoomLineSpecials(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsBoom(compatibility), GameCompatibilityFeatures.SupportsGeneralizedSectorSpecials(compatibility), compatibility.ToString());
            Assert.AreEqual((int)compatibility >= (int)GameCompatibility.Mbf, GameCompatibilityFeatures.SupportsMbf(compatibility), compatibility.ToString());
            Assert.AreEqual((int)compatibility >= (int)GameCompatibility.Mbf21, GameCompatibilityFeatures.SupportsMbf21(compatibility), compatibility.ToString());
        }
    }

    [TestMethod]
    public void GameOptionsDefaultToVanillaCompatibility()
    {
        Assert.AreEqual(GameCompatibility.Vanilla, new GameOptions().Compatibility);
    }
}
