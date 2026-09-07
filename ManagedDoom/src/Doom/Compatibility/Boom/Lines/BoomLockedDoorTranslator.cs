using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomLockedDoorTranslator
{
    public const int Min = 0x3800;
    public const int Max = 0x3BFF;

    private const int KindMask = 0x0020;
    private const int KindShift = 5;
    private const int KeyMask = 0x01C0;
    private const int KeyShift = 6;
    private const int SkullIsCardMask = 0x0200;

    public static bool IsLockedDoorSpecial(LineSpecial special)
    {
        var value = (int)special;
        return value >= Min && value <= Max;
    }

    public static bool TryTranslate(LineSpecial special, out BoomLockedDoorSpecial specification)
    {
        if (!IsLockedDoorSpecial(special))
        {
            specification = default;
            return false;
        }

        var value = (int)special;
        var common = BoomGeneralizedSpecial.DecodeCommon(special, false);
        var kind = (BoomLockedDoorKind)((value & KindMask) >> KindShift);
        var key = (BoomLockedDoorKey)((value & KeyMask) >> KeyShift);
        var skullIsCard = (value & SkullIsCardMask) != 0;

        specification = new BoomLockedDoorSpecial(common, kind, key, skullIsCard);
        return true;
    }

    public static BoomLockedDoorSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special), special, "The linedef is not a generalized Boom locked-door special.");

        return specification;
    }
}
