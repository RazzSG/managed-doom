using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManagedDoom;
using ManagedDoom.Video;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    public class RenderMathTest
    {
        [TestMethod]
        public void PlaneStepMultipliesBeforeDividing()
        {
            var distance = Fixed.FromInt(4096);
            var direction = Fixed.One;
            var centerX = Fixed.FromInt(640);

            var actual = RenderMath.GetPlaneStep(distance, direction, centerX);

            Assert.AreEqual(419430, actual.Data);
        }

        [TestMethod]
        public void PlaneStepDoesNotUseLegacyLowPrecisionOrder()
        {
            var distance = Fixed.FromInt(4096);
            var direction = Fixed.One;
            var centerX = Fixed.FromInt(640);

            var actual = RenderMath.GetPlaneStep(distance, direction, centerX);
            var legacy = distance * (direction / centerX);

            Assert.AreNotEqual(legacy.Data, actual.Data);
            Assert.AreEqual(417792, legacy.Data);
            Assert.AreEqual(419430, actual.Data);
        }

        [TestMethod]
        public void PlaneStepPreservesNegativeDirection()
        {
            var actual = RenderMath.GetPlaneStep(
                Fixed.FromInt(4096),
                -Fixed.One,
                Fixed.FromInt(640));

            Assert.AreEqual(-419430, actual.Data);
        }

        [TestMethod]
        public void WallDistanceIsPerpendicularDistance()
        {
            var distance = RenderMath.GetWallDistance(
                Fixed.FromInt(100),
                Fixed.FromInt(96),
                Fixed.FromInt(-5000),
                Fixed.FromInt(32),
                Fixed.FromInt(8000),
                Fixed.FromInt(32));

            Assert.AreEqual(Fixed.FromInt(64).Data, distance.Data);
        }

        [TestMethod]
        public void WallDistanceDoesNotDependOnEndpointOrder()
        {
            var viewX = Fixed.FromInt(1024);
            var viewY = Fixed.FromInt(-416);

            var a = RenderMath.GetWallDistance(
                viewX,
                viewY,
                Fixed.FromInt(-5000),
                Fixed.FromInt(-5000),
                Fixed.FromInt(8000),
                Fixed.FromInt(8000));

            var b = RenderMath.GetWallDistance(
                viewX,
                viewY,
                Fixed.FromInt(8000),
                Fixed.FromInt(8000),
                Fixed.FromInt(-5000),
                Fixed.FromInt(-5000));

            Assert.AreEqual(a.Data, b.Data);
        }

        [TestMethod]
        public void WallDistanceHandlesLongDiagonalWall()
        {
            var distance = RenderMath.GetWallDistance(
                Fixed.FromInt(100),
                Fixed.Zero,
                Fixed.FromInt(-5000),
                Fixed.FromInt(-5000),
                Fixed.FromInt(8000),
                Fixed.FromInt(8000));

            Assert.AreEqual(4634095, distance.Data);
        }

        [TestMethod]
        public void ZeroLengthWallReturnsEpsilon()
        {
            var distance = RenderMath.GetWallDistance(
                Fixed.Zero,
                Fixed.Zero,
                Fixed.FromInt(10),
                Fixed.FromInt(20),
                Fixed.FromInt(10),
                Fixed.FromInt(20));

            Assert.AreEqual(Fixed.Epsilon.Data, distance.Data);
        }
    }
}