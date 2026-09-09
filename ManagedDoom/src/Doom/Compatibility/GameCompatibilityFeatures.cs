namespace ManagedDoom.Compatibility;

public static class GameCompatibilityFeatures
{
    public static bool SupportsBoom(GameCompatibility compatibility) => (int)compatibility >= (int)GameCompatibility.Boom;

    public static bool SupportsBoomLineSpecials(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsBoomPassThru(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsGeneralizedSectorSpecials(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsTransferHeights(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsTranslucentLines(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsTranslucentSprites(GameCompatibility compatibility) => SupportsBoom(compatibility);

    // MBF introduced linedefs 271/272, but PrBoom-family ports commonly expose
    // them to Boom-profile maps. The detector intentionally keeps these maps at
    // Boom so the rendering feature must follow the same profile boundary.
    public static bool SupportsBoomProfileSkyTransfer(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsMbf(GameCompatibility compatibility) => (int)compatibility >= (int)GameCompatibility.Mbf;

    public static bool SupportsMbfThingFlags(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfActorPhysicsFlags(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfFriendAi(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMonsterMemory(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMonsterFriction(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfFriendDistance(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMonsterHazardAvoidance(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMonsterBacking(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfPursuit(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfTelefragCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfDropoffCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfArchVileCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfBlazingDoorCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfDoorLightingCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfSectorModelCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfGodModeCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfFalloffCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfFloorCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfSkyMapCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfZombieExitCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfStairCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMoveBlockCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfSoundCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfBossDeathCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMaskedAnimationCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfOuchFaceCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMaxHealthCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfTranslucencyCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfLedgeBlockCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfFriendlySpawnCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfVoodooScrollerCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfReservedLineFlagCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfStateControlCodePointers(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfActorUtilityCodePointers(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfLineAndSoundCodePointers(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMeleeCodePointers(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMushroomCodePointer(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfStayOnLift(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfDoorStuckCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfPainElementalCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfSkullSpawnCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfRespawnCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfLostSoulCompatibility(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMonsterInfighting(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfHelpFriends(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfPlayerHelpers(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfDogActor(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfDogJumping(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbfMonkeyClimbing(GameCompatibility compatibility) => SupportsMbf(compatibility);

    public static bool SupportsMbf21(GameCompatibility compatibility) => (int)compatibility >= (int)GameCompatibility.Mbf21;
}
