using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using Trackbound.Core;

namespace Trackbound.Runtime;

public sealed class TerrainRenderer
{
    private const int NearTileSize = 128;
    private const int NearCells = 8;
    private const int NearRadius = 8;
    private const int FarSectors = 48;

    private readonly List<Triangle> _near = new();
    private readonly List<Triangle> _mid = new();
    private readonly List<Triangle> _far = new();
    private readonly List<Triangle> _horizon200km = new();

    private readonly List<Tree> _trees = new();
    private readonly List<GrassTuft> _grass = new();
    private readonly List<Boulder> _boulders = new();

    private int _seed;
    private int _renderDistanceKm = 50;
    private int _centerTileX = int.MinValue;
    private int _centerTileZ = int.MinValue;

    private readonly record struct Triangle(Vector3 A, Vector3 B, Vector3 C, Vector3 Normal, Color BaseColor);
    private readonly record struct Tree(Vector3 Ground, float Height, Color Foliage, int Type, int Tiers);
    private readonly record struct GrassTuft(Vector3 Position, float Size, Color Color);
    private readonly record struct Boulder(Vector3 Position, float Radius, Color Color);

    public int Seed => _seed;
    public int RenderDistanceKm => _renderDistanceKm;

    public TerrainRenderer(int seed, int renderDistanceKm = 50)
    {
        _seed = seed;
        _renderDistanceKm = renderDistanceKm;
    }

    public void SetWorld(int seed, int renderDistanceKm)
    {
        _seed = seed;
        _renderDistanceKm = renderDistanceKm;
        _centerTileX = int.MinValue;
        _centerTileZ = int.MinValue;
        _near.Clear();
        _mid.Clear();
        _far.Clear();
        _horizon200km.Clear();
        _trees.Clear();
        _grass.Clear();
        _boulders.Clear();
    }

    public void SetRenderDistance(int renderDistanceKm)
    {
        if (_renderDistanceKm != renderDistanceKm)
        {
            _renderDistanceKm = renderDistanceKm;
            _centerTileX = int.MinValue;
            _centerTileZ = int.MinValue;
        }
    }

    public void Draw(Vector3 focus, LightingContext lighting)
    {
        var tileX = (int)MathF.Floor(focus.X / NearTileSize);
        var tileZ = (int)MathF.Floor(focus.Z / NearTileSize);
        if (tileX != _centerTileX || tileZ != _centerTileZ)
        {
            BuildVisibleTerrain(tileX, tileZ);
            _centerTileX = tileX;
            _centerTileZ = tileZ;
        }

        // Draw from far to near for depth and haze layering
        DrawTriangles(_horizon200km, focus, true, lighting, 200_000f);
        DrawTriangles(_far, focus, true, lighting, _renderDistanceKm * 1000f);
        DrawTriangles(_mid, focus, true, lighting, _renderDistanceKm * 400f);
        DrawTriangles(_near, focus, false, lighting, 1_000f);

        // Draw natural world details
        DrawBoulders(focus, lighting);
        DrawGrassTufts(focus, lighting);
        DrawTrees(focus, lighting);
    }

