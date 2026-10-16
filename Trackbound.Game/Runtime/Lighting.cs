using Raylib_cs;
using System.Numerics;

namespace Trackbound.Runtime;

public sealed class LightingContext
{
    public float TimeOfDay;
    public float HourOfDay;
    public float Daylight;
    public float GoldenHour;
    public float Night;

    public Vector3 SunDirection;
    public Vector3 SunPosition;
    public Color SunDiscColor;
    public float SunDiscRadius;

    public Vector3 MoonDirection;
    public Vector3 MoonPosition;
    public Color MoonDiscColor;
    public float MoonDiscRadius;

    public Color KeyLightColor;
    public Vector3 KeyLightDirection;
    public Color SkyZenith;
    public Color SkyMid;
    public Color SkyHorizon;
    public Color GroundAmbient;
    public Color FogColor;

    public Vector3 FireboxWorldPos;
    public float FireboxIntensity;
    public Color FireboxColor;

    public bool HeadlightsActive;
    public Vector3 HeadlightWorldPos;
    public Vector3 HeadlightForward;
    public Color HeadlightColor;
}

public static class LightingEngine
{
    private static readonly Vector3[] StarField = GenerateStarField(180);

    private static Vector3[] GenerateStarField(int count)
    {
        var random = new Random(424242);
        var stars = new Vector3[count];
        for (var i = 0; i < count; i++)
        {
            var theta = (float)(random.NextDouble() * Math.PI * 2);
            var phi = (float)(random.NextDouble() * Math.PI * 0.46); // Upper hemisphere only
            var x = MathF.Cos(theta) * MathF.Cos(phi);
            var y = MathF.Sin(phi);
            var z = MathF.Sin(theta) * MathF.Cos(phi);
            stars[i] = new Vector3(x, y, z);
        }
        return stars;
    }

