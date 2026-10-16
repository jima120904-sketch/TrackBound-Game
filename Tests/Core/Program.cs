using Trackbound.Core;
using Trackbound.Gameplay;

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

var terrainA = ProceduralTerrain.Height(1234.5, -987.25, 731);
var terrainB = ProceduralTerrain.Height(1234.5, -987.25, 731);
Require(terrainA == terrainB, "Terrain generation must be deterministic for a seed and coordinate.");
Require(double.IsFinite(terrainA), "Terrain generation must produce finite heights.");
Require(ProceduralTerrain.Height(1234.5, -987.25, 732) != terrainA, "Different seeds should change terrain.");

var highestPeak = double.MinValue;
for (var x = -24000; x <= 24000; x += 512)
{
    for (var z = -24000; z <= 24000; z += 512)
    {
        highestPeak = Math.Max(highestPeak, ProceduralTerrain.Height(x, z, 731));
    }
}
Require(highestPeak > 1000, "Procedural terrain should include substantial mountain relief.");

var train = new TrainSimulation(20);
train.SetControls(1, 0);
for (var i = 0; i < 10; i++)
{
    train.Tick(0.1);
}
Require(train.SpeedMetersPerSecond > 0, "Throttle and available fuel should accelerate the train.");
Require(train.FuelChunks < 20, "Engine load should consume fuel.");

var speedBeforeBraking = train.SpeedMetersPerSecond;
train.SetControls(0, 1);
for (var i = 0; i < 5; i++)
{
    train.Tick(0.1);
}
Require(train.SpeedMetersPerSecond < speedBeforeBraking, "Brake input should reduce speed.");

var emptyTrain = new TrainSimulation(0);
emptyTrain.SetControls(1, 0);
emptyTrain.Tick(1);
Require(emptyTrain.SpeedMetersPerSecond == 0, "An empty firebox must not produce traction.");

var inventory = new Inventory();
inventory.Add(ItemId.Wood, 1);
var planksRecipe = CraftingCatalog.Recipes.Single(recipe => recipe.Output == ItemId.Planks);
Require(CraftingService.TryCraft(inventory, planksRecipe, CraftingStation.Hand), "Hand crafting should produce planks from wood.");
Require(inventory.Count(ItemId.Planks) == 4 && inventory.Count(ItemId.Wood) == 0, "Crafting should atomically consume and create items.");

var rifleRecipe = CraftingCatalog.Recipes.Single(recipe => recipe.Output == ItemId.Rifle);
Require(!CraftingService.TryCraft(inventory, rifleRecipe, CraftingStation.Workbench), "A rifle must not craft without its ingredients.");

inventory.Add(ItemId.ScrapMetal, 14);
inventory.Add(ItemId.IronIngot, 6);
inventory.Add(ItemId.Wood, 3);
inventory.Add(ItemId.MechanicalParts, 2);
Require(CraftingService.TryCraft(inventory, rifleRecipe, CraftingStation.Workbench), "A rifle should craft when all workbench ingredients are present.");
Require(inventory.Count(ItemId.Rifle) == 1 && inventory.Count(ItemId.ScrapMetal) == 0, "Gun crafting should consume its recipe ingredients.");
Require(!CraftingService.CanCraft(inventory, rifleRecipe, CraftingStation.Furnace), "A furnace must not substitute for a workbench.");

Console.WriteLine("Trackbound core checks passed: deterministic terrain, mountain relief, train dynamics, and atomic crafting.");