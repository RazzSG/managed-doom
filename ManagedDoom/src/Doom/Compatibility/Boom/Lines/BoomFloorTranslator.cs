using System;

namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomFloorTranslator
{
    public const int Min = 0x6000;
    public const int Max = 0x7FFF;

    private const int ModelMask = 0x0020;
    private const int DirectionMask = 0x0040;
    private const int TargetMask = 0x0380;
    private const int TargetShift = 7;
    private const int ChangeMask = 0x0C00;
    private const int ChangeShift = 10;
    private const int CrushMask = 0x1000;

    public static bool IsFloorSpecial(LineSpecial special)
    {
        var value = (int)special;
        return value >= Min && value <= Max;
    }

    public static bool TryTranslate(LineSpecial special, out BoomFloorSpecial specification)
    {
        if (!IsFloorSpecial(special))
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
        var target = (BoomFloorTarget)((value & TargetMask) >> TargetShift);
        var crush = (value & CrushMask) != 0;

        specification = new BoomFloorSpecial(common, direction, target, change, model, crush);
        return true;
    }

    public static BoomFloorSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new ArgumentOutOfRangeException(nameof(special), special, "The linedef is not a generalized Boom floor special.");

        return specification;
    }
}
