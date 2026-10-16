using Raylib_cs;
using System.Numerics;

namespace Trackbound.Runtime;

public sealed class TrainRenderer
{
    private static readonly Color Paint = new(42, 77, 68, 255);
    private static readonly Color DarkSteel = new(45, 52, 54, 255);
    private static readonly Color WornSteel = new(115, 120, 114, 255);
    private static readonly Color Brass = new(198, 148, 62, 255);
    private static readonly Color Timber = new(108, 76, 52, 255);
    private static readonly Color Glass = new(92, 138, 148, 255);

    public void Draw(float trainZ, float speed, LightingContext lighting)
    {
        DrawTrack(trainZ, lighting);
        DrawLocomotive(trainZ, lighting);
        DrawCarriage(trainZ - 14, "Cargo car", new Color(95, 80, 58, 255), false, lighting);
        DrawCarriage(trainZ - 29, "Workshop car", new Color(58, 82, 76, 255), true, lighting);
    }

    private static void DrawTrack(float trainZ, LightingContext lighting)
    {
        var start = (int)MathF.Floor((trainZ - 180) / 4) * 4;
        for (var z = start; z < trainZ + 420; z += 4)
        {
            LitCube(new Vector3(0, 0.12f, z), 3.15f, 0.2f, 0.28f, Timber, lighting, 0.85f, 0.0f);
        }
        // Steel Rails
        LitCube(new Vector3(-0.76f, 0.38f, trainZ + 120), 0.15f, 0.24f, 600, WornSteel, lighting, 0.35f, 0.65f);
        LitCube(new Vector3(0.76f, 0.38f, trainZ + 120), 0.15f, 0.24f, 600, WornSteel, lighting, 0.35f, 0.65f);
    }

