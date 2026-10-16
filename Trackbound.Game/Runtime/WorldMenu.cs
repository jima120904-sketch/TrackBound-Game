using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Trackbound.Runtime;

public enum MenuState
{
    Title,
    WorldSelect,
    NewWorld,
    Settings,
    PauseMenu,
    Playing
}

public sealed class WorldMenu
{
    private MenuState _state = MenuState.Title;
    private int _selectedOption;
    private int _worldListIndex;
    private List<WorldSaveData> _cachedWorlds = new();
    private string _newWorldName = "New Expedition";
    private string _newWorldSeedInput = "";
    private int _inputField; // 0 = name, 1 = seed
    private string _statusMessage = "";
    private float _statusTimer;
    private int _renderDistanceKm = 50;

    public MenuState State => _state;
    public int RenderDistanceKm => _renderDistanceKm;
    public WorldSaveData? ActiveWorld { get; private set; }

    public event Action<WorldSaveData>? OnWorldLoaded;

    public WorldMenu()
    {
        RefreshWorldList();
    }

    public void RefreshWorldList()
    {
        _cachedWorlds = WorldSaveManager.ListWorlds();
        if (_worldListIndex >= _cachedWorlds.Count)
        {
            _worldListIndex = Math.Max(0, _cachedWorlds.Count - 1);
        }
    }

    public void OpenPauseMenu()
    {
        _state = MenuState.PauseMenu;
        _selectedOption = 0;
        Raylib.EnableCursor();
        Raylib.ShowCursor();
    }

    public void ResumePlaying()
    {
        _state = MenuState.Playing;
    }

    public void SetActiveWorld(WorldSaveData world)
    {
        ActiveWorld = world;
        _renderDistanceKm = world.RenderDistanceKm > 0 ? world.RenderDistanceKm : 50;
    }

    public void Update(float delta)
    {
        if (_statusTimer > 0)
        {
            _statusTimer -= delta;
            if (_statusTimer <= 0) _statusMessage = "";
        }

        switch (_state)
        {
            case MenuState.Title:
                UpdateTitle();
                break;
            case MenuState.WorldSelect:
                UpdateWorldSelect();
                break;
            case MenuState.NewWorld:
                UpdateNewWorld();
                break;
            case MenuState.Settings:
                UpdateSettings();
                break;
            case MenuState.PauseMenu:
                UpdatePauseMenu();
                break;
        }
    }

    private void UpdateTitle()
    {
        var optionsCount = _cachedWorlds.Count > 0 ? 5 : 4;
        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
        {
            _selectedOption = (_selectedOption + 1) % optionsCount;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
        {
            _selectedOption = (_selectedOption - 1 + optionsCount) % optionsCount;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            var opt = _selectedOption;
            if (_cachedWorlds.Count == 0 && opt >= 0) opt++; // Offset if no continue option

            switch (opt)
            {
                case 0: // Continue last played
                    if (_cachedWorlds.Count > 0)
                    {
                        LoadAndLaunchWorld(_cachedWorlds[0]);
                    }
                    break;
                case 1: // World Select
                    RefreshWorldList();
                    _state = MenuState.WorldSelect;
                    _worldListIndex = 0;
                    break;
                case 2: // New World
                    _state = MenuState.NewWorld;
                    var random = new Random();
                    _newWorldName = $"Expedition_{random.Next(100, 999)}";
                    _newWorldSeedInput = random.Next(1000, 999999).ToString();
                    _inputField = 0;
                    break;
                case 3: // Settings
                    _state = MenuState.Settings;
                    break;
                case 4: // Exit
                    // Handled by window close
                    break;
            }
        }
    }

    private void UpdateWorldSelect()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            _state = MenuState.Title;
            return;
        }

