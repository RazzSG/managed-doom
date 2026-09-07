namespace ManagedDoom.Compatibility.Boom.Scrolling;

public static class BoomScrollerVector
{
    public static (Fixed X, Fixed Y) ResolveWall(Fixed dx, Fixed dy, LineDef line)
    {
        var distance = Geometry.PointToDist(Fixed.Zero, Fixed.Zero, line.Dx, line.Dy);

        if (distance == Fixed.Zero)
            return (Fixed.Zero, Fixed.Zero);

        unchecked
        {
            var denominator = (long)distance.Data;
            var xNumerator =
                (long)dy.Data * -(long)line.Dy.Data -
                (long)dx.Data * line.Dx.Data;
            var yNumerator =
                (long)dy.Data * line.Dx.Data -
                (long)dx.Data * line.Dy.Data;

            return (
                new Fixed((int)(xNumerator / denominator)),
                new Fixed((int)(yNumerator / denominator)));
        }
    }
}
