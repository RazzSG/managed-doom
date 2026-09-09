using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf;

/// <summary>
/// Runtime MBF OPTIONS values that are already consumed by implemented
/// compatibility features. Keep this object small and deterministic; future
/// MBF options can be added here as their runtime semantics land.
/// </summary>
public sealed class MbfOptions
{
    public const int DefaultFriendDistance = 128;
    public const int DefaultPlayerHelpers = 0;
    public const int MaxPlayerHelpers = 3;
    public const int MaxFriendDistance = 999;

    private int playerHelpers;
    private int friendDistance;
    private Fixed friendDistanceFixed;
    private bool compLedgeBlock;
    private bool hasCompLedgeBlockOverride;
    private bool compVoodooScroller;
    private bool hasCompVoodooScrollerOverride;

    public MbfOptions()
    {
        MonstersRemember = true;
        MonsterAvoidHazards = true;
        MonsterFriction = true;
        MonsterInfighting = true;
        MonsterBacking = false;
        CompPursuit = false;
        CompTelefrag = false;
        // A clean MBF/PrBoom configuration defaults comp_dropoff to 0.
        // Physical P_XYMovement passes dropoff=true so torque, corpse momentum,
        // and other involuntary movement can carry objects over tall ledges.
        CompDropoff = false;
        CompVile = false;
        CompBlazing = false;
        CompDoorLight = false;
        CompModel = false;
        CompGod = false;
        CompFalloff = false;
        CompFloors = false;
        CompSkyMap = false;
        CompZombie = true;
        CompStairs = false;
        CompInfCheat = false;
        CompZeroTags = false;
        CompMoveBlock = false;
        CompSound = false;
        Comp666 = false;
        CompMaskedAnim = false;
        CompOuchFace = false;
        CompMaxHealth = false;
        CompTranslucency = false;
        CompFriendlySpawn = true;
        compLedgeBlock = false;
        hasCompLedgeBlockOverride = false;
        compVoodooScroller = true;
        hasCompVoodooScrollerOverride = false;
        CompReservedLineFlag = true;
        CompStayLift = false;
        CompDoorStuck = false;
        CompPain = false;
        CompSkull = false;
        CompRespawn = true;
        CompSoul = true;
        HelpFriends = false;
        DogJumping = true;
        Monkeys = false;
        PlayerHelpers = DefaultPlayerHelpers;
        FriendDistance = DefaultFriendDistance;
    }

    public bool MonstersRemember { get; set; }

    public bool MonsterAvoidHazards { get; set; }

    public bool MonsterFriction { get; set; }

    public bool MonsterInfighting { get; set; }

    public bool MonsterBacking { get; set; }

    public bool CompPursuit { get; set; }

    public bool CompTelefrag { get; set; }

    public bool CompDropoff { get; set; }

    public bool CompVile { get; set; }

    public bool CompBlazing { get; set; }

    public bool CompDoorLight { get; set; }

    public bool CompModel { get; set; }

    public bool CompGod { get; set; }

    public bool CompFalloff { get; set; }

    public bool CompFloors { get; set; }

    public bool CompSkyMap { get; set; }

    public bool CompZombie { get; set; }

    public bool CompStairs { get; set; }

    public bool CompInfCheat { get; set; }

    public bool CompZeroTags { get; set; }

    public bool CompMoveBlock { get; set; }

    public bool CompSound { get; set; }

    public bool Comp666 { get; set; }

    public bool CompMaskedAnim { get; set; }

    public bool CompOuchFace { get; set; }

    public bool CompMaxHealth { get; set; }

    public bool CompTranslucency { get; set; }

    public bool CompFriendlySpawn { get; set; }

    /// <summary>
    /// PrBoom/MBF21 ledge blocking option. The explicit-state bit is kept
    /// separately because MBF defaults this option to false while MBF21
    /// defaults it to true, and an explicit OPTIONS value of 0 must remain
    /// distinguishable from an absent key.
    /// </summary>
    public bool CompLedgeBlock
    {
        get => compLedgeBlock;
        set
        {
            compLedgeBlock = value;
            hasCompLedgeBlockOverride = true;
        }
    }

