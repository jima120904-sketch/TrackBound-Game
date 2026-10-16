using Raylib_cs;
using System.Numerics;
using Trackbound.Core;

namespace Trackbound.Runtime;

public sealed class TerrainRenderer
{
    private const int NearTileSize = 128;
    private const int NearCells = 8;
    private const int NearRadius = 8;
    private const int FarSectors = 40;
    private readonly List<Triangle> _near = new();
    private readonly List<Triangle> _far = new();
    private readonly List<Tree> _trees = new();
    private readonly int _seed;
    private int _centerTileX = int.MinValue;
    private int _centerTileZ = int.MinValue;

    private readonly record struct Triangle(Vector3 A, Vector3 B, Vector3 C, Color Tint);
    private readonly record struct Tree(Vector3 Ground, float Height, Color Foliage);

    public TerrainRenderer(int seed)
    {
        _seed = seed;
    }

    public void Draw(Vector3 focus, float timeOfDay)
    {
        var tileX = (int)MathF.Floor(focus.X / NearTileSize);
        var tileZ = (int)MathF.Floor(focus.Z / NearTileSize);
        if (tileX != _centerTileX || tileZ != _centerTileZ)
        {
            BuildVisibleTerrain(tileX, tileZ);
            _centerTileX = tileX;
            _centerTileZ = tileZ;
        }

        DrawTriangles(_far, focus, true);
        DrawTriangles(_near, focus, false);
        DrawTrees(focus, timeOfDay);
    }

    private void BuildVisibleTerrain(int tileX, int tileZ)
    {
        _near.Clear();
        _far.Clear();
        _trees.Clear();

        for (var tz = -NearRadius; tz <= NearRadius; tz++)
        {
            for (var tx = -NearRadius; tx <= NearRadius; tx++)
            {
                var x = (tileX + tx) * NearTileSize;
                var z = (tileZ + tz) * NearTileSize;
                var nearestX = Math.Clamp(0, x - (int)(tileX * NearTileSize), x + NearTileSize - (int)(tileX * NearTileSize));
                var nearestZ = Math.Clamp(0, z - (int)(tileZ * NearTileSize), z + NearTileSize - (int)(tileZ * NearTileSize));
                if (nearestX * nearestX + nearestZ * nearestZ > 1_000_000)
                {
                    continue;
                }

                AddGridPatch(_near, x, z, NearTileSize, NearCells, 0, 0);
                AddNearTrees(x, z, tileX, tileZ, tx, tz);
            }
        }

        var rings = new[] { 1_000f, 2_200f, 4_200f, 7_400f, 11_000f };
        var farCenterX = (tileX + 0.5f) * NearTileSize;
        var farCenterZ = (tileZ + 0.5f) * NearTileSize;
        var sectorWidth = MathF.Tau / FarSectors;
        for (var ring = 0; ring < rings.Length - 1; ring++)
        {
            var inner = rings[ring];
            var outer = rings[ring + 1];
            var radialSteps = ring < 2 ? 3 : 2;
            var angleSteps = 2;
            for (var sector = 0; sector < FarSectors; sector++)
            {
                var startAngle = sector * sectorWidth;
                AddRadialPatch(_far, inner, outer, startAngle, startAngle + sectorWidth, radialSteps, angleSteps, farCenterX, farCenterZ);
                if (ring >= 2 && sector % 2 == 0)
                {
                    AddFarTree((inner + outer) * 0.5f, startAngle + sectorWidth * 0.5f, ring, sector, tileX, tileZ, farCenterX, farCenterZ);
                }
            }
        }
    }

    private void AddGridPatch(List<Triangle> target, float startX, float startZ, float size, int cells, float offsetX, float offsetZ)
    {
        var step = size / cells;
        var vertices = new Vector3[(cells + 1) * (cells + 1)];
        for (var z = 0; z <= cells; z++)
        {
            for (var x = 0; x <= cells; x++)
            {
                var worldX = startX + x * step;
                var worldZ = startZ + z * step;
                vertices[z * (cells + 1) + x] = new Vector3(worldX + offsetX, Height(worldX, worldZ) + 0.04f, worldZ + offsetZ);
            }
        }

        for (var z = 0; z < cells; z++)
        {
            for (var x = 0; x < cells; x++)
            {
                var a = vertices[z * (cells + 1) + x];
                var b = vertices[z * (cells + 1) + x + 1];
                var c = vertices[(z + 1) * (cells + 1) + x];
                var d = vertices[(z + 1) * (cells + 1) + x + 1];
                AddLitTriangle(target, a, c, b);
                AddLitTriangle(target, b, c, d);
            }
        }
    }

