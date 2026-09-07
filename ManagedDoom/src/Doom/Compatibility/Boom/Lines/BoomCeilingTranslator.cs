using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomCeilingTranslator
{
    public const int Min = 0x4000;
    public const int Max = 0x5FFF;

    private const int ModelMask = 0x0020;
    private const int DirectionMask = 0x0040;
    private const int TargetMask = 0x0380;
    private const int TargetShift = 7;
    private const int ChangeMask = 0x0C00;
    private const int ChangeShift = 10;
    private const int CrushMask = 0x1000;

    public static bool IsCeilingSpecial(LineSpecial special)
    {
        var value = (int)special;
        return value >= Min && value <= Max;
    }

    public static bool TryTranslate(LineSpecial special, out BoomCeilingSpecial specification)
    {
        if (!IsCeilingSpecial(special))
        {
            specification = default;
            return false;
        }

        var value = (int)special;
        var change = (BoomChangeType)((value & ChangeMask) >> ChangeShift);
        var modelBit = (value & ModelMask) != 0;
        var allowsMonsters = change == BoomChangeType.None && modelBit;
        var model = change != BoomChangeType.None && modelBit ? BoomModelType.Numeric : BoomModelType.Trigger;
        var common = BoomGeneralizedSpecial.DecodeCommon(special, allowsMonsters);
        var direction = (value & DirectionMask) != 0 ? BoomPlaneDirection.Up : BoomPlaneDirection.Down;
        var target = (BoomCeilingTarget)((value & TargetMask) >> TargetShift);
        var crush = (value & CrushMask) != 0;

        specification = new BoomCeilingSpecial(common, direction, target, change, model, crush);
        return true;
    }

    public static BoomCeilingSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special), special, "The linedef is not a generalized Boom ceiling special.");

        return specification;
    }
}
