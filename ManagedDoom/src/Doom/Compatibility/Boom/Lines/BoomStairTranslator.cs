using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomStairTranslator
{
    public const int Min = 0x3000;
    public const int Max = 0x33FF;

    public const int DirectionMask = 0x0100;

    private const int IgnoreTextureMask = 0x0200;
    private const int StepMask = 0x00C0;
    private const int StepShift = 6;
    private const int MonsterMask = 0x0020;

    public static bool IsStairSpecial(LineSpecial special)
    {
        var value = (int)special;
        return value >= Min && value <= Max;
    }

    public static bool TryTranslate(LineSpecial special, out BoomStairSpecial specification)
    {
        if (!IsStairSpecial(special))
        {
            specification = default;
            return false;
        }

        var value = (int)special;
        var common = BoomGeneralizedSpecial.DecodeCommon(special, (value & MonsterMask) != 0);
        var direction = (value & DirectionMask) != 0 ? BoomPlaneDirection.Up : BoomPlaneDirection.Down;
        var stepSize = DecodeStepSize((value & StepMask) >> StepShift);
        var ignoreTexture = (value & IgnoreTextureMask) != 0;

        specification = new BoomStairSpecial(common, direction, stepSize, ignoreTexture);
        return true;
    }

    public static BoomStairSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special), special, "The linedef is not a generalized Boom stair special.");

        return specification;
    }

    private static int DecodeStepSize(int value)
    {
        return value switch
        {
            0 => 4,
            1 => 8,
            2 => 16,
            3 => 24,
            _ => 4
        };
    }
}
