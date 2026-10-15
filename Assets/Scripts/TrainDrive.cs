using Trackbound.Core;
using UnityEngine;

public sealed class TrainDrive : MonoBehaviour
{
    private TrainSimulation _simulation;
    private Transform _throttleLever;
    private Transform _brakeLever;
    private Material _paint;
    private Material _steel;
    private Material _brass;
    private Material _timber;
    private float _throttleInput;
    private float _brakeInput;

    public float SpeedMetersPerSecond => (float)_simulation.SpeedMetersPerSecond;
    public float SpeedKilometersPerHour => SpeedMetersPerSecond * 3.6f;
    public float FuelChunks => (float)_simulation.FuelChunks;
    public float Throttle => _throttleInput;
    public float Brake => _brakeInput;
    public double DistanceTravelled { get; private set; }

    public void Initialize(Transform trainRoot, Material paint, Material steel, Material brass, Material timber)
    {
        _simulation = new TrainSimulation(90);
        _paint = paint;
        _steel = steel;
        _brass = brass;
        _timber = timber;
        BuildLocomotive(trainRoot);
        BuildCarriages(trainRoot);
    }

    private void Update()
    {
        if (_simulation == null)
        {
            return;
        }

        var throttleDirection = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
        var brakeDirection = (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f);
        _throttleInput = Mathf.Clamp01(_throttleInput + throttleDirection * Time.deltaTime * 0.35f);
        _brakeInput = Mathf.Clamp01(_brakeInput + brakeDirection * Time.deltaTime * 0.8f);
        if (throttleDirection == 0)
        {
            _throttleInput = Mathf.MoveTowards(_throttleInput, 0, Time.deltaTime * 0.07f);
        }
        if (brakeDirection == 0)
        {
            _brakeInput = Mathf.MoveTowards(_brakeInput, 0, Time.deltaTime * 0.16f);
        }

        _simulation.SetControls(_throttleInput, _brakeInput);
        _simulation.Tick(Time.deltaTime);
        var distance = SpeedMetersPerSecond * Time.deltaTime;
        transform.position += Vector3.forward * distance;
        DistanceTravelled += distance;

        if (_throttleLever != null)
        {
            _throttleLever.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-35, 35, _throttleInput));
            _brakeLever.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-30, 30, _brakeInput));
        }
    }

    public void FeedFuel(PlayerMotor player)
    {
        if (player.TryUseCoal())
        {
            _simulation.AddFuel(1);
            player.SetMessage("Coal dropped into the firebox");
        }
        else
        {
            player.SetMessage("No coal in your hands");
        }
    }

    private void BuildLocomotive(Transform root)
    {
        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Underframe", root, new Vector3(0, 1.25f, 0), new Vector3(3.3f, 0.75f, 14.2f), _steel, true);
        TrackboundGeometry.Primitive(PrimitiveType.Cylinder, "Boiler shell", root, new Vector3(0, 2.55f, 2.8f), new Vector3(2.0f, 4.3f, 2.0f), _paint).transform.localRotation = Quaternion.Euler(90, 0, 0);
        TrackboundGeometry.Primitive(PrimitiveType.Cylinder, "Smokestack", root, new Vector3(0, 3.85f, 4.15f), new Vector3(0.7f, 1.35f, 0.7f), _steel);
        TrackboundGeometry.Primitive(PrimitiveType.Cylinder, "Steam dome", root, new Vector3(0, 3.67f, 0.3f), new Vector3(0.78f, 0.9f, 0.78f), _brass);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Front buffer beam", root, new Vector3(0, 1.1f, 7.15f), new Vector3(3.4f, 0.55f, 0.38f), _paint);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Rear buffer beam", root, new Vector3(0, 1.1f, -7.15f), new Vector3(3.4f, 0.55f, 0.38f), _paint);
        TrackboundGeometry.Primitive(PrimitiveType.Cylinder, "Headlamp", root, new Vector3(0, 2.2f, 7.28f), new Vector3(0.65f, 0.55f, 0.65f), _brass);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Left running board", root, new Vector3(-1.7f, 1.65f, -0.5f), new Vector3(0.25f, 0.16f, 10.5f), _steel);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Right running board", root, new Vector3(1.7f, 1.65f, -0.5f), new Vector3(0.25f, 0.16f, 10.5f), _steel);

        for (var axle = 0; axle < 4; axle++)
        {
            var z = 4.6f - axle * 3.05f;
            TrackboundGeometry.Primitive(PrimitiveType.Cube, $"Bogie {axle + 1}", root, new Vector3(0, 0.95f, z), new Vector3(3.0f, 0.34f, 1.6f), _steel);
            for (var side = -1; side <= 1; side += 2)
            {
                var wheel = TrackboundGeometry.Primitive(PrimitiveType.Cylinder, $"Driving wheel {axle + 1}", root, new Vector3(side * 1.46f, 0.68f, z), new Vector3(1.18f, 0.48f, 1.18f), _steel);
                wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
        }

        BuildCab(root);
        BuildCoupler(root, new Vector3(0, 0.9f, 8.0f));
        BuildCoupler(root, new Vector3(0, 0.9f, -8.0f));
    }

    private void BuildCab(Transform root)
    {
        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Cab floor", root, new Vector3(0, 1.78f, -5.25f), new Vector3(3.15f, 0.22f, 5.2f), _timber, true);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Cab roof", root, new Vector3(0, 4.25f, -5.25f), new Vector3(3.45f, 0.24f, 5.35f), _paint, true);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Cab rear wall", root, new Vector3(0, 3.0f, -7.8f), new Vector3(3.2f, 2.5f, 0.18f), _paint, true);

        foreach (var side in new[] { -1, 1 })
        {
            TrackboundGeometry.Primitive(PrimitiveType.Cube, "Cab lower side panel", root, new Vector3(side * 1.52f, 2.34f, -5.2f), new Vector3(0.16f, 1.0f, 5.0f), _paint, true);
            TrackboundGeometry.Primitive(PrimitiveType.Cube, "Cab window rail", root, new Vector3(side * 1.52f, 3.8f, -5.2f), new Vector3(0.16f, 0.16f, 5.0f), _steel);
            foreach (var z in new[] { -7.4f, -6.2f, -4.5f, -3.0f })
            {
                TrackboundGeometry.Primitive(PrimitiveType.Cube, "Window mullion", root, new Vector3(side * 1.52f, 3.3f, z), new Vector3(0.14f, 0.95f, 0.12f), _steel);
            }
        }

        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Control desk", root, new Vector3(0, 2.26f, -3.25f), new Vector3(2.55f, 0.62f, 0.52f), _timber, true);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Instrument panel", root, new Vector3(0, 2.78f, -3.48f), new Vector3(2.4f, 0.42f, 0.16f), _steel);

        var throttle = TrackboundGeometry.Primitive(PrimitiveType.Cylinder, "Throttle lever", root, new Vector3(-0.7f, 2.75f, -3.0f), new Vector3(0.11f, 0.8f, 0.11f), _brass);
        _throttleLever = throttle.transform;
        var brake = TrackboundGeometry.Primitive(PrimitiveType.Cylinder, "Brake lever", root, new Vector3(0.7f, 2.75f, -3.0f), new Vector3(0.11f, 0.8f, 0.11f), _brass);
        _brakeLever = brake.transform;

        var hinge = new GameObject("Firebox door hinge").transform;
        hinge.SetParent(root, false);
        hinge.localPosition = new Vector3(-0.3f, 2.34f, -4.05f);
        var door = TrackboundGeometry.Primitive(PrimitiveType.Cube, "Firebox door", hinge, new Vector3(0, 0.25f, 0), new Vector3(0.66f, 0.62f, 0.12f), _steel, true);
        var interactable = door.AddComponent<TrainInteractable>();
        interactable.Initialize(this, hinge);

        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Coal bin", root, new Vector3(-0.92f, 2.0f, -6.85f), new Vector3(0.78f, 0.52f, 1.25f), _steel, true);
        for (var i = 0; i < 7; i++)
        {
            var chunk = TrackboundGeometry.Primitive(PrimitiveType.Sphere, "Coal chunk", root,
                new Vector3(-1.08f + (i % 3) * 0.18f, 2.32f + (i % 2) * 0.1f, -7.22f + (i / 3) * 0.22f),
                new Vector3(0.23f, 0.16f, 0.2f), _steel, true);
            chunk.AddComponent<CoalPickup>();
        }
    }

    private void BuildCarriages(Transform root)
    {
        BuildCarriage(root, "Armored cargo car", -19.0f, new Color(0.27f, 0.25f, 0.2f), true);
        BuildCarriage(root, "Workshop car", -33.0f, new Color(0.18f, 0.26f, 0.24f), false);
    }

    private void BuildCarriage(Transform root, string name, float z, Color color, bool cargo)
    {
        var carPaint = TrackboundGeometry.Material(name + " paint", color, 0.38f, 0.34f);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, name + " underframe", root, new Vector3(0, 1.28f, z), new Vector3(3.15f, 0.7f, 12.0f), _steel, true);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, name + " body", root, new Vector3(0, 2.95f, z), new Vector3(2.9f, 2.9f, 10.8f), carPaint, true);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, name + " roof", root, new Vector3(0, 4.48f, z), new Vector3(3.0f, 0.22f, 11.0f), _steel);
        TrackboundGeometry.Primitive(PrimitiveType.Cube, name + " sliding door", root, new Vector3(0, 2.88f, z + 5.48f), new Vector3(1.35f, 2.4f, 0.08f), _steel);

        for (var axle = -1; axle <= 1; axle += 2)
        {
            foreach (var side in new[] { -1, 1 })
            {
                var wheel = TrackboundGeometry.Primitive(PrimitiveType.Cylinder, name + " wheel", root,
                    new Vector3(side * 1.42f, 0.68f, z + axle * 3.7f), new Vector3(1.12f, 0.42f, 1.12f), _steel);
                wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
        }

        if (!cargo)
        {
            TrackboundGeometry.Primitive(PrimitiveType.Cube, "Workshop side window", root, new Vector3(-1.49f, 3.3f, z), new Vector3(0.08f, 0.76f, 3.2f), _brass);
        }
    }

    private void BuildCoupler(Transform root, Vector3 localPosition)
    {
        TrackboundGeometry.Primitive(PrimitiveType.Cube, "Coupler shank", root, localPosition, new Vector3(0.55f, 0.36f, 0.95f), _steel);
        TrackboundGeometry.Primitive(PrimitiveType.Sphere, "Coupler head", root, localPosition + new Vector3(0, 0, Mathf.Sign(localPosition.z) * 0.55f), new Vector3(0.7f, 0.48f, 0.48f), _steel);
    }
}