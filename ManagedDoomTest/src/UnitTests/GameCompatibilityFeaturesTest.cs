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
            Assert.AreEqual(GameCompatibilityFeatures.SupportsBoom(compatibility), GameCompatibilityFeatures.SupportsTranslucentSprites(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsBoom(compatibility), GameCompatibilityFeatures.SupportsBoomProfileSkyTransfer(compatibility), compatibility.ToString());
            Assert.AreEqual((int)compatibility >= (int)GameCompatibility.Mbf, GameCompatibilityFeatures.SupportsMbf(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfThingFlags(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfActorPhysicsFlags(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfFriendAi(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfMonsterMemory(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfMonsterFriction(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfFriendDistance(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfBlazingDoorCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfDoorLightingCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfSectorModelCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfFalloffCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfFloorCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfSkyMapCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfZombieExitCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfStairCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfMoveBlockCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfSoundCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfBossDeathCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfMaskedAnimationCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfOuchFaceCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfMaxHealthCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfTranslucencyCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfLedgeBlockCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfFriendlySpawnCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfVoodooScrollerCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfReservedLineFlagCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfStateControlCodePointers(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfActorUtilityCodePointers(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfLineAndSoundCodePointers(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfMeleeCodePointers(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfMushroomCodePointer(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfTelefragCompatibility(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfMonsterHazardAvoidance(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfMonsterBacking(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfMonsterInfighting(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfHelpFriends(compatibility), compatibility.ToString());
            Assert.AreEqual(GameCompatibilityFeatures.SupportsMbf(compatibility), GameCompatibilityFeatures.SupportsMbfPlayerHelpers(compatibility), compatibility.ToString());
            Assert.AreEqual((int)compatibility >= (int)GameCompatibility.Mbf21, GameCompatibilityFeatures.SupportsMbf21(compatibility), compatibility.ToString());
        }
    }

    [TestMethod]
    public void GameOptionsDefaultToVanillaCompatibility()
    {
        Assert.AreEqual(GameCompatibility.Vanilla, new GameOptions().Compatibility);
    }
}
