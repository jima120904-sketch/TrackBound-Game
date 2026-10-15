using Trackbound.Core;

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

Console.WriteLine("Trackbound core checks passed: deterministic terrain, fuel use, traction, and braking.");