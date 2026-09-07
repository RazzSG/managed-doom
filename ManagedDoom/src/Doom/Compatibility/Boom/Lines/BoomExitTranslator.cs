using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomExitTranslator
{
    public static bool TryTranslate(LineSpecial special, out BoomExitSpecial specification)
    {
        specification = (int)special switch
        {
            197 => new BoomExitSpecial(BoomExitType.Normal, BoomTriggerType.GunOnce),
            198 => new BoomExitSpecial(BoomExitType.Secret, BoomTriggerType.GunOnce),
            _ => default
        };

        return (int)special is 197 or 198;
    }

    public static BoomExitSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special));

        return specification;
    }
}