    private static void DrawLocomotive(float z, LightingContext lighting)
    {
        // Heavy Chassis Underframe
        LitCube(new Vector3(0, 1.25f, z), 3.3f, 0.7f, 14.2f, DarkSteel, lighting, 0.6f, 0.4f);

        // Cylindrical Boiler with Paint & Brass Trim Bands
        var boilerStart = new Vector3(0, 2.7f, z - 0.3f);
        var boilerEnd = new Vector3(0, 2.7f, z + 4.3f);
        Raylib.DrawCylinderEx(boilerStart, boilerEnd, 1.1f, 1.1f, 16,
            LightingEngine.ShadeSurface(Paint, Vector3.UnitY, boilerStart, lighting, 0.45f, 0.25f));

        // Steam Dome (Brass) & Smokestack (Cast Iron)
        LitCylinder(new Vector3(0, 2.72f, z + 0.4f), 0.44f, 0.92f, Brass, lighting, 0.25f, 0.85f);
        LitCylinder(new Vector3(0, 2.7f, z + 4.1f), 0.36f, 1.1f, DarkSteel, lighting, 0.75f, 0.2f);

        // Front Cowcatcher / Pilot Frame & Rear Buffer Beam
        LitCube(new Vector3(0, 1.1f, z + 7.1f), 3.4f, 0.5f, 0.36f, Paint, lighting, 0.5f, 0.3f);
        LitCube(new Vector3(0, 1.1f, z - 7.1f), 3.4f, 0.5f, 0.36f, Paint, lighting, 0.5f, 0.3f);

        // Cab Floor & Roof
        LitCube(new Vector3(0, 1.77f, z - 5.2f), 3.12f, 0.2f, 5.25f, Timber, lighting, 0.8f, 0.0f);
        LitCube(new Vector3(0, 5.27f, z - 5.2f), 3.35f, 0.22f, 5.5f, Paint, lighting, 0.45f, 0.3f);
        LitCube(new Vector3(0, 3.9f, z - 7.8f), 3.12f, 2.55f, 0.17f, Paint, lighting, 0.5f, 0.2f);

        // Cab Side Walls
        LitCube(new Vector3(-1.53f, 2.47f, z - 5.2f), 0.17f, 1.2f, 5.1f, Paint, lighting, 0.5f, 0.25f);
        LitCube(new Vector3(1.53f, 2.47f, z - 5.2f), 0.17f, 1.2f, 5.1f, Paint, lighting, 0.5f, 0.25f);

        // Windows & Window Frames
        foreach (var side in new[] { -1, 1 })
        {
            LitCube(new Vector3(side * 1.54f, 4.13f, z - 5.2f), 0.08f, 0.98f, 5.0f, Glass, lighting, 0.15f, 0.1f);
            LitCube(new Vector3(side * 1.54f, 4.13f, z - 7.62f), 0.12f, 1.1f, 0.12f, Brass, lighting, 0.25f, 0.85f);
            LitCube(new Vector3(side * 1.54f, 4.13f, z - 6.0f), 0.12f, 1.1f, 0.12f, Brass, lighting, 0.25f, 0.85f);
            LitCube(new Vector3(side * 1.54f, 4.13f, z - 4.4f), 0.12f, 1.1f, 0.12f, Brass, lighting, 0.25f, 0.85f);
            LitCube(new Vector3(side * 1.54f, 4.13f, z - 2.8f), 0.12f, 1.1f, 0.12f, Brass, lighting, 0.25f, 0.85f);
            LitCube(new Vector3(side * 1.7f, 1.64f, z - 0.5f), 0.23f, 0.14f, 10.5f, DarkSteel, lighting, 0.6f, 0.4f);
        }

        // Driver Dashboard Console
        LitCube(new Vector3(0, 2.47f, z - 3.25f), 2.6f, 0.57f, 0.58f, Timber, lighting, 0.75f, 0.0f);
        LitCube(new Vector3(0, 2.9f, z - 3.48f), 2.48f, 0.35f, 0.16f, DarkSteel, lighting, 0.45f, 0.6f);

        // Storage Locker & Tool Chest
        LitCube(new Vector3(1.05f, 2.2f, z - 6.6f), 0.95f, 0.72f, 1.15f, Timber, lighting, 0.8f, 0.0f);
        LitCube(new Vector3(1.05f, 2.62f, z - 6.6f), 0.99f, 0.12f, 1.18f, WornSteel, lighting, 0.35f, 0.6f);

        // Control Levers (Throttle & Brake)
        LitLever(new Vector3(-0.75f, 3.0f, z - 3.0f), Brass, lighting, 0.2f, 0.9f);
        LitLever(new Vector3(0.72f, 3.0f, z - 3.0f), WornSteel, lighting, 0.35f, 0.65f);

        // Firebox & Hearth Area
        LitCube(new Vector3(-0.96f, 2.05f, z - 6.85f), 0.82f, 0.56f, 1.4f, DarkSteel, lighting, 0.7f, 0.3f);
        LitCube(new Vector3(-0.15f, 2.35f, z - 4.05f), 0.65f, 0.62f, 0.12f, WornSteel, lighting, 0.4f, 0.5f);
        LitCube(new Vector3(-0.9f, 2.02f, z - 4.0f), 0.7f, 0.55f, 1.0f, new Color(34, 34, 32, 255), lighting, 0.85f, 0.1f);

        // Firebox Door Frame & Glowing Interior
        LitCube(new Vector3(-0.15f, 2.0f, z - 3.72f), 0.52f, 0.56f, 0.16f, DarkSteel, lighting, 0.5f, 0.4f);
        if (lighting.FireboxIntensity > 0.05f)
        {
            // Glowing burning hearth core
            var glowColor = new Color(255, (int)(110 + lighting.FireboxIntensity * 60), 30, 255);
            Raylib.DrawCube(new Vector3(-0.15f, 2.0f, z - 3.68f), 0.40f, 0.44f, 0.08f, glowColor);
            Raylib.DrawSphere(new Vector3(-0.15f, 2.0f, z - 3.62f), 0.18f, new Color(255, 200, 75, 190));
        }

        // Wheel Bogies & Flanged Steel Wheels
        for (var axle = 0; axle < 4; axle++)
        {
            var wheelZ = z + 4.6f - axle * 3.05f;
            LitCube(new Vector3(0, 0.95f, wheelZ), 3.0f, 0.34f, 1.55f, DarkSteel, lighting, 0.7f, 0.4f);
            foreach (var side in new[] { -1, 1 })
            {
                var wheelPos = new Vector3(side * 1.43f, 0.66f, wheelZ);
                var wheelColor = LightingEngine.ShadeSurface(DarkSteel, new Vector3(side, 0, 0), wheelPos, lighting, 0.4f, 0.6f);
                Raylib.DrawSphere(wheelPos, 0.59f, wheelColor);
                LitCylinder(wheelPos, 0.18f, 1.25f, Brass, lighting, 0.25f, 0.8f);
            }
        }

        // Locomotive Headlights & Volumetric Beam
        var lanternPos = new Vector3(0, 2.3f, z + 7.25f);
        LitCube(lanternPos, 0.62f, 0.62f, 0.52f, Brass, lighting, 0.25f, 0.85f);
        if (lighting.HeadlightsActive)
        {
            // Glowing lens
            Raylib.DrawSphere(new Vector3(lanternPos.X, lanternPos.Y, lanternPos.Z + 0.28f), 0.32f, lighting.HeadlightColor);

            // Volumetric headlight beam piercing forward
            var beamTip = new Vector3(0, 1.25f, z + 46f);
            Raylib.DrawCylinderEx(lanternPos, beamTip, 0.38f, 4.8f, 16, new Color(255, 238, 185, 36));

            // Secondary soft illumination on the tracks in front
            Raylib.DrawCube(new Vector3(0, 0.22f, z + 16f), 3.6f, 0.05f, 22f, new Color(255, 235, 175, 28));
        }
        else
        {
            // Daytime glass reflector lens
            Raylib.DrawSphere(new Vector3(lanternPos.X, lanternPos.Y, lanternPos.Z + 0.28f), 0.28f, Glass);
        }

        Coupler(new Vector3(0, 0.95f, z + 8.0f), 1, lighting);
        Coupler(new Vector3(0, 0.95f, z - 8.0f), -1, lighting);
    }