    public static LightingContext Compute(
        float timeOfDay,
        float fullCycleSeconds,
        float daySeconds,
        Vector3 playerPos,
        float trainZ,
        double fuelChunks,
        float elapsed)
    {
        var ctx = new LightingContext
        {
            TimeOfDay = timeOfDay,
            HourOfDay = (timeOfDay / fullCycleSeconds) * 24f
        };

        // Solar & lunar orbit
        // 6:00 is sunrise, 12:00 is noon, 18:00 is sunset
        var solarAngle = ((ctx.HourOfDay - 6f) / 12f) * MathF.PI;
        var sunElevation = MathF.Sin(solarAngle);
        var sunAzimuth = -MathF.Cos(solarAngle) * 0.72f;

        // Daylight curve: smoothly rises at dawn, peaks at midday, drops at dusk
        ctx.Daylight = Math.Clamp((sunElevation + 0.12f) / 1.12f, 0f, 1f);
        ctx.Daylight = MathF.Pow(ctx.Daylight, 0.85f);

        // Golden hour: intense warm glow when sun is near horizon (hours 5:00-7:30 and 16:30-19:00)
        var absElevation = MathF.Abs(sunElevation);
        ctx.GoldenHour = SmoothStep(0.38f, 0.04f, absElevation) * (sunElevation > -0.15f ? 1f : 0f);

        // Night intensity
        ctx.Night = Math.Clamp(1f - ctx.Daylight * 1.25f, 0f, 1f);

        // Sun celestial body
        var sunDirY = MathF.Max(0.04f, sunElevation);
        ctx.SunDirection = Vector3.Normalize(new Vector3(sunAzimuth, sunDirY, 0.42f));
        ctx.SunPosition = playerPos + ctx.SunDirection * 3500f;
        ctx.SunDiscRadius = 110f + ctx.GoldenHour * 35f;

        // Sun disc color shifts from brilliant white-gold at noon to deep amber-coral at sunset
        var noonSunDisc = new Color(255, 248, 225, 255);
        var goldenSunDisc = new Color(255, 175, 85, 255);
        ctx.SunDiscColor = ColorLerp(noonSunDisc, goldenSunDisc, ctx.GoldenHour);

        // Moon celestial body (opposite trajectory)
        var moonElevation = -sunElevation;
        var moonAzimuth = -sunAzimuth;
        var moonDirY = MathF.Max(0.08f, moonElevation);
        ctx.MoonDirection = Vector3.Normalize(new Vector3(moonAzimuth, moonDirY, -0.42f));
        ctx.MoonPosition = playerPos + ctx.MoonDirection * 3200f;
        ctx.MoonDiscRadius = 85f;
        ctx.MoonDiscColor = new Color(215, 232, 255, 255);

        // Key light selection (Sun by day, Moon by night)
        if (ctx.Daylight > 0.15f)
        {
            ctx.KeyLightDirection = ctx.SunDirection;
            var noonKey = new Color(255, 242, 220, 255);
            var goldenKey = new Color(255, 160, 80, 255);
            ctx.KeyLightColor = ColorLerp(noonKey, goldenKey, ctx.GoldenHour);
        }
        else
        {
            ctx.KeyLightDirection = ctx.MoonDirection;
            ctx.KeyLightColor = new Color(48, 68, 102, 255);
        }

        // Sky colors (Zenith, Mid, Horizon)
        // Day sky: crisp alpine cobalt fading to cyan horizon
        var dayZenith = new Color(38, 92, 175, 255);
        var dayMid = new Color(105, 162, 222, 255);
        var dayHorizon = new Color(198, 222, 238, 255);

        // Golden hour sky: deep twilight violet fading through amber to fiery horizon
        var goldenZenith = new Color(45, 36, 75, 255);
        var goldenMid = new Color(192, 98, 85, 255);
        var goldenHorizon = new Color(248, 162, 68, 255);

        // Night sky: deep midnight cosmic black-indigo
        var nightZenith = new Color(6, 10, 24, 255);
        var nightMid = new Color(15, 24, 46, 255);
        var nightHorizon = new Color(28, 38, 64, 255);

        var blendedZenith = ColorLerp(dayZenith, goldenZenith, ctx.GoldenHour);
        var blendedMid = ColorLerp(dayMid, goldenMid, ctx.GoldenHour);
        var blendedHorizon = ColorLerp(dayHorizon, goldenHorizon, ctx.GoldenHour);

        ctx.SkyZenith = ColorLerp(blendedZenith, nightZenith, ctx.Night);
        ctx.SkyMid = ColorLerp(blendedMid, nightMid, ctx.Night);
        ctx.SkyHorizon = ColorLerp(blendedHorizon, nightHorizon, ctx.Night);

        // Distance fog matches horizon to seamlessly blend the 11km mountain relief
        var dayFog = new Color(175, 202, 222, 255);
        var goldenFog = new Color(222, 142, 88, 255);
        var nightFog = new Color(20, 28, 48, 255);
        var blendedFog = ColorLerp(dayFog, goldenFog, ctx.GoldenHour);
        ctx.FogColor = ColorLerp(blendedFog, nightFog, ctx.Night);

        // Ambient light: sky dome illumination for shadowed surfaces
        var dayAmbient = new Color(95, 118, 148, 255);
        var goldenAmbient = new Color(135, 92, 98, 255);
        var nightAmbient = new Color(22, 28, 42, 255);
        var blendedAmbient = ColorLerp(dayAmbient, goldenAmbient, ctx.GoldenHour);
        ctx.GroundAmbient = ColorLerp(blendedAmbient, nightAmbient, ctx.Night);

        // Firebox hearth glow
        ctx.FireboxWorldPos = new Vector3(-0.15f, 2.3f, trainZ - 3.75f);
        if (fuelChunks > 0)
        {
            var baseHeat = Math.Clamp((float)fuelChunks / 18f, 0.45f, 1f);
            var flicker = MathF.Sin(elapsed * 9.2f) * 0.08f + MathF.Sin(elapsed * 17.5f) * 0.05f + MathF.Cos(elapsed * 29.1f) * 0.04f;
            ctx.FireboxIntensity = Math.Clamp(baseHeat + flicker, 0.25f, 1.15f);
            ctx.FireboxColor = new Color(255, 128, 38, 255);
        }
        else
        {
            ctx.FireboxIntensity = 0f;
            ctx.FireboxColor = Color.Black;
        }

        // Locomotive headlights
        ctx.HeadlightsActive = ctx.Daylight < 0.32f;
        ctx.HeadlightWorldPos = new Vector3(0, 2.3f, trainZ + 7.25f);
        ctx.HeadlightForward = new Vector3(0, -0.04f, 1f); // Pointing forward along tracks
        ctx.HeadlightColor = new Color(255, 235, 175, 255);

        return ctx;
    }

