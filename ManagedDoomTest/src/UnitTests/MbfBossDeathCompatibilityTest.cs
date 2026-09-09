using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfBossDeathCompatibilityTest
{
    [TestMethod]
    public void SelectorMatchesVanillaBoomMbfAndMbf21Boundaries()
    {
        Assert.IsFalse(MbfBossDeathCompatibility.UsesPreUltimateBossChecks(
            GameCompatibility.Vanilla, comp666: true));
        Assert.IsFalse(MbfBossDeathCompatibility.UsesPreUltimateBossChecks(
            GameCompatibility.Boom, comp666: true));
        Assert.IsFalse(MbfBossDeathCompatibility.UsesPreUltimateBossChecks(
            GameCompatibility.Mbf, comp666: false));
        Assert.IsTrue(MbfBossDeathCompatibility.UsesPreUltimateBossChecks(
            GameCompatibility.Mbf, comp666: true));
        Assert.IsFalse(MbfBossDeathCompatibility.UsesPreUltimateBossChecks(
            GameCompatibility.Mbf21, comp666: true));
    }

    [TestMethod]
    public void CorrectedPathUsesEpisodeSpecificBossTypes()
    {
        Assert.IsTrue(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: false, episode: 1, map: 8, MobjType.Bruiser));
        Assert.IsFalse(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: false, episode: 1, map: 8, MobjType.Cyborg));

        Assert.IsTrue(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: false, episode: 2, map: 8, MobjType.Cyborg));
        Assert.IsFalse(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: false, episode: 2, map: 8, MobjType.Spider));

        Assert.IsTrue(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: false, episode: 3, map: 8, MobjType.Spider));
        Assert.IsTrue(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: false, episode: 4, map: 6, MobjType.Cyborg));
        Assert.IsTrue(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: false, episode: 4, map: 8, MobjType.Spider));
    }

    [TestMethod]
    public void PreUltimatePathBroadensEpisodeOneToThreeMapEightChecks()
    {
        Assert.IsTrue(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: true, episode: 1, map: 8, MobjType.Cyborg));
        Assert.IsTrue(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: true, episode: 2, map: 8, MobjType.Spider));
        Assert.IsTrue(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: true, episode: 3, map: 8, MobjType.Cyborg));

        Assert.IsFalse(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: true, episode: 2, map: 8, MobjType.Bruiser));
        Assert.IsFalse(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf, comp666: true, episode: 3, map: 7, MobjType.Spider));
    }

    [TestMethod]
    public void Mbf21DeoptionalizesComp666ToCorrectedBehavior()
    {
        Assert.IsFalse(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf21, comp666: true, episode: 2, map: 8, MobjType.Spider));
        Assert.IsTrue(MbfBossDeathCompatibility.IsNonCommercialBossDeathTrigger(
            GameCompatibility.Mbf21, comp666: true, episode: 2, map: 8, MobjType.Cyborg));
    }
}
