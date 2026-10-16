using Raylib_cs;
using System.Numerics;
using Trackbound.Core;
using Trackbound.Gameplay;

namespace Trackbound.Runtime;

public sealed class SurvivalGame
{
    private const float DaySeconds = 30 * 60f;
    private const float FullCycleSeconds = 48 * 60f;

    private readonly Inventory _inventory = new();
    private TerrainRenderer _terrain = new(731);
    private readonly TrainRenderer _trainRenderer = new();
    private TrainSimulation _train = new(75);
    private readonly List<ResourceNode> _resources = new();
    private readonly List<Enemy> _enemies = new();
    private readonly HashSet<long> _collectedNodes = new();
    private Random _random = new(191);
    private readonly WorldMenu _menu = new();
    private Camera3D _camera;
    private Vector3 _playerPosition = new(0, 3.45f, -5.1f);
    private Vector3 _forward = new(0, 0, 1);
    private Vector3 _tracerStart;
    private Vector3 _tracerEnd;
    private float _yaw;
    private float _pitch;
    private float _throttle;
    private float _brake;
    private float _health = 100;
    private float _hunger = 100;
    private float _stamina = 100;
    private float _trainZ;
    private float _timeOfDay = 12 * 60f;
    private float _elapsed;
    private float _attackCooldown;
    private float _enemySpawnTimer = 18;
    private float _messageTimer;
    private float _tracerTimer;
    private float _autosaveTimer = 60f;
    private string _message = "The cab is yours. Find supplies, craft gear, keep the train moving.";
    private bool _craftingOpen;
    private bool _showDebug = true;
    private int _mouseMode = 1; // 1 = raylib captured/raw mode (default), 0 = relative delta tracking
    private bool _mouseReset = true;
    private bool _wasFocused = true;
    private Vector2 _lastMousePosition;
    private Vector2 _debugRawMouse;
    private bool _inventoryOpen;
    private bool _isOnTrain = true;
    private int _recipeIndex;
    private int _resourceCenterX = int.MinValue;
    private int _resourceCenterZ = int.MinValue;
    private int _selectedSlot;
    private CraftingStation _station = CraftingStation.Hand;
    private ItemId[] _hotbar = { ItemId.Revolver, ItemId.WoodenSword, ItemId.IronAxe, ItemId.Rifle, ItemId.Shotgun };

    private readonly record struct ResourceNode(long Id, Vector3 Position, ItemId Item, int Amount, Color Color);

    private sealed class Enemy
    {
        public Vector3 Position;
        public float AttackWait;
        public float Health = 70;
    }

    public void Run()
    {
        _camera = new Camera3D
        {
            Position = _playerPosition,
            Target = _playerPosition + _forward,
            Up = Vector3.UnitY,
            FovY = 76,
            Projection = CameraProjection.Perspective
        };

        // Subscribe to world load events from the menu system
        _menu.OnWorldLoaded += OnWorldLoaded;

        // Show cursor for menu
        Raylib.EnableCursor();
        Raylib.ShowCursor();

        while (!Raylib.WindowShouldClose())
        {
            var delta = Math.Clamp(Raylib.GetFrameTime(), 0, 0.05f);

            if (_menu.State != MenuState.Playing)
            {
                // In a menu screen
                _menu.Update(delta);
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(12, 18, 28, 255));
                _menu.Draw();
                Raylib.EndDrawing();
            }
            else
            {
                // Active gameplay
                Update(delta);
                Draw();
            }
        }

