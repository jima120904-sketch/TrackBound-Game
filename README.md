# Trackbound

Trackbound is a first-person 3D railway-survival game for 1–4 players. The train is the team's moving home, workshop, and lifeline. The design references live in `Long Instructs/`.

## Prototype

Open the repository root with Unity Hub using the editor version in `ProjectSettings/ProjectVersion.txt`. Unity Package Manager installs HDRP and the Test Framework from `Packages/manifest.json`. On first import, the editor setup script creates and registers `Assets/Scenes/TrackboundPrototype.unity`.

The current playable slice builds its scene at runtime and includes a first-person locomotive cab, throttle and brake simulation, coal/firebox interaction, two attached cars, day/night lighting, deterministic terrain chunks, instanced trees, and streamed track.

Controls: WASD move, mouse look, Shift sprint, Space jump, Up/Down throttle, Left/Right brake, E interact. Press Escape to release the mouse; click to recapture it.

## HDRP and Ray Tracing

HDRP is declared as a project dependency. The editor menu `Trackbound > Diagnostics > HDRP and Ray Tracing` reports the active render pipeline, graphics API, and device ray-tracing support. Ray tracing is not assumed to be available: HDRP must be active, and ray-traced effects must be enabled in the HDRP asset and camera Frame Settings on a supported GPU/API. This container has no Unity Editor, so it cannot import HDRP, build the project, or verify rendered ray tracing. Use HDRP's Wizard in the Unity Editor to configure the pipeline; the prototype retains raster lighting as its fallback.

## Core Checks

The shared train dynamics and terrain generator can be checked without Unity:

```sh
dotnet run --project Tests/Core/Trackbound.Core.Tests.csproj
```

These checks cover deterministic terrain, fuel use, traction, and braking. They do not replace Unity Editor or in-game validation.
