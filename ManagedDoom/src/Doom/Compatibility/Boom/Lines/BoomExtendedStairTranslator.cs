using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomExtendedStairTranslator
{
    public static bool TryTranslate(LineSpecial special, out BoomExtendedStairSpecial specification)
    {
        specification = (int)special switch
        {
            256 => new BoomExtendedStairSpecial(BoomExtendedStairAction.Build8, BoomTriggerType.WalkRepeat),
            257 => new BoomExtendedStairSpecial(BoomExtendedStairAction.Turbo16, BoomTriggerType.WalkRepeat),
            258 => new BoomExtendedStairSpecial(BoomExtendedStairAction.Build8, BoomTriggerType.SwitchRepeat),
            259 => new BoomExtendedStairSpecial(BoomExtendedStairAction.Turbo16, BoomTriggerType.SwitchRepeat),
            _ => default
        };

        return IsExtendedStairSpecial(special);
    }

    public static BoomExtendedStairSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special), special, "Not an extended Boom stair special.");

        return specification;
    }

    public static bool IsExtendedStairSpecial(LineSpecial special)
    {
        return (int)special is 256 or 257 or 258 or 259;
    }
}