    private void AddRadialPatch(List<Triangle> target, float inner, float outer, float startAngle, float endAngle, int radialSteps, int angleSteps, float centerX, float centerZ)
    {
        var vertices = new Vector3[radialSteps + 1, angleSteps + 1];
        for (var r = 0; r <= radialSteps; r++)
        {
            var radius = Lerp(inner, outer, r / (float)radialSteps);
            for (var a = 0; a <= angleSteps; a++)
            {
                var angle = Lerp(startAngle, endAngle, a / (float)angleSteps);
                var x = centerX + MathF.Cos(angle) * radius;
                var z = centerZ + MathF.Sin(angle) * radius;
                vertices[r, a] = new Vector3(x, Height(x, z) - 3f, z); // sit slightly below the near terrain so the two layers don't z-fight
            }
        }

        for (var r = 0; r < radialSteps; r++)
        {
            for (var a = 0; a < angleSteps; a++)
            {
                var v0 = vertices[r, a];
                var v1 = vertices[r + 1, a];
                var v2 = vertices[r, a + 1];
                var v3 = vertices[r + 1, a + 1];
                // Counter-clockwise when seen from above (the old order faced down and was backface-culled).
                AddLitTriangle(target, v0, v2, v1);
                AddLitTriangle(target, v2, v3, v1);
            }
        }
    }

    private void AddLitTriangle(List<Triangle> target, Vector3 a, Vector3 b, Vector3 c)
    {
        var height = (a.Y + b.Y + c.Y) / 3f;
        var baseColor = TerrainColor(height);
        var tint = Shade(baseColor, a, b, c);
        target.Add(new Triangle(a, b, c, tint));
    }

    private void AddNearTrees(int startX, int startZ, int centerX, int centerZ, int tileX, int tileZ)
    {
        var random = new Random(unchecked(_seed + (centerX + tileX) * 73856093 ^ (centerZ + tileZ) * 19349663));
        for (var tree = 0; tree < 5; tree++)
        {
            var x = startX + (float)random.NextDouble() * NearTileSize;
            var z = startZ + (float)random.NextDouble() * NearTileSize;
            if (MathF.Abs(x) < 25f)
            {
                continue;
            }
            var ground = new Vector3(x, Height(x, z), z);
            var height = 9f + (float)random.NextDouble() * 17f;
            var shade = random.Next(0, 28);
            _trees.Add(new Tree(ground, height, new Color(35 + shade, 74 + shade, 61 + shade, 255)));
        }
    }

    private void AddFarTree(float radius, float angle, int ring, int sector, int tileX, int tileZ, float centerX, float centerZ)
    {
        var x = centerX + MathF.Cos(angle) * radius;
        var z = centerZ + MathF.Sin(angle) * radius;
        var random = new Random(unchecked(_seed * 31 + tileX * 374761393 + tileZ * 668265263 + ring * 7919 + sector * 104729));
        var height = 28f + 62f * (float)random.NextDouble();
        _trees.Add(new Tree(new Vector3(x, Height(x, z), z), height,
            new Color(47, 69 + random.Next(0, 24), 65, 255)));
    }

    private void DrawTriangles(List<Triangle> triangles, Vector3 focus, bool haze)
    {
        foreach (var triangle in triangles)
        {
            var color = triangle.Tint;
            if (haze)
            {
                var centerX = (triangle.A.X + triangle.B.X + triangle.C.X) / 3f;
                var centerZ = (triangle.A.Z + triangle.B.Z + triangle.C.Z) / 3f;
                var distance = MathF.Sqrt(Sqr(centerX - focus.X) + Sqr(centerZ - focus.Z));
                var fog = SmoothStep(2_700f, 11_000f, distance);
                color = ColorLerp(color, new Color(133, 155, 161, 255), fog * 0.9f);
            }
            Raylib.DrawTriangle3D(triangle.A, triangle.B, triangle.C, color);
        }
    }