    private static void DrawCarriage(float z, string name, Color color, bool workshop, LightingContext lighting)
    {
        // Carriage Frame & Body
        LitCube(new Vector3(0, 1.27f, z), 3.2f, 0.7f, 12.2f, DarkSteel, lighting, 0.65f, 0.35f);
        LitCube(new Vector3(0, 2.9f, z), 2.96f, 2.75f, 10.9f, color, lighting, 0.55f, 0.15f);
        LitCube(new Vector3(0, 4.34f, z), 3.1f, 0.18f, 11.15f, DarkSteel, lighting, 0.6f, 0.3f);

        // End Bulkhead Doors
        LitCube(new Vector3(0, 2.85f, z + 5.5f), 1.4f, 2.3f, 0.1f, WornSteel, lighting, 0.45f, 0.5f);
        LitCube(new Vector3(0, 2.85f, z - 5.5f), 1.4f, 2.3f, 0.1f, WornSteel, lighting, 0.45f, 0.5f);

        // Windows & Trim
        for (var side = -1; side <= 1; side += 2)
        {
            LitCube(new Vector3(side * 1.52f, 4.05f, z), 0.08f, 0.74f, 2.8f, workshop ? Glass : color, lighting, 0.2f, 0.1f);
            LitCube(new Vector3(side * 1.52f, 2.0f, z), 0.12f, 0.14f, 11f, Brass, lighting, 0.3f, 0.8f);
        }

        // Heavy Bogies
        foreach (var axle in new[] { -3.8f, 3.8f })
        {
            LitCube(new Vector3(0, 0.86f, z + axle), 2.9f, 0.34f, 1.2f, DarkSteel, lighting, 0.7f, 0.4f);
            foreach (var side in new[] { -1, 1 })
            {
                var wheelPos = new Vector3(side * 1.4f, 0.65f, z + axle);
                var wheelColor = LightingEngine.ShadeSurface(DarkSteel, new Vector3(side, 0, 0), wheelPos, lighting, 0.4f, 0.6f);
                Raylib.DrawSphere(wheelPos, 0.55f, wheelColor);
            }
        }

        Coupler(new Vector3(0, 0.95f, z + 6.1f), 1, lighting);
        Coupler(new Vector3(0, 0.95f, z - 6.1f), -1, lighting);

        if (workshop)
        {
            LitCube(new Vector3(0.9f, 2.05f, z + 1.0f), 1.1f, 0.8f, 2.6f, Timber, lighting, 0.8f, 0.0f);
            LitCube(new Vector3(-0.8f, 2.4f, z - 1.6f), 0.7f, 1.2f, 0.7f, DarkSteel, lighting, 0.5f, 0.5f);
        }
    }

    private static void Coupler(Vector3 position, int direction, LightingContext lighting)
    {
        LitCube(position, 0.42f, 0.32f, 0.85f, DarkSteel, lighting, 0.55f, 0.45f);
        var headPos = new Vector3(position.X, position.Y, position.Z + direction * 0.48f);
        var headColor = LightingEngine.ShadeSurface(WornSteel, new Vector3(0, 0, direction), headPos, lighting, 0.4f, 0.6f);
        Raylib.DrawSphere(headPos, 0.3f, headColor);
    }

    private static void LitLever(Vector3 basePoint, Color color, LightingContext lighting, float roughness, float metallicity)
    {
        var tip = new Vector3(basePoint.X + 0.08f, basePoint.Y + 0.76f, basePoint.Z);
        var leverColor = LightingEngine.ShadeSurface(color, Vector3.UnitY, basePoint, lighting, roughness, metallicity);
        Raylib.DrawCylinderEx(basePoint, tip, 0.055f, 0.035f, 8, leverColor);

        var knobPos = new Vector3(basePoint.X + 0.08f, basePoint.Y + 0.8f, basePoint.Z);
        var knobColor = LightingEngine.ShadeSurface(Brass, Vector3.UnitY, knobPos, lighting, 0.2f, 0.9f);
        Raylib.DrawSphere(knobPos, 0.09f, knobColor);
    }

