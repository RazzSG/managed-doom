using System;

namespace ManagedDoom.Video
{
    public static class RenderMath
    {
        public static Fixed GetPlaneStep(Fixed distance, Fixed direction, Fixed centerXFrac)
        {
            return (distance * direction) / centerXFrac;
        }

        public static Fixed GetWallDistance(Fixed viewX, Fixed viewY, Fixed x1, Fixed y1, Fixed x2, Fixed y2)
        {
            var dx = (double)x2.Data - x1.Data;
            var dy = (double)y2.Data - y1.Data;
            var length = Math.Sqrt(dx * dx + dy * dy);

            if (length <= 0)
                return Fixed.Epsilon;

            var viewDx = viewX.Data - (double)x1.Data;
            var viewDy = viewY.Data - (double)y1.Data;
            var distance = Math.Abs((dy * viewDx - dx * viewDy) / length);

            return new Fixed((int)Math.Clamp(Math.Round(distance), 1.0, int.MaxValue));
        }
    }
}