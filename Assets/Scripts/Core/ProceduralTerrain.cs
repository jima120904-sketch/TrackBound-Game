using System;

namespace Trackbound.Core
{
    public static class ProceduralTerrain
    {
        public static double Height(double worldX, double worldZ, int worldSeed)
        {
            var broad = ValueNoise(worldX * 0.00022, worldZ * 0.00022, worldSeed) * 2 - 1;
            var rangeMask = SmoothStep(0.38, 0.72,
                ValueNoise(worldX * 0.00048, worldZ * 0.00048, worldSeed + 19));
            var ridgeNoise = ValueNoise(worldX * 0.0011, worldZ * 0.0011, worldSeed + 37) * 2 - 1;
            var ridges = Math.Pow(1 - Math.Abs(ridgeNoise), 1.35);
            var detail = ValueNoise(worldX * 0.0028, worldZ * 0.0028, worldSeed + 71) * 2 - 1;

            return broad * 120 + rangeMask * (90 + ridges * 2100) + detail * 22;
        }

        private static double ValueNoise(double x, double y, int seed)
        {
            var x0 = (int)Math.Floor(x);
            var y0 = (int)Math.Floor(y);
            var tx = Fade(x - x0);
            var ty = Fade(y - y0);

            var a = Lerp(Hash(x0, y0, seed), Hash(x0 + 1, y0, seed), tx);
            var b = Lerp(Hash(x0, y0 + 1, seed), Hash(x0 + 1, y0 + 1, seed), tx);
            return Lerp(a, b, ty);
        }

        private static double Hash(int x, int y, int seed)
        {
            unchecked
            {
                var value = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                value = (value ^ (value >> 13)) * 1274126177;
                value ^= value >> 16;
                return value / (double)uint.MaxValue;
            }
        }

        private static double Fade(double value)
        {
            return value * value * value * (value * (value * 6 - 15) + 10);
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        private static double SmoothStep(double edge0, double edge1, double value)
        {
            var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
            return t * t * (3 - 2 * t);
        }
    }
}