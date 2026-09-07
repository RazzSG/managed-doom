namespace ManagedDoom.Compatibility.Boom.Friction;

public static class BoomFrictionTranslator
{
    public const int OriginalFrictionData = 0xe800;
    public const int OriginalMoveFactorData = 2048;
    public const int MoreFrictionMomentumData = 15000;
    public const int FrictionMask = 0x100;

    public static readonly Fixed OriginalFriction = new Fixed(OriginalFrictionData);
    public static readonly Fixed OriginalMoveFactor = new Fixed(OriginalMoveFactorData);

    public static (Fixed Friction, Fixed MoveFactor) Resolve(Fixed dx, Fixed dy)
    {
        var length = Geometry.AproxDistance(dx, dy).Data >> Fixed.FracBits;

        int friction;
        unchecked
        {
            friction = (0x1eb8 * length) / 0x80 + 0xd000;
        }

        int moveFactor;
        if (friction > OriginalFrictionData)
        {
            moveFactor = ((0x10092 - friction) * 0x70) / 0x158;
        }
        else
        {
            moveFactor = ((friction - 0xdb34) * 0x0a) / 0x80;
        }

        return (new Fixed(friction), new Fixed(moveFactor));
    }
}
