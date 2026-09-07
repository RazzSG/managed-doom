using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomDoorTranslator
{
    public const int Min = 0x3C00;
    public const int Max = 0x3FFF;

    private const int KindMask = 0x0060;
    private const int KindShift = 5;
    private const int MonsterMask = 0x0080;
    private const int DelayMask = 0x0300;
    private const int DelayShift = 8;

    public static bool IsDoorSpecial(LineSpecial special)
    {
        var value = (int)special;
        return value >= Min && value <= Max;
    }

    public static bool TryTranslate(LineSpecial special, out BoomDoorSpecial specification)
    {
        if (!IsDoorSpecial(special))
        {
            specification = default;
            return false;
        }

        var value = (int)special;
        var common = BoomGeneralizedSpecial.DecodeCommon(special, (value & MonsterMask) != 0);
        var kind = (BoomDoorKind)((value & KindMask) >> KindShift);
        var waitTics = DecodeWaitTics((value & DelayMask) >> DelayShift);

        specification = new BoomDoorSpecial(common, kind, waitTics);
        return true;
    }

    public static BoomDoorSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special), special, "The linedef is not a generalized Boom door special.");

        return specification;
    }

    private static int DecodeWaitTics(int value)
    {
        return value switch
        {
            0 => 35,
            1 => 150,
            2 => 300,
            3 => 1050,
            _ => 150
        };
    }
}