    private void BuildVisibleTerrain(int tileX, int tileZ)
    {
        _near.Clear();
        _mid.Clear();
        _far.Clear();
        _horizon200km.Clear();
        _trees.Clear();
        _grass.Clear();
        _boulders.Clear();

        // 1. High-detail near grid (0 to 1 km radius)
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
                AddNearVegetationAndDetails(x, z, tileX, tileZ, tx, tz);
            }
        }

        // 2. Mid & Far radial concentric rings based on user Render Distance
        var farCenterX = (tileX + 0.5f) * NearTileSize;
        var farCenterZ = (tileZ + 0.5f) * NearTileSize;
        var sectorWidth = MathF.Tau / FarSectors;

        var midOuter = Math.Max(3_500f, _renderDistanceKm * 280f);
        var farOuter = Math.Max(10_000f, _renderDistanceKm * 1000f);

        // Mid-range terrain ring
        for (var sector = 0; sector < FarSectors; sector++)
        {
            var startAngle = sector * sectorWidth;
            AddRadialPatch(_mid, 1_000f, midOuter, startAngle, startAngle + sectorWidth, 3, 2, farCenterX, farCenterZ);
            if (sector % 2 == 0)
            {
                AddMidTree((1_000f + midOuter) * 0.5f, startAngle + sectorWidth * 0.5f, sector, tileX, tileZ, farCenterX, farCenterZ);
            }
        }

        // Far mountain ranges ring (up to user render distance)
        for (var sector = 0; sector < FarSectors; sector++)
        {
            var startAngle = sector * sectorWidth;
            AddRadialPatch(_far, midOuter, farOuter, startAngle, startAngle + sectorWidth, 2, 2, farCenterX, farCenterZ);
        }

        // 3. Grand 200 km Continental Horizon Ring (extends out to 200,000 meters)
        // Colossal alpine massifs rising thousands of meters above the distant atmospheric haze
        var horizonInner = farOuter;
        var horizonOuter = 200_000f; // 200 km
        for (var sector = 0; sector < FarSectors; sector++)
        {
            var startAngle = sector * sectorWidth;
            AddRadialPatch(_horizon200km, horizonInner, horizonOuter, startAngle, startAngle + sectorWidth, 2, 2, farCenterX, farCenterZ);
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
                AddTriangle(target, a, c, b);
                AddTriangle(target, b, c, d);
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
                vertices[r, a] = new Vector3(x, Height(x, z) - 3f, z);
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
                AddTriangle(target, v0, v2, v1);
                AddTriangle(target, v2, v3, v1);
            }
        }
    }

    private void AddTriangle(List<Triangle> target, Vector3 a, Vector3 b, Vector3 c)
    {
        var height = (a.Y + b.Y + c.Y) / 3f;
        var baseColor = TerrainColor(height);
        var ab = Subtract(b, a);
        var ac = Subtract(c, a);
        var normal = Normalize(Cross(ab, ac));
        target.Add(new Triangle(a, b, c, normal, baseColor));
    }

    private void AddNearVegetationAndDetails(int startX, int startZ, int centerX, int centerZ, int tileX, int tileZ)
    {
        var random = new Random(unchecked(_seed + (centerX + tileX) * 73856093 ^ (centerZ + tileZ) * 19349663));

        // 1. Detailed Multi-Tiered Trees
        for (var tree = 0; tree < 6; tree++)
        {
            var x = startX + (float)random.NextDouble() * NearTileSize;
            var z = startZ + (float)random.NextDouble() * NearTileSize;
            if (MathF.Abs(x) < 14f) continue; // Keep track corridor clear

            var ground = new Vector3(x, Height(x, z), z);
            var height = 10f + (float)random.NextDouble() * 18f;
            var treeType = random.Next(0, 4); // 0, 1, 2 = conifer varieties, 3 = alpine birch
            var tiers = random.Next(3, 5);

            Color foliage = treeType switch
            {
                0 => new Color(34, 76, 58, 255),  // Deep spruce
                1 => new Color(48, 92, 54, 255),  // Alpine pine
                2 => new Color(135, 112, 48, 255), // Autumn larch
                _ => new Color(76, 124, 62, 255)   // Birch foliage
            };

            _trees.Add(new Tree(ground, height, foliage, treeType, tiers));
        }

        // 2. Procedural 3D Grass Tufts along rail embankment and meadows
        for (var g = 0; g < 12; g++)
        {
            var gx = startX + (float)random.NextDouble() * NearTileSize;
            var gz = startZ + (float)random.NextDouble() * NearTileSize;
            if (MathF.Abs(gx) < 3.2f) continue; // Off the steel rails

            var gy = Height(gx, gz);
            var size = 0.55f + (float)random.NextDouble() * 0.75f;
            var shade = random.Next(-18, 18);
            var grassColor = new Color(78 + shade, 126 + shade, 54 + shade, 255);
            _grass.Add(new GrassTuft(new Vector3(gx, gy, gz), size, grassColor));
        }

        // 3. Scattered Boulders & Field Stones
        for (var b = 0; b < 2; b++)
        {
            var bx = startX + (float)random.NextDouble() * NearTileSize;
            var bz = startZ + (float)random.NextDouble() * NearTileSize;
            if (MathF.Abs(bx) < 6.5f) continue;

            var by = Height(bx, bz);
            var radius = 0.65f + (float)random.NextDouble() * 1.4f;
            var shade = random.Next(-15, 20);
            var rockColor = new Color(102 + shade, 105 + shade, 98 + shade, 255);
            _boulders.Add(new Boulder(new Vector3(bx, by + radius * 0.4f, bz), radius, rockColor));
        }
    }

    private void AddMidTree(float radius, float angle, int sector, int tileX, int tileZ, float centerX, float centerZ)
    {
        var x = centerX + MathF.Cos(angle) * radius;
        var z = centerZ + MathF.Sin(angle) * radius;
        if (MathF.Abs(x) < 20f) return;

        var random = new Random(unchecked(_seed * 31 + tileX * 374761393 + tileZ * 668265263 + sector * 104729));
        var height = 24f + 48f * (float)random.NextDouble();
        var foliage = new Color(42, 78 + random.Next(0, 22), 60, 255);
        _trees.Add(new Tree(new Vector3(x, Height(x, z), z), height, foliage, 0, 3));
    }

    private void DrawTriangles(List<Triangle> triangles, Vector3 focus, bool haze, LightingContext lighting, float maxDist)
    {
        foreach (var triangle in triangles)
        {
            var color = LightingEngine.ShadeSurface(triangle.BaseColor, triangle.Normal, triangle.A, lighting, 0.9f, 0.0f);
            if (haze)
            {
                var centerX = (triangle.A.X + triangle.B.X + triangle.C.X) / 3f;
                var centerZ = (triangle.A.Z + triangle.B.Z + triangle.C.Z) / 3f;
                var distance = MathF.Sqrt(Sqr(centerX - focus.X) + Sqr(centerZ - focus.Z));
                var fog = SmoothStep(2_500f, maxDist * 0.95f, distance);
                color = LightingEngine.ColorLerp(color, lighting.FogColor, fog * 0.92f);
            }
            Raylib.DrawTriangle3D(triangle.A, triangle.B, triangle.C, color);
        }
    }

    private void DrawBoulders(Vector3 focus, LightingContext lighting)
    {
        foreach (var rock in _boulders)
        {
            var dx = rock.Position.X - focus.X;
            var dz = rock.Position.Z - focus.Z;
            if (dx * dx + dz * dz > 250_000f) continue; // 500m cull

            var litColor = LightingEngine.ShadeSurface(rock.Color, Vector3.UnitY, rock.Position, lighting, 0.8f, 0.1f);
            Raylib.DrawSphere(rock.Position, rock.Radius, litColor);
        }
    }

    private void DrawGrassTufts(Vector3 focus, LightingContext lighting)
    {
        var wind = MathF.Sin(lighting.TimeOfDay * 1.4f) * 0.12f;
        foreach (var tuft in _grass)
        {
            var dx = tuft.Position.X - focus.X;
            var dz = tuft.Position.Z - focus.Z;
            if (dx * dx + dz * dz > 360_000f) continue; // 600m cull

            var litColor = LightingEngine.ShadeSurface(tuft.Color, Vector3.UnitY, tuft.Position, lighting, 0.9f, 0f);
            var pos = tuft.Position;
            var h = tuft.Size;
            var w = tuft.Size * 0.5f;

            // 2 crossed triangular blades creating 3D grass tuft
            var tipA = new Vector3(pos.X + wind, pos.Y + h, pos.Z);
            var b1 = new Vector3(pos.X - w, pos.Y, pos.Z);
            var b2 = new Vector3(pos.X + w, pos.Y, pos.Z);
            Raylib.DrawTriangle3D(b1, tipA, b2, litColor);

            var tipB = new Vector3(pos.X, pos.Y + h, pos.Z + wind);
            var c1 = new Vector3(pos.X, pos.Y, pos.Z - w);
            var c2 = new Vector3(pos.X, pos.Y, pos.Z + w);
            Raylib.DrawTriangle3D(c1, tipB, c2, litColor);
        }
    }

    private void DrawTrees(Vector3 focus, LightingContext lighting)
    {
        var wind = MathF.Sin(lighting.TimeOfDay * 0.8f) * 0.28f;
        foreach (var tree in _trees)
        {
            var dx = tree.Ground.X - focus.X;
            var dz = tree.Ground.Z - focus.Z;
            var distanceSquared = dx * dx + dz * dz;
            if (distanceSquared > 121_000_000f) continue;

            var distance = MathF.Sqrt(distanceSquared);
            var basePoint = tree.Ground;
            var isBirch = tree.Type == 3;

            // Trunk coloring
            Color baseTrunk = isBirch
                ? new Color(222, 218, 206, 255) // Pale birch bark
                : (distance > 1_000 ? new Color(63, 73, 69, 255) : new Color(82, 65, 48, 255)); // Pine bark

            var trunkColor = LightingEngine.ShadeSurface(baseTrunk, Vector3.UnitY, basePoint, lighting, 0.85f, 0f);
            var foliageColor = LightingEngine.ShadeSurface(tree.Foliage, Vector3.UnitY, basePoint, lighting, 0.8f, 0f);

            if (distance > 2_500f)
            {
                var fog = SmoothStep(2_500f, 11_000f, distance);
                trunkColor = LightingEngine.ColorLerp(trunkColor, lighting.FogColor, fog * 0.88f);
                foliageColor = LightingEngine.ColorLerp(foliageColor, lighting.FogColor, fog * 0.88f);
            }

            var lean = wind * Math.Clamp(tree.Height / 30f, 0.2f, 1f);
            var tip = new Vector3(basePoint.X + lean, basePoint.Y + tree.Height, basePoint.Z);

            // Trunk
            var trunkRadius = MathF.Max(0.18f, tree.Height * 0.024f);
            Raylib.DrawCylinderEx(basePoint, tip, trunkRadius, 0.06f, 6, trunkColor);

            if (isBirch)
            {
                // Deciduous Birch rounded puff canopy
                var crownY = basePoint.Y + tree.Height * 0.65f;
                var puffPos = new Vector3(basePoint.X + lean, crownY, basePoint.Z);
                Raylib.DrawSphere(puffPos, tree.Height * 0.35f, foliageColor);
                Raylib.DrawSphere(new Vector3(puffPos.X + 0.3f, crownY + tree.Height * 0.2f, puffPos.Z), tree.Height * 0.24f, foliageColor);
            }
            else
            {
                // Multi-tiered Conifer (3 to 4 conical needle layers)
                var tiers = Math.Clamp(tree.Tiers, 3, 5);
                var tierStart = tree.Height * 0.35f;
                var tierStep = (tree.Height - tierStart) / tiers;

                for (var t = 0; t < tiers; t++)
                {
                    var bottomY = basePoint.Y + tierStart + t * tierStep;
                    var topY = bottomY + tierStep * 1.35f;
                    var tierWidth = (tree.Height * 0.28f) * (1f - (float)t / tiers * 0.65f);

                    var bPos = new Vector3(basePoint.X + lean * ((float)t / tiers), bottomY, basePoint.Z);
                    var tPos = new Vector3(basePoint.X + lean * ((float)(t + 1) / tiers), topY, basePoint.Z);

                    Raylib.DrawCylinderEx(bPos, tPos, tierWidth, 0.02f, 6, foliageColor);
                }
            }
        }
    }

    // Natural route civil engineering: gentle undulating railway grade + cut/fill slopes
    // Mountains now rise naturally in front, far away, across valleys, and in all directions!
    public float Height(float x, float z)
    {
        var rawTerrain = (float)ProceduralTerrain.Height(x, z, _seed);

        // Railway grade: long gentle rolling gradient (spanning kilometers)
        var railBed = RailGrade(z, _seed);

        var absX = MathF.Abs(x);

        // 1. Within 4.5m: flat railway ballast bed
        if (absX <= 4.5f)
        {
            return railBed;
        }

        // 2. From 4.5m to 24m: engineered cut or fill embankment slope transitioning into wilderness
        if (absX <= 24f)
        {
            var t = SmoothStep(4.5f, 24f, absX);
            return Lerp(railBed, rawTerrain, t);
        }

        // 3. Beyond 24m: pure unconstrained natural geography! Mountains appear anywhere!
        return rawTerrain;
    }

    public static float RailGrade(float z, int seed)
    {
        // Gentle undulating railway elevation spanning kilometers
        var w1 = MathF.Sin(z * 0.00018f) * 35f;
        var w2 = MathF.Sin(z * 0.00052f + 1.2f) * 14f;
        return w1 + w2;
    }

    public float HeightAt(float worldX, float worldZ) => Height(worldX, worldZ);

    private static Color TerrainColor(float height)
    {
        if (height > 1_600)
        {
            return LightingEngine.ColorLerp(new Color(98, 100, 91, 255), new Color(226, 226, 216, 255), SmoothStep(1_600, 2_200, height));
        }
        if (height > 420)
        {
            return LightingEngine.ColorLerp(new Color(72, 83, 67, 255), new Color(111, 113, 101, 255), SmoothStep(420, 1_500, height));
        }
        return LightingEngine.ColorLerp(new Color(57, 78, 61, 255), new Color(93, 103, 78, 255), SmoothStep(-80, 420, height));
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
}