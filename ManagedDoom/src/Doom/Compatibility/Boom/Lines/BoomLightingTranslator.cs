using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomLightingTranslator
{
    public static bool TryTranslate(LineSpecial special, out BoomLightingSpecial specification)
    {
        specification = (int)special switch
        {
            156 => new BoomLightingSpecial(BoomLightingTarget.Blinking, BoomTriggerType.WalkRepeat),
            157 => new BoomLightingSpecial(BoomLightingTarget.MinimumNeighbor, BoomTriggerType.WalkRepeat),
            169 => new BoomLightingSpecial(BoomLightingTarget.MaximumNeighbor, BoomTriggerType.SwitchOnce),
            170 => new BoomLightingSpecial(BoomLightingTarget.Light35, BoomTriggerType.SwitchOnce),
            171 => new BoomLightingSpecial(BoomLightingTarget.Light255, BoomTriggerType.SwitchOnce),
            172 => new BoomLightingSpecial(BoomLightingTarget.Blinking, BoomTriggerType.SwitchOnce),
            173 => new BoomLightingSpecial(BoomLightingTarget.MinimumNeighbor, BoomTriggerType.SwitchOnce),
            192 => new BoomLightingSpecial(BoomLightingTarget.MaximumNeighbor, BoomTriggerType.SwitchRepeat),
            193 => new BoomLightingSpecial(BoomLightingTarget.Blinking, BoomTriggerType.SwitchRepeat),
            194 => new BoomLightingSpecial(BoomLightingTarget.MinimumNeighbor, BoomTriggerType.SwitchRepeat),
            _ => default
        };

        return (int)special is 156 or 157 or 169 or 170 or 171 or 172 or 173 or 192 or 193 or 194;
    }

    public static BoomLightingSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special));

        return specification;
    }
}