    public bool HasCompLedgeBlockOverride => hasCompLedgeBlockOverride;

    /// <summary>
    /// PrBoom/MBF21 voodoo-scroller option. MBF defaults this compatibility
    /// bug to true, while MBF21 defaults it to false. Keep the explicit-state
    /// bit so an OPTIONS override remains distinguishable from profile default.
    /// </summary>
    public bool CompVoodooScroller
    {
        get => compVoodooScroller;
        set
        {
            compVoodooScroller = value;
            hasCompVoodooScrollerOverride = true;
        }
    }

    public bool HasCompVoodooScrollerOverride => hasCompVoodooScrollerOverride;

    /// <summary>
    /// PrBoom/MBF21 compatibility for the reserved linedef flag 0x0800.
    /// When enabled, a line carrying that bit is reduced to the original
    /// Doom flag range (flags &amp; 0x01ff). MBF and MBF21 both default to true.
    /// </summary>
    public bool CompReservedLineFlag { get; set; }

    public bool CompStayLift { get; set; }

    public bool CompDoorStuck { get; set; }

    public bool CompPain { get; set; }

    public bool CompSkull { get; set; }

    public bool CompRespawn { get; set; }

    public bool CompSoul { get; set; }

    public bool HelpFriends { get; set; }

    public bool DogJumping { get; set; }

    public bool Monkeys { get; set; }

    public int PlayerHelpers
    {
        get => playerHelpers;
        set => playerHelpers = System.Math.Clamp(value, 0, MaxPlayerHelpers);
    }

    public int FriendDistance
    {
        get => friendDistance;
        set
        {
            friendDistance = System.Math.Clamp(value, 0, MaxFriendDistance);
            friendDistanceFixed = Fixed.FromInt(friendDistance);
        }
    }

    public Fixed FriendDistanceFixed => friendDistanceFixed;

    public MbfOptions Clone()
    {
        var clone = new MbfOptions
        {
            MonstersRemember = MonstersRemember,
            MonsterAvoidHazards = MonsterAvoidHazards,
            MonsterFriction = MonsterFriction,
            MonsterInfighting = MonsterInfighting,
            MonsterBacking = MonsterBacking,
            CompPursuit = CompPursuit,
            CompTelefrag = CompTelefrag,
            CompDropoff = CompDropoff,
            CompVile = CompVile,
            CompBlazing = CompBlazing,
            CompDoorLight = CompDoorLight,
            CompModel = CompModel,
            CompGod = CompGod,
            CompFalloff = CompFalloff,
            CompFloors = CompFloors,
            CompSkyMap = CompSkyMap,
            CompZombie = CompZombie,
            CompStairs = CompStairs,
            CompInfCheat = CompInfCheat,
            CompZeroTags = CompZeroTags,
            CompMoveBlock = CompMoveBlock,
            CompSound = CompSound,
            Comp666 = Comp666,
            CompMaskedAnim = CompMaskedAnim,
            CompOuchFace = CompOuchFace,
            CompMaxHealth = CompMaxHealth,
            CompTranslucency = CompTranslucency,
            CompFriendlySpawn = CompFriendlySpawn,
            CompReservedLineFlag = CompReservedLineFlag,
            CompStayLift = CompStayLift,
            CompDoorStuck = CompDoorStuck,
            CompPain = CompPain,
            CompSkull = CompSkull,
            CompRespawn = CompRespawn,
            CompSoul = CompSoul,
            HelpFriends = HelpFriends,
            DogJumping = DogJumping,
            Monkeys = Monkeys,
            PlayerHelpers = PlayerHelpers,
            FriendDistance = FriendDistance
        };

        // Preserve whether comp_ledgeblock was absent or explicitly assigned.
        // Using the public setter here would turn an absent source option into
        // an explicit false override and break the MBF21 default of true.
        clone.compLedgeBlock = compLedgeBlock;
        clone.hasCompLedgeBlockOverride = hasCompLedgeBlockOverride;

        // MBF and MBF21 use different profile defaults for this option too.
        clone.compVoodooScroller = compVoodooScroller;
        clone.hasCompVoodooScrollerOverride = hasCompVoodooScrollerOverride;

        return clone;
    }
}
