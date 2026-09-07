using ManagedDoom.Compatibility.Boom.Sectors;

namespace ManagedDoom.Compatibility.Boom.Sectors;

public static class BoomSectorSpecialDecoder
{
    public const int LightingMask = 0x01f;
    public const int DamageMask = 0x060;
    public const int DamageShift = 5;
    public const int SecretMask = 0x080;
    public const int FrictionMask = 0x100;
    public const int PusherMask = 0x200;

    // Boom reserves bits 10 and 11 for sound control, but does not implement them.
    public const int ReservedSoundMask = 0xc00;

    public static BoomSectorSpecial Decode(SectorSpecial special)
    {
        return Decode((int)special);
    }

    public static SectorSpecial ClearLightingSpecial(SectorSpecial special)
    {
        return (SectorSpecial)((int)special & ~LightingMask);
    }

    public static SectorSpecial ClearSecretFlag(SectorSpecial special)
    {
        return (SectorSpecial)((int)special & ~SecretMask);
    }

    public static BoomSectorSpecial Decode(int special)
    {
        var damage = (BoomSectorDamage)((special & DamageMask) >> DamageShift);

        return new BoomSectorSpecial(
            special,
            special & LightingMask,
            damage,
            (special & SecretMask) != 0,
            (special & FrictionMask) != 0,
            (special & PusherMask) != 0);
    }
}
