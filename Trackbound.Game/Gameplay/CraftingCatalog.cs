namespace Trackbound.Gameplay;

public sealed record Recipe(
    string Name,
    CraftingStation Station,
    ItemId Output,
    int OutputCount,
    IReadOnlyDictionary<ItemId, int> Ingredients);

public static class CraftingCatalog
{
    private static IReadOnlyDictionary<ItemId, int> Cost(params (ItemId Item, int Count)[] ingredients) =>
        ingredients.ToDictionary(pair => pair.Item, pair => pair.Count);

    public static IReadOnlyList<Recipe> Recipes { get; } = new List<Recipe>
    {
        new("Wood planks", CraftingStation.Hand, ItemId.Planks, 4, Cost((ItemId.Wood, 1))),
        new("Sticks", CraftingStation.Hand, ItemId.Stick, 4, Cost((ItemId.Planks, 2))),
        new("Campfire", CraftingStation.Hand, ItemId.Furnace, 1, Cost((ItemId.Stone, 8), (ItemId.Wood, 2))),
        new("Workbench", CraftingStation.Hand, ItemId.Workbench, 1, Cost((ItemId.Planks, 4))),

        new("Wood pickaxe", CraftingStation.Workbench, ItemId.WoodenPickaxe, 1, Cost((ItemId.Planks, 3), (ItemId.Stick, 2))),
        new("Wood axe", CraftingStation.Workbench, ItemId.WoodenAxe, 1, Cost((ItemId.Planks, 3), (ItemId.Stick, 2))),
        new("Wood sword", CraftingStation.Workbench, ItemId.WoodenSword, 1, Cost((ItemId.Planks, 2), (ItemId.Stick, 1))),
        new("Stone pickaxe", CraftingStation.Workbench, ItemId.StonePickaxe, 1, Cost((ItemId.Stone, 3), (ItemId.Stick, 2))),
        new("Stone axe", CraftingStation.Workbench, ItemId.StoneAxe, 1, Cost((ItemId.Stone, 3), (ItemId.Stick, 2))),
        new("Stone sword", CraftingStation.Workbench, ItemId.StoneSword, 1, Cost((ItemId.Stone, 2), (ItemId.Stick, 1))),
        new("Smelt iron ingot", CraftingStation.Furnace, ItemId.IronIngot, 1, Cost((ItemId.IronOre, 1), (ItemId.Coal, 1))),
        new("Iron pickaxe", CraftingStation.Workbench, ItemId.IronPickaxe, 1, Cost((ItemId.IronIngot, 3), (ItemId.Stick, 2))),
        new("Iron axe", CraftingStation.Workbench, ItemId.IronAxe, 1, Cost((ItemId.IronIngot, 3), (ItemId.Stick, 2))),
        new("Iron sword", CraftingStation.Workbench, ItemId.IronSword, 1, Cost((ItemId.IronIngot, 2), (ItemId.Stick, 1))),
        new("Revolver", CraftingStation.Workbench, ItemId.Revolver, 1, Cost((ItemId.ScrapMetal, 8), (ItemId.IronIngot, 3), (ItemId.Wood, 2), (ItemId.MechanicalParts, 1))),
        new("Rifle", CraftingStation.Workbench, ItemId.Rifle, 1, Cost((ItemId.ScrapMetal, 14), (ItemId.IronIngot, 6), (ItemId.Wood, 3), (ItemId.MechanicalParts, 2))),
        new("Shotgun", CraftingStation.Workbench, ItemId.Shotgun, 1, Cost((ItemId.ScrapMetal, 12), (ItemId.IronIngot, 5), (ItemId.Wood, 3), (ItemId.MechanicalParts, 2))),
        new("Ammunition x12", CraftingStation.Workbench, ItemId.Ammunition, 12, Cost((ItemId.IronIngot, 1), (ItemId.ScrapMetal, 2))),
        new("Cooked ration", CraftingStation.Furnace, ItemId.CookedFood, 1, Cost((ItemId.RawFood, 1), (ItemId.Wood, 1)))
    };
}

public static class CraftingService
{
    public static bool CanCraft(Inventory inventory, Recipe recipe, CraftingStation station)
    {
        var stationMatches = recipe.Station == CraftingStation.Hand
            || station == recipe.Station;
        return stationMatches && recipe.Ingredients.All(pair => inventory.Has(pair.Key, pair.Value));
    }

    public static bool TryCraft(Inventory inventory, Recipe recipe, CraftingStation station)
    {
        if (!CanCraft(inventory, recipe, station))
        {
            return false;
        }

        foreach (var ingredient in recipe.Ingredients)
        {
            inventory.Remove(ingredient.Key, ingredient.Value);
        }

        var added = inventory.Add(recipe.Output, recipe.OutputCount);
        if (added == recipe.OutputCount)
        {
            return true;
        }

        inventory.Remove(recipe.Output, added);
        foreach (var ingredient in recipe.Ingredients)
        {
            inventory.Add(ingredient.Key, ingredient.Value);
        }
        return false;
    }
}