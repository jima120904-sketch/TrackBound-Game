using Raylib_cs;
using System.Numerics;

namespace Trackbound.Runtime;

public sealed class TrainRenderer
{
    private static readonly Color Paint = new(42, 77, 68, 255);
    private static readonly Color DarkSteel = new(45, 52, 54, 255);
    private static readonly Color WornSteel = new(101, 105, 99, 255);
    private static readonly Color Brass = new(183, 132, 54, 255);
    private static readonly Color Timber = new(104, 72, 49, 255);
    private static readonly Color Glass = new(79, 124, 133, 255);

    public void Draw(float trainZ, float speed, bool headlights)
    {
        DrawTrack(trainZ);
        DrawLocomotive(trainZ, headlights);
        DrawCarriage(trainZ - 14, "Cargo car", new Color(90, 76, 56, 255), false);
        DrawCarriage(trainZ - 29, "Workshop car", new Color(55, 78, 73, 255), true);
    }

    private static void DrawTrack(float trainZ)
    {
        var start = (int)MathF.Floor((trainZ - 180) / 4) * 4;
        for (var z = start; z < trainZ + 420; z += 4)
        {
            Cube(new Vector3(0, 0.12f, z), 3.15f, 0.2f, 0.28f, Timber);
        }
        Cube(new Vector3(-0.76f, 0.38f, trainZ + 120), 0.15f, 0.24f, 600, WornSteel);
        Cube(new Vector3(0.76f, 0.38f, trainZ + 120), 0.15f, 0.24f, 600, WornSteel);
    }

    private static void DrawLocomotive(float z, bool headlights)
    {
        Cube(new Vector3(0, 1.25f, z), 3.3f, 0.7f, 14.2f, DarkSteel);
        Raylib.DrawCylinderEx(new Vector3(0, 2.7f, z - 0.3f), new Vector3(0, 2.7f, z + 4.3f), 1.1f, 1.1f, 16, Paint);
        Cylinder(new Vector3(0, 2.72f, z + 0.4f), 0.44f, 0.92f, Brass);
        Cylinder(new Vector3(0, 2.7f, z + 4.1f), 0.36f, 1.1f, DarkSteel);
        Cube(new Vector3(0, 1.1f, z + 7.1f), 3.4f, 0.5f, 0.36f, Paint);
        Cube(new Vector3(0, 1.1f, z - 7.1f), 3.4f, 0.5f, 0.36f, Paint);
        Cube(new Vector3(0, 1.77f, z - 5.2f), 3.12f, 0.2f, 5.25f, Timber);
        Cube(new Vector3(0, 5.27f, z - 5.2f), 3.35f, 0.22f, 5.5f, Paint);
        Cube(new Vector3(0, 3.9f, z - 7.8f), 3.12f, 2.55f, 0.17f, Paint);
        Cube(new Vector3(-1.53f, 2.47f, z - 5.2f), 0.17f, 1.2f, 5.1f, Paint);
        Cube(new Vector3(1.53f, 2.47f, z - 5.2f), 0.17f, 1.2f, 5.1f, Paint);

        foreach (var side in new[] { -1, 1 })
        {
            Cube(new Vector3(side * 1.54f, 4.13f, z - 5.2f), 0.08f, 0.98f, 5.0f, Glass);
            Cube(new Vector3(side * 1.54f, 4.13f, z - 7.62f), 0.12f, 1.1f, 0.12f, Brass);
            Cube(new Vector3(side * 1.54f, 4.13f, z - 6.0f), 0.12f, 1.1f, 0.12f, Brass);
            Cube(new Vector3(side * 1.54f, 4.13f, z - 4.4f), 0.12f, 1.1f, 0.12f, Brass);
            Cube(new Vector3(side * 1.54f, 4.13f, z - 2.8f), 0.12f, 1.1f, 0.12f, Brass);
            Cube(new Vector3(side * 1.7f, 1.64f, z - 0.5f), 0.23f, 0.14f, 10.5f, DarkSteel);
        }

        Cube(new Vector3(0, 2.47f, z - 3.25f), 2.6f, 0.57f, 0.58f, Timber);
        Cube(new Vector3(0, 2.9f, z - 3.48f), 2.48f, 0.35f, 0.16f, DarkSteel);
        Cube(new Vector3(1.05f, 2.2f, z - 6.6f), 0.95f, 0.72f, 1.15f, Timber);
        Cube(new Vector3(1.05f, 2.62f, z - 6.6f), 0.99f, 0.12f, 1.18f, WornSteel);
        Lever(new Vector3(-0.75f, 3.0f, z - 3.0f), Brass);
        Lever(new Vector3(0.72f, 3.0f, z - 3.0f), WornSteel);

        Cube(new Vector3(-0.96f, 2.05f, z - 6.85f), 0.82f, 0.56f, 1.4f, DarkSteel);
        Cube(new Vector3(-0.15f, 2.35f, z - 4.05f), 0.65f, 0.62f, 0.12f, WornSteel);
        Cube(new Vector3(-0.9f, 2.02f, z - 4.0f), 0.7f, 0.55f, 1.0f, new Color(34, 34, 32, 255));
        Cube(new Vector3(-0.15f, 2.0f, z - 3.72f), 0.45f, 0.5f, 0.16f, new Color(125, 68, 44, 255));

        for (var axle = 0; axle < 4; axle++)
        {
            var wheelZ = z + 4.6f - axle * 3.05f;
            Cube(new Vector3(0, 0.95f, wheelZ), 3.0f, 0.34f, 1.55f, DarkSteel);
            foreach (var side in new[] { -1, 1 })
            {
                var wheelPos = new Vector3(side * 1.43f, 0.66f, wheelZ);
                Raylib.DrawSphere(wheelPos, 0.59f, DarkSteel);
                Raylib.DrawCylinder(new Vector3(wheelPos.X, wheelPos.Y, wheelPos.Z), 0.18f, 0.18f, 1.25f, 10, Brass);
            }
        }

        if (headlights)
        {
            Raylib.DrawSphere(new Vector3(0, 2.3f, z + 7.25f), 0.35f, new Color(255, 222, 152, 255));
        }
        Coupler(new Vector3(0, 0.95f, z + 8.0f), 1);
        Coupler(new Vector3(0, 0.95f, z - 8.0f), -1);
    }

