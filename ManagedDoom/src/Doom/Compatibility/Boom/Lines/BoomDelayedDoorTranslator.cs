using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomDelayedDoorTranslator
{
    public static bool TryTranslate(LineSpecial special, out BoomDelayedDoorSpecial specification)
    {
        specification = (int)special switch
        {
            175 => new BoomDelayedDoorSpecial(BoomTriggerType.SwitchOnce),
            196 => new BoomDelayedDoorSpecial(BoomTriggerType.SwitchRepeat),
            _ => default
        };

        return IsDelayedDoorSpecial(special);
    }

    public static BoomDelayedDoorSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special), special, "Not an extended Boom delayed-door special.");

        return specification;
    }

    public static bool IsDelayedDoorSpecial(LineSpecial special)
    {
        return (int)special is 175 or 196;
    }
}
