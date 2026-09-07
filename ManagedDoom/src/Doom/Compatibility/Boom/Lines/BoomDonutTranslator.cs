using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomDonutTranslator
{
    public static bool TryTranslate(LineSpecial special, out BoomDonutSpecial specification)
    {
        specification = (int)special switch
        {
            146 => new BoomDonutSpecial(BoomTriggerType.WalkOnce),
            155 => new BoomDonutSpecial(BoomTriggerType.WalkRepeat),
            191 => new BoomDonutSpecial(BoomTriggerType.SwitchRepeat),
            _ => default
        };

        return (int)special is 146 or 155 or 191;
    }

    public static BoomDonutSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special));

        return specification;
    }
}