    private static void DrawCarriage(float z, string name, Color color, bool workshop)
    {
        Cube(new Vector3(0, 1.27f, z), 3.2f, 0.7f, 12.2f, DarkSteel);
        Cube(new Vector3(0, 2.9f, z), 2.96f, 2.75f, 10.9f, color);
        Cube(new Vector3(0, 4.34f, z), 3.1f, 0.18f, 11.15f, DarkSteel);
        Cube(new Vector3(0, 2.85f, z + 5.5f), 1.4f, 2.3f, 0.1f, WornSteel);
        Cube(new Vector3(0, 2.85f, z - 5.5f), 1.4f, 2.3f, 0.1f, WornSteel);
        for (var side = -1; side <= 1; side += 2)
        {
            Cube(new Vector3(side * 1.52f, 4.05f, z), 0.08f, 0.74f, 2.8f, workshop ? Glass : color);
            Cube(new Vector3(side * 1.52f, 2.0f, z), 0.12f, 0.14f, 11f, Brass);
        }
        foreach (var axle in new[] { -3.8f, 3.8f })
        {
            Cube(new Vector3(0, 0.86f, z + axle), 2.9f, 0.34f, 1.2f, DarkSteel);
            foreach (var side in new[] { -1, 1 })
            {
                Raylib.DrawSphere(new Vector3(side * 1.4f, 0.65f, z + axle), 0.55f, DarkSteel);
            }
        }
        Coupler(new Vector3(0, 0.95f, z + 6.1f), 1);
        Coupler(new Vector3(0, 0.95f, z - 6.1f), -1);

        if (workshop)
        {
            Cube(new Vector3(0.9f, 2.05f, z + 1.0f), 1.1f, 0.8f, 2.6f, Timber);
            Cube(new Vector3(-0.8f, 2.4f, z - 1.6f), 0.7f, 1.2f, 0.7f, DarkSteel);
        }
    }

    private static void Coupler(Vector3 position, int direction)
    {
        Cube(position, 0.42f, 0.32f, 0.85f, DarkSteel);
        Raylib.DrawSphere(new Vector3(position.X, position.Y, position.Z + direction * 0.48f), 0.3f, WornSteel);
    }

    private static void Lever(Vector3 basePoint, Color color)
    {
        Raylib.DrawCylinderEx(basePoint, new Vector3(basePoint.X + 0.08f, basePoint.Y + 0.76f, basePoint.Z), 0.055f, 0.035f, 8, color);
        Raylib.DrawSphere(new Vector3(basePoint.X + 0.08f, basePoint.Y + 0.8f, basePoint.Z), 0.09f, Brass);
    }

    private static void Cylinder(Vector3 center, float radius, float height, Color color)
    {
        Raylib.DrawCylinder(center, radius * 0.72f, radius, height, 12, color);
    }

    private static void Cube(Vector3 center, float width, float height, float length, Color color)
    {
        Raylib.DrawCube(center, width, height, length, color);
    }
}