        // Save on exit if a world is active
        SaveCurrentWorldState();
        Raylib.EnableCursor();
        Raylib.ShowCursor();
    }

    private void OnWorldLoaded(WorldSaveData world)
    {
        // Apply world seed to terrain and random
        _terrain.SetWorld(world.Seed, world.RenderDistanceKm);
        _random = new Random(world.Seed);

        // Restore train state
        _trainZ = world.TrainZ;
        _throttle = world.Throttle;
        _brake = world.Brake;
        _train = new TrainSimulation(world.FuelChunks);
        _train.RestoreState(world.TrainSpeed, world.FuelChunks, world.Throttle, world.Brake);

        // Restore player state
        _playerPosition = world.PlayerPosition;
        _yaw = world.Yaw;
        _pitch = world.Pitch;
        _isOnTrain = world.IsOnTrain;
        _health = world.Health;
        _hunger = world.Hunger;
        _stamina = world.Stamina;

        // Restore clock
        _timeOfDay = world.TimeOfDay;
        _elapsed = world.Elapsed;

        // Restore inventory
        _inventory.Clear();
        foreach (var (item, count) in world.InventoryItems)
        {
            _inventory.Add(item, count);
        }

        // Restore collected nodes
        _collectedNodes.Clear();
        foreach (var id in world.CollectedNodeIds)
        {
            _collectedNodes.Add(id);
        }

        // Force resource re-generation around player
        _resourceCenterX = int.MinValue;
        _resourceCenterZ = int.MinValue;
        _resources.Clear();
        _enemies.Clear();

        // Recalculate forward vector from yaw/pitch
        var cosPitch = MathF.Cos(_pitch);
        _forward = Vector3.Normalize(new Vector3(MathF.Sin(_yaw) * cosPitch, MathF.Sin(_pitch), MathF.Cos(_yaw) * cosPitch));

        // Update camera
        _camera.Position = _playerPosition;
        _camera.Target = _playerPosition + _forward;

        // Reset autosave timer
        _autosaveTimer = 60f;

        // Enter gameplay mode with captured mouse
        ApplyMouseMode();
        ShowMessage($"Welcome to {world.WorldName}. Seed: {world.Seed}");
    }

    private void SaveCurrentWorldState()
    {
        var world = _menu.ActiveWorld;
        if (world == null) return;

        world.TrainZ = _trainZ;
        world.TrainSpeed = _train.SpeedMetersPerSecond;
        world.FuelChunks = _train.FuelChunks;
        world.Throttle = _throttle;
        world.Brake = _brake;

        world.PlayerPosition = _playerPosition;
        world.Yaw = _yaw;
        world.Pitch = _pitch;
        world.IsOnTrain = _isOnTrain;
        world.Health = _health;
        world.Hunger = _hunger;
        world.Stamina = _stamina;

        world.TimeOfDay = _timeOfDay;
        world.Elapsed = _elapsed;
        world.RenderDistanceKm = _menu.RenderDistanceKm;

        // Save inventory
        world.InventoryItems.Clear();
        foreach (var (item, count) in _inventory.Items)
        {
            world.InventoryItems[item] = count;
        }

        // Save collected nodes
        world.CollectedNodeIds = new List<long>(_collectedNodes);

        WorldSaveManager.SaveWorld(world);
    }

    private void ApplyMouseMode()
    {
        if (_mouseMode == 1)
        {
            Raylib.DisableCursor();
        }
        else
        {
            Raylib.EnableCursor();
            Raylib.HideCursor();
            _lastMousePosition = Raylib.GetMousePosition();
        }
        _mouseReset = true;
        Raylib.GetMouseDelta();
    }

    private Vector2 ReadMouseLook()
    {
        if (!Raylib.IsWindowFocused() || _craftingOpen || _inventoryOpen)
        {
            _mouseReset = true;
            return Vector2.Zero;
        }

        if (_mouseMode == 1)
        {
            var raw = Raylib.GetMouseDelta();
            if (_mouseReset)
            {
                _mouseReset = false;
                return Vector2.Zero;
            }
            return raw;
        }

        // Mode 0: relative tracking using delta from previous frame (never drifts)
        var position = Raylib.GetMousePosition();
        if (_mouseReset)
        {
            _mouseReset = false;
            _lastMousePosition = position;
            return Vector2.Zero;
        }

        var delta = position - _lastMousePosition;
        _lastMousePosition = position;

        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        if (position.X < 50 || position.X > screenW - 50 || position.Y < 50 || position.Y > screenH - 50)
        {
            var center = new Vector2(screenW / 2f, screenH / 2f);
            Raylib.SetMousePosition((int)center.X, (int)center.Y);
            _lastMousePosition = center;
        }

        return delta;
    }

    private void SeedInventory()
    {
        _inventory.Add(ItemId.Wood, 16);
        _inventory.Add(ItemId.Planks, 20);
        _inventory.Add(ItemId.Stick, 16);
        _inventory.Add(ItemId.Stone, 18);
        _inventory.Add(ItemId.Coal, 12);
        _inventory.Add(ItemId.IronOre, 8);
        _inventory.Add(ItemId.IronIngot, 8);
        _inventory.Add(ItemId.ScrapMetal, 18);
        _inventory.Add(ItemId.MechanicalParts, 4);
        _inventory.Add(ItemId.RawFood, 4);
        _inventory.Add(ItemId.Ammunition, 24);
        _inventory.Add(ItemId.Revolver, 1);
        _inventory.Add(ItemId.Workbench, 1);
        _inventory.Add(ItemId.Furnace, 1);
    }

    private void Update(float delta)
    {
        _elapsed += delta;
        _timeOfDay = (_timeOfDay + delta) % FullCycleSeconds;
        _attackCooldown = MathF.Max(0, _attackCooldown - delta);
        _messageTimer = MathF.Max(0, _messageTimer - delta);
        _tracerTimer = MathF.Max(0, _tracerTimer - delta);
        _hunger = MathF.Max(0, _hunger - delta * 0.11f);
        if (_hunger <= 0)
        {
            _health = MathF.Max(0, _health - delta * 1.8f);
        }
        if (_health <= 0)
        {
            Respawn();
        }

        // Autosave every 60 seconds
        _autosaveTimer -= delta;
        if (_autosaveTimer <= 0)
        {
            _autosaveTimer = 60f;
            SaveCurrentWorldState();
        }

        // Handle Escape key: open pause menu instead of quitting
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            if (_craftingOpen || _inventoryOpen)
            {
                // Close any open panel first
                _craftingOpen = false;
                _inventoryOpen = false;
                OnMenuToggled();
            }
            else
            {
                // Save and open pause menu
                SaveCurrentWorldState();
                _menu.OpenPauseMenu();

                // If menu returned to title, don't continue updating gameplay
                return;
            }
        }

        // If pause menu is now active (because OpenPauseMenu was called), stop gameplay updates
        if (_menu.State != MenuState.Playing) return;

        // Check if render distance changed from pause menu
        if (_terrain.RenderDistanceKm != _menu.RenderDistanceKm)
        {
            _terrain.SetRenderDistance(_menu.RenderDistanceKm);
        }

        UpdateTrain(delta);
        UpdatePlayer(delta);
        UpdateResources();
        UpdateEnemies(delta);
        UpdateStation();
        UpdateMenuInput();

        if (!_craftingOpen && !_inventoryOpen && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Attack();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.F))
        {
            Eat();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.X))
        {
            ToggleBoarding();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.E) && !_craftingOpen && !_inventoryOpen)
        {
            Interact();
        }
        if (_isOnTrain)
        {
            _playerPosition.Z += (float)_train.SpeedMetersPerSecond * delta;
        }
    }

    private void UpdateTrain(float delta)
    {
        var throttleDirection = (Raylib.IsKeyDown(KeyboardKey.Up) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.Down) ? 1f : 0f);
        var brakeDirection = (Raylib.IsKeyDown(KeyboardKey.Space) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.LeftControl) ? 1f : 0f);
        _throttle = Math.Clamp(_throttle + throttleDirection * delta * 0.34f, 0, 1);
        _brake = Math.Clamp(_brake + brakeDirection * delta * 0.8f, 0, 1);
        if (throttleDirection == 0)
        {
            _throttle = MathF.Max(0, _throttle - delta * 0.045f);
        }
        if (brakeDirection == 0)
        {
            _brake = MathF.Max(0, _brake - delta * 0.24f);
        }
        _train.SetControls(_throttle, _brake);
        _train.Tick(delta);
        _trainZ += (float)_train.SpeedMetersPerSecond * delta;
    }

    private void UpdatePlayer(float delta)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F3))
        {
            _showDebug = !_showDebug;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.F4))
        {
            _mouseMode = 1 - _mouseMode;
            ApplyMouseMode();
        }

        var isFocused = Raylib.IsWindowFocused();
        if (isFocused != _wasFocused)
        {
            _wasFocused = isFocused;
            if (isFocused && !_craftingOpen && !_inventoryOpen)
            {
                ApplyMouseMode();
            }
            else
            {
                Raylib.EnableCursor();
                Raylib.ShowCursor();
            }
            _mouseReset = true;
        }

        var mouse = ReadMouseLook();
        _debugRawMouse = mouse;
        // Ignore absurd single-frame jumps (focus changes, broken pointer devices, hitches).
        if (MathF.Abs(mouse.X) > 400f || MathF.Abs(mouse.Y) > 400f)
        {
            mouse = Vector2.Zero;
        }
        mouse = new Vector2(Math.Clamp(mouse.X, -120f, 120f), Math.Clamp(mouse.Y, -120f, 120f));
        // Raylib is right-handed: with forward = +Z, screen-right is -X, so moving the mouse right must DEcrease yaw.
        _yaw -= mouse.X * 0.0022f;
        _pitch = Math.Clamp(_pitch - mouse.Y * 0.0022f, -1.36f, 1.36f);

        // Keyboard look backup: Left/Right arrows turn, PageUp/PageDown look up/down.
        var keyYaw = (Raylib.IsKeyDown(KeyboardKey.Left) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.Right) ? 1f : 0f);
        var keyPitch = (Raylib.IsKeyDown(KeyboardKey.PageUp) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.PageDown) ? 1f : 0f);
        _yaw += keyYaw * 1.8f * delta;
        _pitch = Math.Clamp(_pitch + keyPitch * 1.2f * delta, -1.36f, 1.36f);
        _yaw = MathF.IEEERemainder(_yaw, MathF.Tau);
        var cosPitch = MathF.Cos(_pitch);
        _forward = Vector3.Normalize(new Vector3(MathF.Sin(_yaw) * cosPitch, MathF.Sin(_pitch), MathF.Cos(_yaw) * cosPitch));

        var forwardFlat = Vector3.Normalize(new Vector3(_forward.X, 0, _forward.Z));
        var right = new Vector3(-forwardFlat.Z, 0, forwardFlat.X); // forward x up
        var movement = Vector3.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) movement += forwardFlat;
        if (Raylib.IsKeyDown(KeyboardKey.S)) movement -= forwardFlat;
        if (Raylib.IsKeyDown(KeyboardKey.D)) movement += right;
        if (Raylib.IsKeyDown(KeyboardKey.A)) movement -= right;

        if (movement.LengthSquared() > 0)
        {
            movement = Vector3.Normalize(movement);
            var sprint = Raylib.IsKeyDown(KeyboardKey.LeftShift) && _stamina > 1;
            var speed = sprint ? 6.1f : 3.35f;
            if (sprint) _stamina = MathF.Max(0, _stamina - delta * 17);
            else _stamina = MathF.Min(100, _stamina + delta * 12);
            _playerPosition += movement * speed * delta;
        }
        else
        {
            _stamina = MathF.Min(100, _stamina + delta * 15);
        }

        var floor = _isOnTrain ? 3.42f : TerrainHeight(_playerPosition.X, _playerPosition.Z) + 1.72f;
        _playerPosition.Y = floor;
        _camera.Position = _playerPosition;
        _camera.Target = _playerPosition + _forward;
        _camera.Up = Vector3.UnitY;
    }

    private void UpdateResources()
    {
        var cellX = (int)MathF.Floor(_playerPosition.X / 55f);
        var cellZ = (int)MathF.Floor(_playerPosition.Z / 55f);
        if (cellX == _resourceCenterX && cellZ == _resourceCenterZ)
        {
            return;
        }
        _resourceCenterX = cellX;
        _resourceCenterZ = cellZ;
        _resources.Clear();

        var seed = _menu.ActiveWorld?.Seed ?? 731;
        for (var z = cellZ - 4; z <= cellZ + 4; z++)
        {
            for (var x = cellX - 4; x <= cellX + 4; x++)
            {
                var id = ((long)x << 32) ^ (uint)z;
                if (_collectedNodes.Contains(id)) continue;
                var hash = unchecked((uint)(x * 374761393 ^ z * 668265263 ^ seed * 1442695041));
                hash = (hash ^ (hash >> 13)) * 1274126177;
                var random = new Random((int)hash);
                if (random.NextDouble() > 0.44) continue;

                var itemRoll = random.Next(100);
                var item = itemRoll switch
                {
                    < 24 => ItemId.Wood,
                    < 43 => ItemId.Stone,
                    < 57 => ItemId.Coal,
                    < 71 => ItemId.IronOre,
                    < 84 => ItemId.ScrapMetal,
                    < 93 => ItemId.RawFood,
                    _ => ItemId.MechanicalParts
                };
                var px = x * 55f + 8 + (float)random.NextDouble() * 38;
                var pz = z * 55f + 8 + (float)random.NextDouble() * 38;
                if (MathF.Abs(px) < 24) continue;
                var amount = item is ItemId.Wood or ItemId.Stone or ItemId.Coal ? random.Next(2, 5) : random.Next(1, 3);
                _resources.Add(new ResourceNode(id, new Vector3(px, TerrainHeight(px, pz), pz), item, amount, ResourceColor(item)));
            }
        }
    }

    private void UpdateEnemies(float delta)
    {
        var isNight = _timeOfDay >= DaySeconds;
        if (!isNight || _isOnTrain)
        {
            _enemies.Clear();
            _enemySpawnTimer = 12;
            return;
        }
        _enemySpawnTimer -= delta;
        if (_enemySpawnTimer <= 0 && _enemies.Count < 4)
        {
            var angle = (float)_random.NextDouble() * MathF.Tau;
            var distance = 32 + (float)_random.NextDouble() * 38;
            var x = _playerPosition.X + MathF.Cos(angle) * distance;
            var z = _playerPosition.Z + MathF.Sin(angle) * distance;
            _enemies.Add(new Enemy { Position = new Vector3(x, TerrainHeight(x, z), z) });
            _enemySpawnTimer = 27 + (float)_random.NextDouble() * 22;
        }

        for (var i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            var toPlayer = new Vector3(_playerPosition.X - enemy.Position.X, 0, _playerPosition.Z - enemy.Position.Z);
            var distance = toPlayer.Length();
            enemy.AttackWait -= delta;
            if (distance > 1.6f)
            {
                enemy.Position += Vector3.Normalize(toPlayer) * 1.75f * delta;
                enemy.Position.Y = TerrainHeight(enemy.Position.X, enemy.Position.Z);
            }
            else if (enemy.AttackWait <= 0)
            {
                _health = MathF.Max(0, _health - 12);
                enemy.AttackWait = 1.7f;
                ShowMessage("A Forgotten hit you. Fight back or get clear.");
            }
            if (enemy.Health <= 0)
            {
                _enemies.RemoveAt(i);
            }
        }
    }

    private void UpdateStation()
    {
        var bench = new Vector3(1.1f, 3.2f, _trainZ - 6.6f);
        var furnace = new Vector3(-0.9f, 3.0f, _trainZ - 4.0f);
        if (_isOnTrain && Vector3.Distance(_playerPosition, bench) < 4.2f)
        {
            _station = CraftingStation.Workbench;
        }
        else if (_isOnTrain && Vector3.Distance(_playerPosition, furnace) < 4.2f)
        {
            _station = CraftingStation.Furnace;
        }
        else
        {
            _station = CraftingStation.Hand;
        }
    }

    private void UpdateMenuInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.C))
        {
            _craftingOpen = !_craftingOpen;
            _inventoryOpen = false;
            OnMenuToggled();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.I))
        {
            _inventoryOpen = !_inventoryOpen;
            _craftingOpen = false;
            OnMenuToggled();
        }
        if (_craftingOpen)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Down)) _recipeIndex = (_recipeIndex + 1) % CraftingCatalog.Recipes.Count;
            if (Raylib.IsKeyPressed(KeyboardKey.Up)) _recipeIndex = (_recipeIndex - 1 + CraftingCatalog.Recipes.Count) % CraftingCatalog.Recipes.Count;
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                var recipe = CraftingCatalog.Recipes[_recipeIndex];
                if (CraftingService.TryCraft(_inventory, recipe, _station)) ShowMessage($"Crafted {recipe.Name}");
                else ShowMessage("Missing ingredients or the required station");
            }
        }
        for (var slot = 0; slot < _hotbar.Length; slot++)
        {
            if (Raylib.IsKeyPressed((KeyboardKey)((int)KeyboardKey.One + slot))) _selectedSlot = slot;
        }
    }

    private void OnMenuToggled()
    {
        if (_craftingOpen || _inventoryOpen)
        {
            Raylib.EnableCursor();
            Raylib.ShowCursor();
        }
        else
        {
            ApplyMouseMode();
        }
        _mouseReset = true;
    }

    private void Interact()
    {
        if (ClosestResource(out var index))
        {
            var node = _resources[index];
            var added = _inventory.Add(node.Item, node.Amount);
            if (added > 0)
            {
                _collectedNodes.Add(node.Id);
                _resources.RemoveAt(index);
                ShowMessage($"Collected {added} {DisplayName(node.Item)}");
            }
            else ShowMessage("Inventory full");
            return;
        }

        var firebox = new Vector3(-0.2f, 3.2f, _trainZ - 4.1f);
        if (_isOnTrain && Vector3.Distance(_playerPosition, firebox) < 3f)
        {
            if (_inventory.Remove(ItemId.Coal, 1))
            {
                _train.AddFuel(1);
                ShowMessage("Coal fed to the firebox");
            }
            else ShowMessage("You need coal to feed the locomotive");
            return;
        }
        ShowMessage("Nothing close enough to use");
    }

    private bool ClosestResource(out int bestIndex)
    {
        bestIndex = -1;
        var bestDistance = 4.6f;
        for (var i = 0; i < _resources.Count; i++)
        {
            var target = _resources[i].Position + Vector3.UnitY * 0.8f;
            var toTarget = target - _camera.Position;
            var distance = toTarget.Length();
            if (distance > bestDistance || distance <= 0.01f) continue;
            var alignment = Vector3.Dot(Vector3.Normalize(toTarget), _forward);
            if (alignment < 0.93f) continue;
            bestDistance = distance;
            bestIndex = i;
        }
        return bestIndex >= 0;
    }

    private void Attack()
    {
        if (_attackCooldown > 0) return;
        var weapon = _hotbar[_selectedSlot];
        var isGun = weapon is ItemId.Revolver or ItemId.Rifle or ItemId.Shotgun;
        var isMelee = weapon is ItemId.WoodenSword or ItemId.StoneSword or ItemId.IronSword or ItemId.WoodenAxe or ItemId.StoneAxe or ItemId.IronAxe;
        if (!_inventory.Has(weapon, 1) || (!isGun && !isMelee))
        {
            ShowMessage("Select a crafted weapon in the hotbar");
            _attackCooldown = 0.3f;
            return;
        }
        if (isGun && !_inventory.Remove(ItemId.Ammunition, 1))
        {
            ShowMessage("Out of ammunition");
            _attackCooldown = 0.4f;
            return;
        }

        var range = isGun ? 95f : 2.8f;
        var hit = false;
        foreach (var enemy in _enemies)
        {
            var target = enemy.Position + Vector3.UnitY * 1.2f;
            var toTarget = target - _camera.Position;
            var distance = toTarget.Length();
            if (distance > range || Vector3.Dot(Vector3.Normalize(toTarget), _forward) < 0.95f) continue;
            enemy.Health -= weapon == ItemId.Shotgun ? 48 : weapon == ItemId.Rifle ? 40 : isGun ? 32 : 28;
            hit = true;
            if (enemy.Health <= 0)
            {
                _inventory.Add(ItemId.ScrapMetal, 2);
                _inventory.Add(ItemId.MechanicalParts, 1);
                ShowMessage("Forgotten defeated. Salvage recovered.");
            }
            break;
        }
        if (isGun)
        {
            _tracerStart = _camera.Position + _forward * 0.5f;
            _tracerEnd = _camera.Position + _forward * (hit ? 20f : 42f);
            _tracerTimer = 0.07f;
        }
        else if (!hit) ShowMessage("You swing into the dark");
        _attackCooldown = weapon == ItemId.Shotgun ? 0.85f : isGun ? 0.42f : 0.55f;
    }

    private void Eat()
    {
        if (_inventory.Remove(ItemId.CookedFood, 1))
        {
            _hunger = MathF.Min(100, _hunger + 38);
            _health = MathF.Min(100, _health + 8);
            ShowMessage("Ate a cooked ration");
        }
        else if (_inventory.Remove(ItemId.RawFood, 1))
        {
            _hunger = MathF.Min(100, _hunger + 16);
            ShowMessage("Ate raw food. Better to cook it next time.");
        }
        else ShowMessage("No food available");
    }

    private void ToggleBoarding()
    {
        var distance = Vector2.Distance(new Vector2(_playerPosition.X, _playerPosition.Z), new Vector2(0, _trainZ));
        if (_isOnTrain)
        {
            if (_train.SpeedMetersPerSecond > 8)
            {
                ShowMessage("Slow the train before getting off");
                return;
            }
            _isOnTrain = false;
            _playerPosition.X = 3.8f;
            ShowMessage("You stepped down beside the train");
        }
        else if (distance < 12 && _train.SpeedMetersPerSecond < 8)
        {
            _isOnTrain = true;
            _playerPosition.X = 0;
            _playerPosition.Z = _trainZ - 5.1f;
            ShowMessage("Back aboard");
        }
        else ShowMessage("Move close to the stopped train to board");
    }

    private void Respawn()
    {
        _health = 70;
        _hunger = MathF.Max(35, _hunger);
        _playerPosition = new Vector3(0, 3.45f, _trainZ - 5.1f);
        _isOnTrain = true;
        ShowMessage("You recovered aboard the train. Some time has passed.");
    }

    private void Draw()
    {
        var lighting = LightingEngine.Compute(
            _timeOfDay,
            FullCycleSeconds,
            DaySeconds,
            _playerPosition,
            _trainZ,
            _train.FuelChunks,
            _elapsed);

        Raylib.BeginDrawing();
        Raylib.ClearBackground(lighting.SkyHorizon);
        // Raylib's default clip planes (0.01 .. 1000) wreck depth precision and cut off the 11 km world.
        Rlgl.SetClipPlanes(0.2, 220000.0);
        LightingEngine.DrawSky(lighting);

        Raylib.BeginMode3D(_camera);
        LightingEngine.DrawCelestialAndAtmosphere(lighting, _playerPosition);
        _terrain.Draw(_playerPosition, lighting);
        _trainRenderer.Draw(_trainZ, (float)_train.SpeedMetersPerSecond, lighting);
        DrawResources(lighting);
        DrawEnemies(lighting);
        if (_tracerTimer > 0) Raylib.DrawLine3D(_tracerStart, _tracerEnd, new Color(255, 203, 120, 255));
        Raylib.EndMode3D();

        DrawHud();
        if (_showDebug) DrawDebug(lighting);

        // Draw pause menu overlay on top of the 3D scene
        if (_menu.State == MenuState.PauseMenu)
        {
            _menu.Update(Raylib.GetFrameTime());
            _menu.Draw();

            // If pause menu transitioned back to playing, re-capture the mouse
            if (_menu.State == MenuState.Playing)
            {
                ApplyMouseMode();
            }
        }

        Raylib.EndDrawing();
    }

    private void DrawResources(LightingContext lighting)
    {
        foreach (var node in _resources)
        {
            var position = node.Position + Vector3.UnitY * 0.72f;
            var litColor = LightingEngine.ShadeSurface(node.Color, Vector3.UnitY, position, lighting, 0.5f, 0.35f);
            Raylib.DrawCube(position, 0.9f, 0.9f, 0.9f, litColor);
            Raylib.DrawCubeWires(position, 0.92f, 0.92f, 0.92f, new Color(209, 207, 184, 180));
        }
    }

    private void DrawEnemies(LightingContext lighting)
    {
        foreach (var enemy in _enemies)
        {
            var body = new Vector3(enemy.Position.X, enemy.Position.Y + 1.0f, enemy.Position.Z);
            var bodyColor = LightingEngine.ShadeSurface(new Color(68, 74, 66, 255), Vector3.UnitY, body, lighting, 0.8f, 0.1f);
            var headColor = LightingEngine.ShadeSurface(new Color(92, 100, 86, 255), Vector3.UnitY, body, lighting, 0.8f, 0.1f);
            Raylib.DrawCube(body, 0.82f, 1.35f, 0.55f, bodyColor);
            Raylib.DrawSphere(new Vector3(body.X, body.Y + 0.95f, body.Z), 0.34f, headColor);

            // Glowing emissive orange eyes
            Raylib.DrawSphere(new Vector3(body.X - 0.12f, body.Y + 1.0f, body.Z + 0.28f), 0.045f, new Color(255, 105, 30, 255));
            Raylib.DrawSphere(new Vector3(body.X + 0.12f, body.Y + 1.0f, body.Z + 0.28f), 0.045f, new Color(255, 105, 30, 255));
        }
    }

    private void DrawDebug(LightingContext lighting)
    {
        var y = Raylib.GetScreenHeight() - 195;
        Raylib.DrawRectangle(18, y - 6, 620, 158, new Color(0, 0, 0, 170));
        Raylib.DrawText($"DEBUG (F3 hides)  FPS {Raylib.GetFPS()}  focused {Raylib.IsWindowFocused()}", 28, y, 16, Color.Yellow);
        Raylib.DrawText($"mouse mode {(_mouseMode == 1 ? "captured (raw)" : "relative (free)")} (F4 switches)  turn: Left/Right arrows, look: PgUp/PgDn", 28, y + 96, 14, Color.Yellow);
        Raylib.DrawText($"lighting {lighting.HourOfDay:00.0}h  daylight {lighting.Daylight * 100:0}%  golden {lighting.GoldenHour * 100:0}%  night {lighting.Night * 100:0}%", 28, y + 24, 15, Color.Yellow);
        Raylib.DrawText($"yaw {_yaw:0.00}  pitch {_pitch:0.00}", 28, y + 48, 16, Color.Yellow);
        Raylib.DrawText($"pos {_playerPosition.X:0.0} {_playerPosition.Y:0.0} {_playerPosition.Z:0.0}", 28, y + 72, 16, Color.Yellow);
        var worldInfo = _menu.ActiveWorld != null
            ? $"world \"{_menu.ActiveWorld.WorldName}\"  seed {_menu.ActiveWorld.Seed}  render {_menu.RenderDistanceKm}km  horizon 200km"
            : "world: default";
        Raylib.DrawText(worldInfo, 28, y + 118, 14, Color.Yellow);
    }

    private void DrawHud()
    {
        var width = Raylib.GetScreenWidth();
        var height = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(18, 18, 335, 132, new Color(16, 23, 24, 210));
        Raylib.DrawText("TRACKBOUND   /   FIELD CAB", 32, 28, 18, new Color(228, 218, 187, 255));
        Raylib.DrawText($"{_train.SpeedMetersPerSecond * 3.6:00} km/h     {_train.FuelChunks:00} coal", 32, 57, 20, Color.White);
        Raylib.DrawText($"{_trainZ / 1000:0.00} km     {(_isOnTrain ? "ABOARD" : "ON FOOT")}", 32, 88, 16, new Color(165, 181, 171, 255));
        DrawBar(32, 118, 142, 13, _health / 100, new Color(177, 74, 62, 255), "HEALTH");
        DrawBar(194, 118, 142, 13, _hunger / 100, new Color(192, 143, 61, 255), "HUNGER");
        DrawBar(32, 143, 142, 7, _stamina / 100, new Color(93, 157, 134, 255), "");

        Raylib.DrawRectangle(width - 275, 18, 257, 122, new Color(16, 23, 24, 210));
        Raylib.DrawText("WASD move  |  Mouse look", width - 260, 28, 15, Color.White);
        Raylib.DrawText("Up/Down throttle  |  Space brake", width - 260, 52, 14, Color.White);
        Raylib.DrawText("E gather/feed  |  C recipes  |  I bag", width - 260, 76, 14, Color.White);
        Raylib.DrawText("F eat  |  1-5 select  |  LMB use", width - 260, 100, 14, Color.White);

        var centerX = width / 2;
        var centerY = height / 2;
        Raylib.DrawLine(centerX - 9, centerY, centerX + 9, centerY, new Color(245, 228, 190, 220));
        Raylib.DrawLine(centerX, centerY - 9, centerX, centerY + 9, new Color(245, 228, 190, 220));

        var day = _timeOfDay < DaySeconds;
        var hours = (int)(_timeOfDay / 60f) % 24;
        var minutes = (int)_timeOfDay % 60;
        var status = $"{(day ? "DAY" : "NIGHT")}  {hours:00}:{minutes:00}     {(_station == CraftingStation.Hand ? "FIELD CRAFTING" : _station.ToString().ToUpperInvariant())}";
        Raylib.DrawText(status, width / 2 - 220, 20, 18, new Color(238, 225, 191, 255));

        var hotbarWidth = 5 * 82 + 8 * 4;
        var startX = (width - hotbarWidth) / 2;
        var hotbarY = height - 94;
        for (var i = 0; i < _hotbar.Length; i++)
        {
            var x = startX + i * 86;
            var active = i == _selectedSlot;
            Raylib.DrawRectangle(x, hotbarY, 82, 76, active ? new Color(82, 91, 78, 235) : new Color(22, 30, 31, 220));
            Raylib.DrawRectangleLines(x, hotbarY, 82, 76, active ? new Color(222, 188, 119, 255) : new Color(99, 105, 97, 255));
            Raylib.DrawText((i + 1).ToString(), x + 7, hotbarY + 5, 14, new Color(210, 203, 181, 255));
            Raylib.DrawText(ShortName(_hotbar[i]), x + 7, hotbarY + 30, 13, Color.White);
            var amount = _inventory.Count(_hotbar[i]);
            if (amount > 0) Raylib.DrawText(amount.ToString(), x + 61, hotbarY + 55, 14, new Color(234, 216, 172, 255));
        }

        if (_messageTimer > 0)
        {
            var boxWidth = Math.Min(760, width - 32);
            Raylib.DrawRectangle((width - boxWidth) / 2, height - 139, boxWidth, 30, new Color(17, 24, 25, 220));
            Raylib.DrawText(_message, (width - boxWidth) / 2 + 12, height - 132, 16, new Color(237, 227, 199, 255));
        }

        if (_craftingOpen) DrawCrafting(width, height);
        if (_inventoryOpen) DrawInventory(width, height);
    }

    private void DrawCrafting(int width, int height)
    {
        var panelWidth = Math.Min(720, width - 36);
        var panelHeight = Math.Min(650, height - 80);
        var x = (width - panelWidth) / 2;
        var y = (height - panelHeight) / 2;
        Raylib.DrawRectangle(x, y, panelWidth, panelHeight, new Color(20, 28, 29, 248));
        Raylib.DrawRectangleLines(x, y, panelWidth, panelHeight, new Color(161, 137, 95, 255));
        Raylib.DrawText("FIELD RECIPES", x + 24, y + 20, 24, new Color(232, 215, 177, 255));
        Raylib.DrawText($"Station: {_station}     Up/Down select     Enter craft     C close", x + 24, y + 56, 15, new Color(173, 186, 173, 255));

        var visible = Math.Min(13, Math.Max(1, (panelHeight - 145) / 32));
        var first = Math.Clamp(_recipeIndex - visible / 2, 0, Math.Max(0, CraftingCatalog.Recipes.Count - visible));
        for (var row = 0; row < visible; row++)
        {
            var index = first + row;
            if (index >= CraftingCatalog.Recipes.Count) break;
            var recipe = CraftingCatalog.Recipes[index];
            var rowY = y + 94 + row * 32;
            if (index == _recipeIndex) Raylib.DrawRectangle(x + 14, rowY - 3, panelWidth - 28, 28, new Color(68, 78, 68, 255));
            var canCraft = CraftingService.CanCraft(_inventory, recipe, _station);
            var color = canCraft ? new Color(232, 226, 202, 255) : new Color(133, 140, 132, 255);
            Raylib.DrawText(recipe.Name, x + 26, rowY, 16, color);
            Raylib.DrawText($"{recipe.OutputCount} x {DisplayName(recipe.Output)}", x + panelWidth - 218, rowY, 14, color);
        }

        var selected = CraftingCatalog.Recipes[_recipeIndex];
        var requirementY = y + panelHeight - 102;
        Raylib.DrawLine(x + 20, requirementY - 10, x + panelWidth - 20, requirementY - 10, new Color(85, 94, 84, 255));
        var requirements = string.Join("   ", selected.Ingredients.Select(pair => $"{ShortName(pair.Key)} {_inventory.Count(pair.Key)}/{pair.Value}"));
        Raylib.DrawText(requirements, x + 26, requirementY, 14, new Color(226, 196, 142, 255));
        Raylib.DrawText("Hand recipes work anywhere; tools and guns need the locomotive workbench.", x + 26, requirementY + 28, 13, new Color(151, 164, 155, 255));
    }

    private void DrawInventory(int width, int height)
    {
        var panelWidth = Math.Min(660, width - 36);
        var panelHeight = Math.Min(600, height - 90);
        var x = (width - panelWidth) / 2;
        var y = (height - panelHeight) / 2;
        Raylib.DrawRectangle(x, y, panelWidth, panelHeight, new Color(20, 28, 29, 246));
        Raylib.DrawRectangleLines(x, y, panelWidth, panelHeight, new Color(161, 137, 95, 255));
        Raylib.DrawText("PACK", x + 24, y + 20, 24, new Color(232, 215, 177, 255));
        Raylib.DrawText($"{_inventory.Items.Count}/{Inventory.SlotLimit} stacks", x + 24, y + 58, 15, new Color(173, 186, 173, 255));

        var items = _inventory.Items.OrderBy(pair => pair.Key).ToArray();
        for (var i = 0; i < items.Length; i++)
        {
            var column = i % 2;
            var row = i / 2;
            var itemX = x + 30 + column * ((panelWidth - 60) / 2);
            var itemY = y + 94 + row * 32;
            Raylib.DrawRectangle(itemX, itemY, 20, 20, ResourceColor(items[i].Key));
            Raylib.DrawText($"{DisplayName(items[i].Key)}  x{items[i].Value}", itemX + 32, itemY + 1, 15, Color.White);
        }
        Raylib.DrawText("I close   |   C opens recipes", x + 24, y + panelHeight - 34, 15, new Color(190, 187, 168, 255));
    }

    private static void DrawBar(int x, int y, int width, int height, float value, Color color, string label)
    {
        Raylib.DrawRectangle(x, y, width, height, new Color(49, 54, 51, 255));
        Raylib.DrawRectangle(x, y, (int)(width * Math.Clamp(value, 0, 1)), height, color);
        if (label.Length > 0) Raylib.DrawText(label, x, y - 13, 10, new Color(207, 207, 190, 255));
    }

    private void ShowMessage(string message)
    {
        _message = message;
        _messageTimer = 2.8f;
    }

    private float Daylight()
    {
        var phase = _timeOfDay / FullCycleSeconds;
        if (_timeOfDay <= DaySeconds)
        {
            var dayPhase = _timeOfDay / DaySeconds;
            return 0.12f + 0.88f * MathF.Sin(dayPhase * MathF.PI);
        }
        return 0.025f + 0.035f * (1 + MathF.Sin(phase * MathF.Tau));
    }

    /// <summary>
    /// Returns terrain height using the TerrainRenderer's civil-engineered railway grade
    /// with natural mountains in all directions (no artificial valley walls).
    /// </summary>
    private float TerrainHeight(float x, float z)
    {
        return _terrain.Height(x, z);
    }

    private static Color ResourceColor(ItemId item) => item switch
    {
        ItemId.Wood => new Color(109, 75, 48, 255),
        ItemId.Stone => new Color(122, 128, 122, 255),
        ItemId.Coal => new Color(38, 41, 40, 255),
        ItemId.IronOre => new Color(151, 91, 65, 255),
        ItemId.ScrapMetal => new Color(174, 135, 76, 255),
        ItemId.RawFood => new Color(103, 143, 76, 255),
        ItemId.MechanicalParts => new Color(77, 133, 133, 255),
        _ => new Color(110, 110, 110, 255)
    };

    private static string DisplayName(ItemId item) => string.Concat(item.ToString().Select((character, index) => index > 0 && char.IsUpper(character) ? " " + character : character.ToString()));
    private static string ShortName(ItemId item) => item switch
    {
        ItemId.WoodenSword => "Wood sword",
        ItemId.StoneSword => "Stone sword",
        ItemId.IronSword => "Iron sword",
        ItemId.WoodenAxe => "Wood axe",
        ItemId.StoneAxe => "Stone axe",
        ItemId.IronAxe => "Iron axe",
        _ => DisplayName(item)
    };
    private static float SmoothStep(float a, float b, float value)
    {
        var t = Math.Clamp((value - a) / (b - a), 0, 1);
        return t * t * (3 - 2 * t);
    }
    private static Color ColorLerp(Color a, Color b, float t) => new(
        (byte)Math.Clamp((int)(a.R + (b.R - a.R) * t), 0, 255),
        (byte)Math.Clamp((int)(a.G + (b.G - a.G) * t), 0, 255),
        (byte)Math.Clamp((int)(a.B + (b.B - a.B) * t), 0, 255),
        (byte)255);
}