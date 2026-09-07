using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomLiftTranslator
{
    public const int Min = 0x3400;
    public const int Max = 0x37FF;

    private const int MonsterMask = 0x0020;
    private const int DelayMask = 0x00C0;
    private const int DelayShift = 6;
    private const int TargetMask = 0x0300;
    private const int TargetShift = 8;

    public static bool IsLiftSpecial(LineSpecial special)
    {
        var value = (int)special;
        return value >= Min && value <= Max;
    }

    public static bool TryTranslate(LineSpecial special, out BoomLiftSpecial specification)
    {
        if (!IsLiftSpecial(special))
        {
            specification = default;
            return false;
        }

        var value = (int)special;
        var common = BoomGeneralizedSpecial.DecodeCommon(special, (value & MonsterMask) != 0);
        var target = (BoomLiftTarget)((value & TargetMask) >> TargetShift);
        var waitTics = DecodeWaitTics((value & DelayMask) >> DelayShift);

        specification = new BoomLiftSpecial(common, target, waitTics);
        return true;
    }

    public static BoomLiftSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special), special, "The linedef is not a generalized Boom lift special.");

        return specification;
    }

    private static int DecodeWaitTics(int value)
    {
        return value switch
        {
            0 => 35,
            1 => 105,
            2 => 165,
            3 => 350,
            _ => 105
        };
    }
}