    private void DrawTrees(Vector3 focus, float timeOfDay)
    {
        var wind = MathF.Sin(timeOfDay * 0.7f) * 0.25f;
        foreach (var tree in _trees)
        {
            var dx = tree.Ground.X - focus.X;
            var dz = tree.Ground.Z - focus.Z;
            var distanceSquared = dx * dx + dz * dz;
            if (distanceSquared > 121_000_000f)
            {
                continue;
            }

            var distance = MathF.Sqrt(distanceSquared);
            var trunkColor = distance > 1_000 ? new Color(63, 73, 69, 255) : new Color(82, 65, 48, 255);
            var lean = wind * Math.Clamp(tree.Height / 30f, 0.2f, 1f);
            var basePoint = tree.Ground;
            var tip = new Vector3(basePoint.X + lean, basePoint.Y + tree.Height, basePoint.Z);
            Raylib.DrawCylinderEx(basePoint, tip, MathF.Max(0.15f, tree.Height * 0.022f), 0.08f, 5, trunkColor);

            var crown = new Vector3(basePoint.X + lean, basePoint.Y + tree.Height * 0.57f, basePoint.Z);
            var crownHeight = tree.Height * 0.58f;
            if (distance > 2_500)
            {
                crownHeight *= 1.25f;
            }
            Raylib.DrawCylinderEx(crown, new Vector3(crown.X + lean, crown.Y + crownHeight, crown.Z),
                tree.Height * 0.22f, 0f, 5, tree.Foliage);
        }
    }

    private float Height(float x, float z)
    {
        var generated = (float)ProceduralTerrain.Height(x, z, _seed);
        var trackBlend = SmoothStep(18, 420, MathF.Abs(x));
        return generated * trackBlend;
    }

    public float HeightAt(float worldX, float worldZ) => Height(worldX, worldZ);

    private static Color TerrainColor(float height)
    {
        if (height > 1_600)
        {
            return ColorLerp(new Color(98, 100, 91, 255), new Color(226, 226, 216, 255), SmoothStep(1_600, 2_200, height));
        }
        if (height > 420)
        {
            return ColorLerp(new Color(72, 83, 67, 255), new Color(111, 113, 101, 255), SmoothStep(420, 1_500, height));
        }
        return ColorLerp(new Color(57, 78, 61, 255), new Color(93, 103, 78, 255), SmoothStep(-80, 420, height));
    }

    private static Color Shade(Color color, Vector3 a, Vector3 b, Vector3 c)
    {
        var ab = Subtract(b, a);
        var ac = Subtract(c, a);
        var normal = Normalize(Cross(ab, ac));
        var sun = Normalize(new Vector3(-0.42f, 0.86f, -0.28f));
        var diffuse = MathF.Max(0, Dot(normal, sun));
        return ColorLerp(new Color(39, 47, 48, 255), color, 0.48f + diffuse * 0.54f);
    }

    private static Vector3 Subtract(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    private static Vector3 Cross(Vector3 a, Vector3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    private static float Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    private static Vector3 Normalize(Vector3 value)
    {
        var length = MathF.Sqrt(Dot(value, value));
        return length > 0 ? new Vector3(value.X / length, value.Y / length, value.Z / length) : new Vector3(0, 1, 0);
    }
    private static float Sqr(float value) => value * value;
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    private static float SmoothStep(float a, float b, float value)
    {
        var t = Math.Clamp((value - a) / (b - a), 0, 1);
        return t * t * (3 - 2 * t);
    }
    private static Color ColorLerp(Color a, Color b, float t) => new(
        (byte)Math.Clamp((int)Lerp(a.R, b.R, t), 0, 255),
        (byte)Math.Clamp((int)Lerp(a.G, b.G, t), 0, 255),
        (byte)Math.Clamp((int)Lerp(a.B, b.B, t), 0, 255),
        (byte)255);
}