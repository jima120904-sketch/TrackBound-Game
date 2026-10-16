# Trackbound

Trackbound is a first-person 3D railway-survival game. The train is the crew's moving home, workshop, and lifeline. The design references are in `Long Instructs/`.

## Build and Run

Install the .NET 10 SDK and a desktop environment with OpenGL support. The game uses Raylib-cs 7.0.2; `dotnet` restores the managed package and native Raylib library automatically.

```sh
dotnet build Trackbound.slnx
dotnet run --project Trackbound.Game/Trackbound.Game.csproj
dotnet run --project Tests/Core/Trackbound.Core.Tests.csproj
```

## Controls

- `WASD`: walk; mouse: look; `Shift`: sprint.
- `Up` / `Down`: throttle; `Space`: brake; `Left Control`: release brake.
- `E`: gather a resource or feed coal when aiming at the firebox.
- `C`: recipe menu; `Up` / `Down` select; `Enter` crafts; `I` opens inventory.
- `1`–`5`: hotbar; left mouse: swing or fire; `F`: eat; `X`: board or leave the train while stopped.

## Current Game Slice

The standalone C# runtime includes a first-person 3D view, moving locomotive with a cab and two cars, train fuel/throttle/braking, deterministic resource deposits, wood/stone/iron tool progression, hand/workbench/furnace recipes, craftable revolver/rifle/shotgun and ammunition, inventory and recipe menus, combat, a day/night threat loop, and health/hunger/stamina bars.

Terrain is regenerated deterministically from world coordinates. The detailed player-centered terrain region covers a 1 km radius; coarser radial LOD continues 10 km beyond that region, to an 11 km outer radius, with distance haze. The implementation is intentionally low-poly and procedural; it is a playable foundation, not finished AAA art or the full multiplayer GDD.

## Rendering Limits

Raylib-cs renders through Raylib's OpenGL raster pipeline. It does not provide hardware ray tracing, so this standalone build does not claim ray-traced lighting. Shading, sun color, terrain palettes, and distance haze are raster techniques. The Unity HDRP scaffold remains in the repository as an optional future route for HDRP effects, but Unity is not required to compile or run this .NET game.
