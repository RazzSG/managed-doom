using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomElevatorTranslator
{
    private const int FirstSpecial = 227;
    private const int LastSpecial = 238;

    public static bool TryTranslate(LineSpecial special, out BoomElevatorSpecial specification)
    {
        var value = (int)special;
        if (value < FirstSpecial || value > LastSpecial)
        {
            specification = default;
            return false;
        }

        var relative = value - FirstSpecial;
        var target = (BoomElevatorTarget)(relative / 4);
        var trigger = (relative & 3) switch
        {
            0 => BoomTriggerType.WalkOnce,
            1 => BoomTriggerType.WalkRepeat,
            2 => BoomTriggerType.SwitchOnce,
            3 => BoomTriggerType.SwitchRepeat,
            _ => throw new InvalidOperationException()
        };

        specification = new BoomElevatorSpecial(target, trigger);
        return true;
    }

    public static BoomElevatorSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special));

        return specification;
    }
}
