using System;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Pushers;

public static class BoomPusherTranslator
{
    public const int PushMask = 0x200;

    private const int PushFactor = 7;
    private const int MomentumShift = Fixed.FracBits - PushFactor;

    public static (Fixed FullX, Fixed FullY, Fixed GroundX, Fixed GroundY) ResolveWind(Fixed dx, Fixed dy)
    {
        var xMagnitude = dx.Data >> Fixed.FracBits;
        var yMagnitude = dy.Data >> Fixed.FracBits;

        return (
            new Fixed(xMagnitude << MomentumShift),
            new Fixed(yMagnitude << MomentumShift),
            new Fixed((xMagnitude >> 1) << MomentumShift),
            new Fixed((yMagnitude >> 1) << MomentumShift));
    }

    public static (Fixed X, Fixed Y) ResolveCurrent(Fixed dx, Fixed dy)
    {
        var xMagnitude = dx.Data >> Fixed.FracBits;
        var yMagnitude = dy.Data >> Fixed.FracBits;

        return (
            new Fixed(xMagnitude << MomentumShift),
            new Fixed(yMagnitude << MomentumShift));
    }

    public static int ResolvePointMagnitude(Fixed dx, Fixed dy)
    {
        var xMagnitude = dx.Data >> Fixed.FracBits;
        var yMagnitude = dy.Data >> Fixed.FracBits;

        return AproxDistance(xMagnitude, yMagnitude);
    }

    public static Fixed ResolvePointSpeed(int magnitude, Fixed dx, Fixed dy)
    {
        var distance = Geometry.AproxDistance(dx, dy).Data >> Fixed.FracBits;
        var speed = magnitude - (distance >> 1);

        return new Fixed(speed << (Fixed.FracBits - PushFactor - 1));
    }

    public static Fixed ResolvePointSpeed(
        GameCompatibility compatibility,
        int magnitude,
        Fixed dx,
        Fixed dy)
    {
        return GameCompatibilityFeatures.SupportsMbf(compatibility)
            ? ResolveMbfPointSpeed(magnitude, dx, dy)
            : ResolvePointSpeed(magnitude, dx, dy);
    }

    public static Fixed ResolveMbfPointSpeed(int magnitude, Fixed dx, Fixed dy)
    {
        // MBF keeps Boom's original linear formula as the effective-radius gate,
        // then replaces the in-range strength with inverse-square falloff.
        if (ResolvePointSpeed(magnitude, dx, dy) <= Fixed.Zero)
            return Fixed.Zero;

        var x = dx.Data >> Fixed.FracBits;
        var y = dy.Data >> Fixed.FracBits;
        var denominator = ((long)x * x) + ((long)y * y) + 1;
        var speed = ((long)magnitude << 23) / denominator;

        return new Fixed(unchecked((int)speed));
    }

    private static int AproxDistance(int dx, int dy)
    {
        dx = Math.Abs(dx);
        dy = Math.Abs(dy);

        return dx < dy
            ? dx + dy - (dx >> 1)
            : dx + dy - (dy >> 1);
    }
}
