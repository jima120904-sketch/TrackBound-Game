using Raylib_cs;
using Trackbound.Runtime;

Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
Raylib.InitWindow(1440, 900, "Trackbound | 3D Railway Survival");
Raylib.SetTargetFPS(60);

try
{
    new SurvivalGame().Run();
}
finally
{
    Raylib.CloseWindow();
}