    public static Color ShadeSurface(
        Color baseColor,
        Vector3 normal,
        Vector3 worldPos,
        LightingContext lighting,
        float roughness = 0.65f,
        float metallicity = 0.0f)
    {
        // 1. Direct key light (sun or moon)
        var nDotL = MathF.Max(0f, Vector3.Dot(normal, lighting.KeyLightDirection));
        var directFactor = nDotL * (lighting.Daylight > 0.15f ? 0.72f : 0.35f);

        // 2. Hemisphere ambient (sky above, ground bounce below)
        var ambientFactor = normal.Y * 0.45f + 0.55f;
        var r = (baseColor.R / 255f) * ((lighting.GroundAmbient.R / 255f) * (1f - ambientFactor) + (lighting.KeyLightColor.R / 255f) * directFactor + 0.18f * ambientFactor);
        var g = (baseColor.G / 255f) * ((lighting.GroundAmbient.G / 255f) * (1f - ambientFactor) + (lighting.KeyLightColor.G / 255f) * directFactor + 0.18f * ambientFactor);
        var b = (baseColor.B / 255f) * ((lighting.GroundAmbient.B / 255f) * (1f - ambientFactor) + (lighting.KeyLightColor.B / 255f) * directFactor + 0.18f * ambientFactor);

        // 3. Firebox hearth glow (interior cab lighting)
        if (lighting.FireboxIntensity > 0.01f)
        {
            var toFire = lighting.FireboxWorldPos - worldPos;
            var distSq = toFire.LengthSquared();
            if (distSq < 100f) // Within 10m radius of furnace
            {
                var dist = MathF.Sqrt(distSq);
                var fireDir = toFire / dist;
                var fireDot = MathF.Max(0f, Vector3.Dot(normal, fireDir));
                var atten = 1f / (1f + 0.45f * dist + 0.18f * distSq);
                var fireStrength = fireDot * atten * lighting.FireboxIntensity * 1.8f;

                r += (lighting.FireboxColor.R / 255f) * fireStrength * 0.85f;
                g += (lighting.FireboxColor.G / 255f) * fireStrength * 0.45f;
                b += (lighting.FireboxColor.B / 255f) * fireStrength * 0.10f;
            }
        }

        // 4. Specular glint for polished industrial materials (brass, machined steel)
        if (metallicity > 0.2f && nDotL > 0f)
        {
            var spec = MathF.Pow(nDotL, 16f) * metallicity * (lighting.Daylight * 0.55f + 0.15f);
            r += spec * (lighting.KeyLightColor.R / 255f);
            g += spec * (lighting.KeyLightColor.G / 255f);
            b += spec * (lighting.KeyLightColor.B / 255f);
        }

        return new Color(
            Math.Clamp((int)(r * 255f), 0, 255),
            Math.Clamp((int)(g * 255f), 0, 255),
            Math.Clamp((int)(b * 255f), 0, 255),
            (int)baseColor.A);
    }

    public static void DrawSky(LightingContext lighting)
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();

        // 3-stop atmospheric sky gradient (Zenith -> Mid -> Horizon)
        var halfH = screenH / 2;
        Raylib.DrawRectangleGradientV(0, 0, screenW, halfH, lighting.SkyZenith, lighting.SkyMid);
        Raylib.DrawRectangleGradientV(0, halfH, screenW, screenH - halfH, lighting.SkyMid, lighting.SkyHorizon);

        // Golden hour horizon atmospheric band
        if (lighting.GoldenHour > 0.05f)
        {
            var bandH = (int)(screenH * 0.38f);
            var bandY = screenH - bandH;
            var glowColor = new Color((int)lighting.SkyHorizon.R, (int)lighting.SkyHorizon.G, (int)lighting.SkyHorizon.B, (int)(lighting.GoldenHour * 140));
            Raylib.DrawRectangleGradientV(0, bandY, screenW, bandH, Color.Blank, glowColor);
        }
    }

    public static void DrawCelestialAndAtmosphere(LightingContext lighting, Vector3 playerPos)
    {
        // 1. Draw Starfield at night
        if (lighting.Night > 0.08f)
        {
            var starAlpha = Math.Clamp((int)(lighting.Night * 240), 0, 255);
            var starColor = new Color(245, 248, 255, starAlpha);
            foreach (var star in StarField)
            {
                var starPos = playerPos + star * 4200f;
                Raylib.DrawSphere(starPos, 8.5f, starColor);
            }
        }

        // 2. Draw Sun or Moon
        if (lighting.Daylight > 0.05f)
        {
            // Volumetric sun atmospheric corona
            var coronaColor = new Color((int)lighting.SunDiscColor.R, (int)lighting.SunDiscColor.G, (int)lighting.SunDiscColor.B, (int)(50 + lighting.GoldenHour * 65));
            Raylib.DrawSphere(lighting.SunPosition, lighting.SunDiscRadius * 2.4f, coronaColor);

            // Sun core disc
            Raylib.DrawSphere(lighting.SunPosition, lighting.SunDiscRadius, lighting.SunDiscColor);
        }
        else
        {
            // Moon halo & core disc
            var moonHalo = new Color(195, 220, 255, 38);
            Raylib.DrawSphere(lighting.MoonPosition, lighting.MoonDiscRadius * 1.8f, moonHalo);
            Raylib.DrawSphere(lighting.MoonPosition, lighting.MoonDiscRadius, lighting.MoonDiscColor);
        }
    }

    private static float SmoothStep(float a, float b, float v)
    {
        var t = Math.Clamp((v - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    public static Color ColorLerp(Color a, Color b, float t) => new(
        Math.Clamp((int)(a.R + (b.R - a.R) * t), 0, 255),
        Math.Clamp((int)(a.G + (b.G - a.G) * t), 0, 255),
        Math.Clamp((int)(a.B + (b.B - a.B) * t), 0, 255),
        Math.Clamp((int)(a.A + (b.A - a.A) * t), 0, 255));
}
