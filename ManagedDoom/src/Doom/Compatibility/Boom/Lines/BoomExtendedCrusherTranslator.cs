using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomExtendedCrusherTranslator
{
    public static bool TryTranslate(LineSpecial special, out BoomExtendedCrusherSpecial specification)
    {
        specification = (int)special switch
        {
            150 => new BoomExtendedCrusherSpecial(BoomExtendedCrusherAction.Silent, BoomTriggerType.WalkRepeat),
            164 => new BoomExtendedCrusherSpecial(BoomExtendedCrusherAction.Fast, BoomTriggerType.SwitchOnce),
            165 => new BoomExtendedCrusherSpecial(BoomExtendedCrusherAction.Silent, BoomTriggerType.SwitchOnce),
            168 => new BoomExtendedCrusherSpecial(BoomExtendedCrusherAction.Stop, BoomTriggerType.SwitchOnce),
            183 => new BoomExtendedCrusherSpecial(BoomExtendedCrusherAction.Fast, BoomTriggerType.SwitchRepeat),
            184 => new BoomExtendedCrusherSpecial(BoomExtendedCrusherAction.Slow, BoomTriggerType.SwitchRepeat),
            185 => new BoomExtendedCrusherSpecial(BoomExtendedCrusherAction.Silent, BoomTriggerType.SwitchRepeat),
            188 => new BoomExtendedCrusherSpecial(BoomExtendedCrusherAction.Stop, BoomTriggerType.SwitchRepeat),
            _ => default
        };

        return IsExtendedCrusherSpecial(special);
    }

    public static BoomExtendedCrusherSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special), special, "Not an extended Boom crusher special.");

        return specification;
    }

    public static bool IsExtendedCrusherSpecial(LineSpecial special)
    {
        return (int)special is 150 or 164 or 165 or 168 or 183 or 184 or 185 or 188;
    }
}
