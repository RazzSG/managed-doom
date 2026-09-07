using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomPlatformTranslator
{
    public static bool TryTranslate(LineSpecial special, out BoomPlatformSpecial specification)
    {
        specification = (int)special switch
        {
            143 => new BoomPlatformSpecial(BoomPlatformAction.RaiseAndChange, BoomTriggerType.WalkOnce, 24),
            144 => new BoomPlatformSpecial(BoomPlatformAction.RaiseAndChange, BoomTriggerType.WalkOnce, 32),
            148 => new BoomPlatformSpecial(BoomPlatformAction.RaiseAndChange, BoomTriggerType.WalkRepeat, 24),
            149 => new BoomPlatformSpecial(BoomPlatformAction.RaiseAndChange, BoomTriggerType.WalkRepeat, 32),
            162 => new BoomPlatformSpecial(BoomPlatformAction.Perpetual, BoomTriggerType.SwitchOnce),
            163 => new BoomPlatformSpecial(BoomPlatformAction.Stop, BoomTriggerType.SwitchOnce),
            181 => new BoomPlatformSpecial(BoomPlatformAction.Perpetual, BoomTriggerType.SwitchRepeat),
            182 => new BoomPlatformSpecial(BoomPlatformAction.Stop, BoomTriggerType.SwitchRepeat),
            211 => new BoomPlatformSpecial(BoomPlatformAction.Toggle, BoomTriggerType.SwitchRepeat),
            212 => new BoomPlatformSpecial(BoomPlatformAction.Toggle, BoomTriggerType.WalkRepeat),
            _ => default
        };

        return IsExtendedPlatformSpecial(special);
    }

    public static BoomPlatformSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special), special, "Not an extended Boom platform special.");

        return specification;
    }

    public static bool IsExtendedPlatformSpecial(LineSpecial special)
    {
        return (int)special is 143 or 144 or 148 or 149 or 162 or 163 or 181 or 182 or 211 or 212;
    }
}
