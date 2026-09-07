using System;

namespace ManagedDoom.Video;

/// <summary>
/// Linearly interpolates raw <see cref="Fixed"/> values across an integer number
/// of screen-space steps without discarding a sub-LSB slope.
/// </summary>
public struct FixedRangeInterpolator
{
    private long current;
    private readonly long quotient;
    private readonly long remainder;
    private long error;
    private readonly long divisor;

    public FixedRangeInterpolator(Fixed start, Fixed end, int steps, int offset = 0)
    {
        if (steps < 0)
            throw new ArgumentOutOfRangeException(nameof(steps));

        if (offset < 0 || offset > steps)
            throw new ArgumentOutOfRangeException(nameof(offset));

        current = start.Data;
        quotient = 0;
        remainder = 0;
        error = 0;
        divisor = steps;

        if (steps == 0)
            return;

        var delta = (long)end.Data - start.Data;
        quotient = delta / steps;
        remainder = delta % steps;

        if (offset == 0)
            return;

        current += quotient * offset;

        var remainderDistance = remainder * offset;
        current += remainderDistance / steps;
        error = remainderDistance % steps;
    }

    public readonly Fixed Value => new((int)current);

    public void Advance()
    {
        if (divisor == 0)
            return;

        current += quotient;
        error += remainder;

        if (error >= divisor)
        {
            current++;
            error -= divisor;
        }
        else if (error <= -divisor)
        {
            current--;
            error += divisor;
        }
    }
}