        if (_cachedWorlds.Count == 0)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                _state = MenuState.NewWorld;
            }
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
        {
            _worldListIndex = (_worldListIndex + 1) % _cachedWorlds.Count;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
        {
            _worldListIndex = (_worldListIndex - 1 + _cachedWorlds.Count) % _cachedWorlds.Count;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            LoadAndLaunchWorld(_cachedWorlds[_worldListIndex]);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Delete))
        {
            var world = _cachedWorlds[_worldListIndex];
            WorldSaveManager.DeleteWorld(world.WorldName);
            ShowStatus($"Deleted {world.WorldName}");
            RefreshWorldList();
        }
    }

    private void UpdateNewWorld()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            _state = MenuState.Title;
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Tab) || Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            _inputField = 1 - _inputField;
        }

        // Handle text typing
        var key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if (_inputField == 0 && _newWorldName.Length < 24 && (char.IsLetterOrDigit((char)key) || key == ' ' || key == '_'))
            {
                _newWorldName += (char)key;
            }
            else if (_inputField == 1 && _newWorldSeedInput.Length < 9 && char.IsDigit((char)key))
            {
                _newWorldSeedInput += (char)key;
            }
            key = Raylib.GetCharPressed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
        {
            if (_inputField == 0 && _newWorldName.Length > 0)
            {
                _newWorldName = _newWorldName[..^1];
            }
            else if (_inputField == 1 && _newWorldSeedInput.Length > 0)
            {
                _newWorldSeedInput = _newWorldSeedInput[..^1];
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.R)) // Randomize seed
        {
            var random = new Random();
            _newWorldSeedInput = random.Next(1000, 999999).ToString();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            int? seed = null;
            if (int.TryParse(_newWorldSeedInput, out var parsedSeed))
            {
                seed = parsedSeed;
            }

            var save = WorldSaveManager.CreateDefaultWorld(_newWorldName, seed);
            save.RenderDistanceKm = _renderDistanceKm;
            WorldSaveManager.SaveWorld(save);
            LoadAndLaunchWorld(save);
        }
    }

    private void UpdateSettings()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            _state = ActiveWorld != null ? MenuState.PauseMenu : MenuState.Title;
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A))
        {
            CycleRenderDistance(-1);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            CycleRenderDistance(1);
        }
    }

    private void CycleRenderDistance(int direction)
    {
        var distances = new[] { 10, 25, 50, 100 };
        var index = Array.IndexOf(distances, _renderDistanceKm);
        if (index < 0) index = 2; // Default 50km
        index = (index + direction + distances.Length) % distances.Length;
        _renderDistanceKm = distances[index];
        if (ActiveWorld != null)
        {
            ActiveWorld.RenderDistanceKm = _renderDistanceKm;
        }
    }

    private void UpdatePauseMenu()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            ResumePlaying();
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
        {
            _selectedOption = (_selectedOption + 1) % 4;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
        {
            _selectedOption = (_selectedOption - 1 + 4) % 4;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            switch (_selectedOption)
            {
                case 0: // Resume
                    ResumePlaying();
                    break;
                case 1: // Save World
                    if (ActiveWorld != null)
                    {
                        WorldSaveManager.SaveWorld(ActiveWorld);
                        ShowStatus("World progress saved successfully!");
                    }
                    break;
                case 2: // Settings (render distance)
                    CycleRenderDistance(1);
                    break;
                case 3: // Save & Return to Title
                    if (ActiveWorld != null)
                    {
                        WorldSaveManager.SaveWorld(ActiveWorld);
                    }
                    RefreshWorldList();
                    _state = MenuState.Title;
                    _selectedOption = 0;
                    break;
            }
        }
    }

    private void LoadAndLaunchWorld(WorldSaveData world)
    {
        SetActiveWorld(world);
        _state = MenuState.Playing;
        OnWorldLoaded?.Invoke(world);
    }

    private void ShowStatus(string message)
    {
        _statusMessage = message;
        _statusTimer = 3.2f;
    }

    public void Draw()
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();

        // Dark atmospheric menu background
        Raylib.DrawRectangleGradientV(0, 0, screenW, screenH, new Color(12, 18, 28, 245), new Color(24, 32, 45, 252));

        // Decorative background railway line
        Raylib.DrawLine(0, screenH - 120, screenW, screenH - 120, new Color(45, 58, 68, 180));
        Raylib.DrawLine(0, screenH - 116, screenW, screenH - 116, new Color(110, 115, 112, 220));

        switch (_state)
        {
            case MenuState.Title:
                DrawTitle(screenW, screenH);
                break;
            case MenuState.WorldSelect:
                DrawWorldSelect(screenW, screenH);
                break;
            case MenuState.NewWorld:
                DrawNewWorld(screenW, screenH);
                break;
            case MenuState.Settings:
                DrawSettings(screenW, screenH);
                break;
            case MenuState.PauseMenu:
                DrawPauseMenu(screenW, screenH);
                break;
        }

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            var msgWidth = Raylib.MeasureText(_statusMessage, 18);
            Raylib.DrawRectangle((screenW - msgWidth) / 2 - 16, screenH - 85, msgWidth + 32, 38, new Color(18, 24, 22, 220));
            Raylib.DrawRectangleLines((screenW - msgWidth) / 2 - 16, screenH - 85, msgWidth + 32, 38, new Color(120, 195, 135, 255));
            Raylib.DrawText(_statusMessage, (screenW - msgWidth) / 2, screenH - 75, 18, new Color(145, 235, 165, 255));
        }
    }

    private void DrawTitle(int screenW, int screenH)
    {
        // Title Header
        const string title = "TRACKBOUND";
        const string subtitle = "PROCEDURAL RAILWAY SURVIVAL  |  200 KM HORIZON ENGINE";
        var titleW = Raylib.MeasureText(title, 54);
        var subW = Raylib.MeasureText(subtitle, 16);

        Raylib.DrawText(title, (screenW - titleW) / 2, 110, 54, new Color(245, 242, 228, 255));
        Raylib.DrawText(subtitle, (screenW - subW) / 2, 175, 16, new Color(185, 142, 85, 255));

        var hasContinue = _cachedWorlds.Count > 0;
        var options = new List<string>();
        if (hasContinue) options.Add($"CONTINUE: {_cachedWorlds[0].WorldName} (Seed {_cachedWorlds[0].Seed})");
        options.Add("WORLD SELECT (SAVED EXPEDITIONS)");
        options.Add("NEW WORLD (CUSTOM OR RANDOM SEED)");
        options.Add($"RENDER DISTANCE: {_renderDistanceKm} KM  (200 KM HORIZON)");
        options.Add("EXIT TO DESKTOP");

        var startY = 250;
        for (var i = 0; i < options.Count; i++)
        {
            var isSelected = i == _selectedOption;
            var text = (isSelected ? "> " : "  ") + options[i];
            var color = isSelected ? new Color(255, 205, 105, 255) : new Color(175, 185, 195, 255);
            var textW = Raylib.MeasureText(text, 20);
            var x = (screenW - textW) / 2;
            var y = startY + i * 46;

            if (isSelected)
            {
                Raylib.DrawRectangle(x - 20, y - 4, textW + 40, 32, new Color(42, 56, 72, 140));
                Raylib.DrawRectangleLines(x - 20, y - 4, textW + 40, 32, new Color(195, 145, 75, 200));
            }
            Raylib.DrawText(text, x, y, 20, color);
        }

        // Instructions footer
        var hint = "Use UP / DOWN to navigate  |  ENTER to select";
        var hintW = Raylib.MeasureText(hint, 14);
        Raylib.DrawText(hint, (screenW - hintW) / 2, screenH - 50, 14, new Color(120, 135, 145, 255));
    }

    private void DrawWorldSelect(int screenW, int screenH)
    {
        const string header = "SELECT EXPEDITION";
        var headW = Raylib.MeasureText(header, 36);
        Raylib.DrawText(header, (screenW - headW) / 2, 80, 36, new Color(245, 242, 228, 255));

        if (_cachedWorlds.Count == 0)
        {
            var empty = "No saved worlds found. Press ENTER to create a new world!";
            var emptyW = Raylib.MeasureText(empty, 20);
            Raylib.DrawText(empty, (screenW - emptyW) / 2, 260, 20, Color.Yellow);
            return;
        }

        var startY = 160;
        var boxW = 680;
        var boxX = (screenW - boxW) / 2;

        for (var i = 0; i < Math.Min(6, _cachedWorlds.Count); i++)
        {
            var world = _cachedWorlds[i];
            var isSelected = i == _worldListIndex;
            var y = startY + i * 72;

            var bg = isSelected ? new Color(38, 55, 75, 220) : new Color(22, 28, 38, 180);
            var border = isSelected ? new Color(225, 165, 80, 255) : new Color(55, 68, 80, 255);
            Raylib.DrawRectangle(boxX, y, boxW, 60, bg);
            Raylib.DrawRectangleLines(boxX, y, boxW, 60, border);

            var titleText = $"{(isSelected ? "> " : "  ")}{world.WorldName}";
            var detailText = $"Seed: {world.Seed}   Distance: {world.TrainZ / 1000f:0.0} km   Fuel: {world.FuelChunks:0} coal   Played: {world.LastPlayed.ToLocalTime():yyyy-MM-dd HH:mm}";

            Raylib.DrawText(titleText, boxX + 16, y + 10, 20, isSelected ? new Color(255, 215, 120, 255) : Color.White);
            Raylib.DrawText(detailText, boxX + 32, y + 35, 14, new Color(165, 178, 188, 255));
        }

        var hint = "UP / DOWN select  |  ENTER embark  |  DEL delete  |  ESC back";
        var hintW = Raylib.MeasureText(hint, 14);
        Raylib.DrawText(hint, (screenW - hintW) / 2, screenH - 50, 14, new Color(120, 135, 145, 255));
    }

    private void DrawNewWorld(int screenW, int screenH)
    {
        const string header = "EMBARK ON NEW EXPEDITION";
        var headW = Raylib.MeasureText(header, 36);
        Raylib.DrawText(header, (screenW - headW) / 2, 90, 36, new Color(245, 242, 228, 255));

        var boxW = 560;
        var boxX = (screenW - boxW) / 2;

        // Name input box
        Raylib.DrawText("World Name:", boxX, 200, 18, Color.White);
        var nameBorder = _inputField == 0 ? new Color(225, 165, 80, 255) : new Color(70, 85, 100, 255);
        Raylib.DrawRectangle(boxX, 230, boxW, 44, new Color(20, 26, 36, 220));
        Raylib.DrawRectangleLines(boxX, 230, boxW, 44, nameBorder);
        Raylib.DrawText(_newWorldName + (_inputField == 0 && (int)(Raylib.GetTime() * 2) % 2 == 0 ? "_" : ""), boxX + 14, 242, 20, Color.White);

        // Seed input box
        Raylib.DrawText("Procedural Seed (numeric):", boxX, 305, 18, Color.White);
        var seedBorder = _inputField == 1 ? new Color(225, 165, 80, 255) : new Color(70, 85, 100, 255);
        Raylib.DrawRectangle(boxX, 335, boxW, 44, new Color(20, 26, 36, 220));
        Raylib.DrawRectangleLines(boxX, 335, boxW, 44, seedBorder);
        Raylib.DrawText(_newWorldSeedInput + (_inputField == 1 && (int)(Raylib.GetTime() * 2) % 2 == 0 ? "_" : ""), boxX + 14, 347, 20, new Color(255, 220, 130, 255));

        // Options info
        Raylib.DrawText($"Active Render Distance: {_renderDistanceKm} km  (Horizon Mesh: 200 km)", boxX, 410, 16, new Color(165, 182, 195, 255));
        Raylib.DrawText("Each seed procedurally generates completely unique mountain ranges, valleys, and forests.", boxX, 435, 14, new Color(135, 150, 160, 255));

        var hint = "TAB switch field  |  R randomize seed  |  ENTER create & launch  |  ESC cancel";
        var hintW = Raylib.MeasureText(hint, 14);
        Raylib.DrawText(hint, (screenW - hintW) / 2, screenH - 50, 14, new Color(120, 135, 145, 255));
    }

    private void DrawSettings(int screenW, int screenH)
    {
        const string header = "GRAPHICS & SCALE SETTINGS";
        var headW = Raylib.MeasureText(header, 36);
        Raylib.DrawText(header, (screenW - headW) / 2, 120, 36, new Color(245, 242, 228, 255));

        var boxW = 560;
        var boxX = (screenW - boxW) / 2;
        var y = 240;

        Raylib.DrawRectangle(boxX, y, boxW, 110, new Color(24, 34, 48, 220));
        Raylib.DrawRectangleLines(boxX, y, boxW, 110, new Color(225, 165, 80, 255));

        var distText = $"<  Active Terrain Render Distance: {_renderDistanceKm} KM  >";
        var distW = Raylib.MeasureText(distText, 22);
        Raylib.DrawText(distText, (screenW - distW) / 2, y + 24, 22, new Color(255, 215, 120, 255));

        var sub = "Distant Continental Horizon: 200 KM scaled mountain silhouettes";
        var subW = Raylib.MeasureText(sub, 15);
        Raylib.DrawText(sub, (screenW - subW) / 2, y + 68, 15, new Color(185, 205, 220, 255));

        var hint = "LEFT / RIGHT or SPACE to cycle  |  ENTER or ESC to confirm";
        var hintW = Raylib.MeasureText(hint, 14);
        Raylib.DrawText(hint, (screenW - hintW) / 2, screenH - 50, 14, new Color(120, 135, 145, 255));
    }

    private void DrawPauseMenu(int screenW, int screenH)
    {
        // Dark translucent overlay
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(10, 14, 22, 195));

        var boxW = 480;
        var boxH = 340;
        var boxX = (screenW - boxW) / 2;
        var boxY = (screenH - boxH) / 2;

        Raylib.DrawRectangle(boxX, boxY, boxW, boxH, new Color(18, 25, 36, 245));
        Raylib.DrawRectangleLines(boxX, boxY, boxW, boxH, new Color(195, 145, 75, 220));

        var title = "GAME PAUSED";
        var titleW = Raylib.MeasureText(title, 32);
        Raylib.DrawText(title, (screenW - titleW) / 2, boxY + 28, 32, new Color(245, 242, 228, 255));

        var options = new[]
        {
            "RESUME EXPEDITION",
            "SAVE WORLD PROGRESS",
            $"RENDER DISTANCE: {_renderDistanceKm} KM (200KM HORIZON)",
            "SAVE & RETURN TO TITLE"
        };

        for (var i = 0; i < options.Length; i++)
        {
            var isSelected = i == _selectedOption;
            var text = (isSelected ? "> " : "  ") + options[i];
            var color = isSelected ? new Color(255, 210, 105, 255) : new Color(175, 185, 195, 255);
            var optY = boxY + 95 + i * 50;

            if (isSelected)
            {
                Raylib.DrawRectangle(boxX + 25, optY - 4, boxW - 50, 36, new Color(38, 52, 68, 160));
                Raylib.DrawRectangleLines(boxX + 25, optY - 4, boxW - 50, 36, new Color(215, 155, 75, 180));
            }
            Raylib.DrawText(text, boxX + 40, optY + 4, 18, color);
        }
    }
}
