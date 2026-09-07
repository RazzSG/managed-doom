using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomTeleportTranslator
{
    public static bool TryTranslate(LineSpecial special, out BoomTeleportSpecial specification)
    {
        specification = (int)special switch
        {
            174 => Thing(BoomTriggerType.SwitchOnce, silent: false, preserveOrientation: false, playersAllowed: true, allowsZeroTag: true),
            195 => Thing(BoomTriggerType.SwitchRepeat, silent: false, preserveOrientation: false, playersAllowed: true, allowsZeroTag: true),

            207 => Thing(BoomTriggerType.WalkOnce, silent: true, preserveOrientation: true, playersAllowed: true, allowsZeroTag: true),
            208 => Thing(BoomTriggerType.WalkRepeat, silent: true, preserveOrientation: true, playersAllowed: true, allowsZeroTag: true),
            209 => Thing(BoomTriggerType.SwitchOnce, silent: true, preserveOrientation: true, playersAllowed: true, allowsZeroTag: true),
            210 => Thing(BoomTriggerType.SwitchRepeat, silent: true, preserveOrientation: true, playersAllowed: true, allowsZeroTag: true),

            243 => Line(BoomTriggerType.WalkOnce, reverse: false, playersAllowed: true),
            244 => Line(BoomTriggerType.WalkRepeat, reverse: false, playersAllowed: true),
            262 => Line(BoomTriggerType.WalkOnce, reverse: true, playersAllowed: true),
            263 => Line(BoomTriggerType.WalkRepeat, reverse: true, playersAllowed: true),

            264 => Line(BoomTriggerType.WalkOnce, reverse: true, playersAllowed: false),
            265 => Line(BoomTriggerType.WalkRepeat, reverse: true, playersAllowed: false),
            266 => Line(BoomTriggerType.WalkOnce, reverse: false, playersAllowed: false),
            267 => Line(BoomTriggerType.WalkRepeat, reverse: false, playersAllowed: false),

            268 => Thing(BoomTriggerType.WalkOnce, silent: true, preserveOrientation: true, playersAllowed: false, allowsZeroTag: false),
            269 => Thing(BoomTriggerType.WalkRepeat, silent: true, preserveOrientation: true, playersAllowed: false, allowsZeroTag: false),
            _ => default
        };

        return (int)special is 174 or 195 or
            207 or 208 or 209 or 210 or
            243 or 244 or 262 or 263 or 264 or 265 or 266 or 267 or 268 or 269;
    }

    public static BoomTeleportSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special));

        return specification;
    }

    private static BoomTeleportSpecial Thing(
        BoomTriggerType trigger,
        bool silent,
        bool preserveOrientation,
        bool playersAllowed,
        bool allowsZeroTag)
    {
        return new BoomTeleportSpecial(
            trigger,
            BoomTeleportDestination.Thing,
            silent,
            preserveOrientation,
            reverse: false,
            playersAllowed,
            allowsZeroTag);
    }

    private static BoomTeleportSpecial Line(BoomTriggerType trigger, bool reverse, bool playersAllowed)
    {
        return new BoomTeleportSpecial(
            trigger,
            BoomTeleportDestination.Line,
            silent: true,
            preserveOrientation: true,
            reverse,
            playersAllowed,
            allowsZeroTag: false);
    }
}