    private static void LitCylinder(Vector3 center, float radius, float height, Color color, LightingContext lighting, float roughness, float metallicity)
    {
        var litColor = LightingEngine.ShadeSurface(color, Vector3.UnitY, center, lighting, roughness, metallicity);
        Raylib.DrawCylinder(center, radius * 0.72f, radius, height, 12, litColor);
    }

    private static void LitCube(
        Vector3 center,
        float width,
        float height,
        float length,
        Color baseColor,
        LightingContext lighting,
        float roughness = 0.65f,
        float metallicity = 0.0f)
    {
        var hW = width * 0.5f;
        var hH = height * 0.5f;
        var hL = length * 0.5f;

        var x0 = center.X - hW;
        var x1 = center.X + hW;
        var y0 = center.Y - hH;
        var y1 = center.Y + hH;
        var z0 = center.Z - hL;
        var z1 = center.Z + hL;

        // 6 directional faces with physically-modeled shading
        var cTop = LightingEngine.ShadeSurface(baseColor, Vector3.UnitY, new Vector3(center.X, y1, center.Z), lighting, roughness, metallicity);
        var cBottom = LightingEngine.ShadeSurface(baseColor, -Vector3.UnitY, new Vector3(center.X, y0, center.Z), lighting, roughness, metallicity);
        var cFront = LightingEngine.ShadeSurface(baseColor, Vector3.UnitZ, new Vector3(center.X, center.Y, z1), lighting, roughness, metallicity);
        var cBack = LightingEngine.ShadeSurface(baseColor, -Vector3.UnitZ, new Vector3(center.X, center.Y, z0), lighting, roughness, metallicity);
        var cRight = LightingEngine.ShadeSurface(baseColor, Vector3.UnitX, new Vector3(x1, center.Y, center.Z), lighting, roughness, metallicity);
        var cLeft = LightingEngine.ShadeSurface(baseColor, -Vector3.UnitX, new Vector3(x0, center.Y, center.Z), lighting, roughness, metallicity);

        Rlgl.CheckRenderBatchLimit(24);
        Rlgl.Begin(DrawMode.Quads);

        // Top Face (+Y)
        Rlgl.Color4ub(cTop.R, cTop.G, cTop.B, cTop.A);
        Rlgl.Normal3f(0f, 1f, 0f);
        Rlgl.Vertex3f(x0, y1, z0);
        Rlgl.Vertex3f(x0, y1, z1);
        Rlgl.Vertex3f(x1, y1, z1);
        Rlgl.Vertex3f(x1, y1, z0);

        // Bottom Face (-Y)
        Rlgl.Color4ub(cBottom.R, cBottom.G, cBottom.B, cBottom.A);
        Rlgl.Normal3f(0f, -1f, 0f);
        Rlgl.Vertex3f(x0, y0, z0);
        Rlgl.Vertex3f(x1, y0, z0);
        Rlgl.Vertex3f(x1, y0, z1);
        Rlgl.Vertex3f(x0, y0, z1);

        // Front Face (+Z)
        Rlgl.Color4ub(cFront.R, cFront.G, cFront.B, cFront.A);
        Rlgl.Normal3f(0f, 0f, 1f);
        Rlgl.Vertex3f(x0, y0, z1);
        Rlgl.Vertex3f(x1, y0, z1);
        Rlgl.Vertex3f(x1, y1, z1);
        Rlgl.Vertex3f(x0, y1, z1);

        // Back Face (-Z)
        Rlgl.Color4ub(cBack.R, cBack.G, cBack.B, cBack.A);
        Rlgl.Normal3f(0f, 0f, -1f);
        Rlgl.Vertex3f(x0, y0, z0);
        Rlgl.Vertex3f(x0, y1, z0);
        Rlgl.Vertex3f(x1, y1, z0);
        Rlgl.Vertex3f(x1, y0, z0);

        // Right Face (+X)
        Rlgl.Color4ub(cRight.R, cRight.G, cRight.B, cRight.A);
        Rlgl.Normal3f(1f, 0f, 0f);
        Rlgl.Vertex3f(x1, y0, z0);
        Rlgl.Vertex3f(x1, y1, z0);
        Rlgl.Vertex3f(x1, y1, z1);
        Rlgl.Vertex3f(x1, y0, z1);

        // Left Face (-X)
        Rlgl.Color4ub(cLeft.R, cLeft.G, cLeft.B, cLeft.A);
        Rlgl.Normal3f(-1f, 0f, 0f);
        Rlgl.Vertex3f(x0, y0, z0);
        Rlgl.Vertex3f(x0, y0, z1);
        Rlgl.Vertex3f(x0, y1, z1);
        Rlgl.Vertex3f(x0, y1, z0);

        Rlgl.End();
    }
}