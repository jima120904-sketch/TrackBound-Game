using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Trackbound.Gameplay;

namespace Trackbound.Runtime;

public sealed class WorldSaveData
{
    public string WorldName { get; set; } = "Wilderness";
    public int Seed { get; set; } = 731;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastPlayed { get; set; } = DateTime.UtcNow;

    // Simulation & clock
    public float TimeOfDay { get; set; } = 12 * 60f;
    public float Elapsed { get; set; }
    public int DayNumber { get; set; } = 1;

    // Train state
    public float TrainZ { get; set; } = 0f;
    public double TrainSpeed { get; set; } = 0.0;
    public double FuelChunks { get; set; } = 75.0;
    public float Throttle { get; set; } = 0f;
    public float Brake { get; set; } = 0f;

    // Player state
    public Vector3 PlayerPosition { get; set; } = new(0, 3.45f, -5.1f);
    public float Yaw { get; set; } = 0f;
    public float Pitch { get; set; } = 0f;
    public bool IsOnTrain { get; set; } = true;
    public bool TrainDoorOpen { get; set; } = false;
    public float Health { get; set; } = 100f;
    public float Hunger { get; set; } = 100f;
    public float Stamina { get; set; } = 100f;

    // Settings
    public int RenderDistanceKm { get; set; } = 50; // 10, 25, 50, 100 km (with 200km horizon)

    // Inventory & persistent world items
    public Dictionary<ItemId, int> InventoryItems { get; set; } = new();
    public List<long> CollectedNodeIds { get; set; } = new();
}

public static class WorldSaveManager
{
    private static readonly string SavesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    static WorldSaveManager()
    {
        try
        {
            if (!Directory.Exists(SavesDirectory))
            {
                Directory.CreateDirectory(SavesDirectory);
            }
        }
        catch
        {
            // Fallback to local working directory
            SavesDirectory = "Saves";
            if (!Directory.Exists(SavesDirectory))
            {
                Directory.CreateDirectory(SavesDirectory);
            }
        }
    }

    public static List<WorldSaveData> ListWorlds()
    {
        var list = new List<WorldSaveData>();
        if (!Directory.Exists(SavesDirectory)) return list;

        foreach (var file in Directory.GetFiles(SavesDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var save = JsonSerializer.Deserialize<WorldSaveData>(json);
                if (save != null)
                {
                    list.Add(save);
                }
            }
            catch
            {
                // Ignore corrupted or incompatible save file
            }
        }

        list.Sort((a, b) => b.LastPlayed.CompareTo(a.LastPlayed));
        return list;
    }

    public static bool SaveWorld(WorldSaveData save)
    {
        try
        {
            if (!Directory.Exists(SavesDirectory))
            {
                Directory.CreateDirectory(SavesDirectory);
            }

            save.LastPlayed = DateTime.UtcNow;
            var fileName = SanitizeFileName(save.WorldName) + ".json";
            var filePath = Path.Combine(SavesDirectory, fileName);
            var json = JsonSerializer.Serialize(save, JsonOptions);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WorldSaveManager] Failed to save world: {ex.Message}");
            return false;
        }
    }

    public static WorldSaveData? LoadWorld(string worldName)
    {
        try
        {
            var fileName = SanitizeFileName(worldName) + ".json";
            var filePath = Path.Combine(SavesDirectory, fileName);
            if (!File.Exists(filePath)) return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<WorldSaveData>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WorldSaveManager] Failed to load world: {ex.Message}");
            return null;
        }
    }

    public static bool DeleteWorld(string worldName)
    {
        try
        {
            var fileName = SanitizeFileName(worldName) + ".json";
            var filePath = Path.Combine(SavesDirectory, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
        }
        catch
        {
            // Ignore error
        }
        return false;
    }

    public static WorldSaveData CreateDefaultWorld(string name, int? seed = null)
    {
        var random = new Random();
        var worldSeed = seed ?? random.Next(1000, 999999);
        var save = new WorldSaveData
        {
            WorldName = string.IsNullOrWhiteSpace(name) ? $"Expedition_{worldSeed % 1000:000}" : name.Trim(),
            Seed = worldSeed,
            CreatedAt = DateTime.UtcNow,
            LastPlayed = DateTime.UtcNow,
            TimeOfDay = 12 * 60f,
            TrainZ = 0f,
            TrainSpeed = 0.0,
            FuelChunks = 75.0,
            RenderDistanceKm = 50
        };

        // Default initial survival kit
        save.InventoryItems[ItemId.Wood] = 16;
        save.InventoryItems[ItemId.Planks] = 20;
        save.InventoryItems[ItemId.Stick] = 16;
        save.InventoryItems[ItemId.Stone] = 18;
        save.InventoryItems[ItemId.Coal] = 12;
        save.InventoryItems[ItemId.IronOre] = 8;
        save.InventoryItems[ItemId.IronIngot] = 8;
        save.InventoryItems[ItemId.ScrapMetal] = 18;
        save.InventoryItems[ItemId.MechanicalParts] = 4;
        save.InventoryItems[ItemId.RawFood] = 4;
        save.InventoryItems[ItemId.Ammunition] = 24;
        save.InventoryItems[ItemId.Revolver] = 1;
        save.InventoryItems[ItemId.Workbench] = 1;
        save.InventoryItems[ItemId.Furnace] = 1;

        SaveWorld(save);
        return save;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalid, chars[i]) >= 0 || chars[i] == ' ')
            {
                chars[i] = '_';
            }
        }
        return new string(chars);
    }
}
