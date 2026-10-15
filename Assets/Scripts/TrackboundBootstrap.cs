using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public sealed class TrackboundBootstrap : MonoBehaviour
{
    public static TrackboundBootstrap Instance { get; private set; }

    public TrainDrive Train { get; private set; }
    public PlayerMotor Player { get; private set; }
    public Light Sun { get; private set; }
    public Material TerrainMaterial { get; private set; }
    public Material TrunkMaterial { get; private set; }
    public Material FoliageMaterial { get; private set; }
    private Fog _hdrpFog;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Application.targetFrameRate = 90;
        CreateMaterials();
        CreateLighting();
        CreateRail();
        CreateTrain();
        CreatePlayer();
        CreateWorldSystems();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrap()
    {
        if (FindFirstObjectByType<TrackboundBootstrap>() == null)
        {
            new GameObject("Trackbound Runtime").AddComponent<TrackboundBootstrap>();
        }
    }

    private void CreateMaterials()
    {
        TerrainMaterial = TrackboundGeometry.Material("Highland Earth", new Color(0.20f, 0.25f, 0.18f), 0, 0.18f);
        TrunkMaterial = TrackboundGeometry.Material("Weathered Timber", new Color(0.21f, 0.14f, 0.09f), 0, 0.25f);
        FoliageMaterial = TrackboundGeometry.Material("Pine Needles", new Color(0.12f, 0.22f, 0.17f), 0, 0.3f);
    }

    private void CreateLighting()
    {
        var sunObject = new GameObject("Trackbound Sun");
        Sun = sunObject.AddComponent<Light>();
        Sun.type = LightType.Directional;
        Sun.color = new Color(1.0f, 0.79f, 0.58f);
        Sun.intensity = GraphicsSettings.currentRenderPipeline != null ? 80000f : 1.25f;
        Sun.shadows = LightShadows.Soft;
        Sun.shadowStrength = 0.82f;
        sunObject.transform.rotation = Quaternion.Euler(24f, -31f, 0f);
        RenderSettings.sun = Sun;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.34f, 0.43f, 0.52f);
        RenderSettings.ambientEquatorColor = new Color(0.24f, 0.27f, 0.24f);
        RenderSettings.ambientGroundColor = new Color(0.11f, 0.10f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.52f, 0.59f, 0.62f);
        RenderSettings.fogDensity = 0.00018f;

        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader) { name = "Trackbound Procedural Sky" };
            RenderSettings.skybox = sky;
        }

        if (GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipelineAsset"))
        {
            CreateHdrpAtmosphere();
        }
    }

    private void CreateRail()
    {
        var railMaterial = TrackboundGeometry.Material("Rail Steel", new Color(0.24f, 0.27f, 0.28f), 0.78f, 0.62f);
        var sleeperMaterial = TrackboundGeometry.Material("Creosote Sleeper", new Color(0.17f, 0.13f, 0.10f), 0.05f, 0.2f);
        var railSystem = gameObject.AddComponent<TrackStreamer>();
        railSystem.Initialize(railMaterial, sleeperMaterial);
    }

    private void CreateTrain()
    {
        var trainRoot = new GameObject("Trackbound Train");
        Train = trainRoot.AddComponent<TrainDrive>();
        Train.Initialize(
            trainRoot.transform,
            TrackboundGeometry.Material("Locomotive Green Enamel", new Color(0.14f, 0.24f, 0.21f), 0.48f, 0.42f),
            TrackboundGeometry.Material("Graphite Steel", new Color(0.16f, 0.18f, 0.18f), 0.72f, 0.45f),
            TrackboundGeometry.Material("Signal Brass", new Color(0.55f, 0.37f, 0.13f), 0.75f, 0.7f),
            TrackboundGeometry.Material("Warm Cab Timber", new Color(0.28f, 0.19f, 0.12f), 0.05f, 0.24f));
    }

    private void CreatePlayer()
    {
        var playerObject = new GameObject("Player");
        playerObject.transform.SetParent(Train.transform, false);
        playerObject.transform.localPosition = new Vector3(0, 1.93f, -6.2f);
        Player = playerObject.AddComponent<PlayerMotor>();
        Player.Initialize(Train);

        var hud = gameObject.AddComponent<TrackboundHud>();
        hud.Initialize(Train, Player);
    }

    private void CreateWorldSystems()
    {
        var terrain = gameObject.AddComponent<TerrainStreamer>();
        terrain.Initialize(Train.transform, TerrainMaterial, TrunkMaterial, FoliageMaterial, 731);

        var cycle = gameObject.AddComponent<TrackboundDayNight>();
        cycle.Initialize(Sun, _hdrpFog);
    }

    private void CreateHdrpAtmosphere()
    {
        var volumeObject = new GameObject("Global HDRP Atmosphere");
        var volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "Trackbound Sky and Fog";
        volume.sharedProfile = profile;

        var environment = profile.Add<VisualEnvironment>(true);
        environment.skyType.value = (int)SkyType.PhysicallyBased;
        profile.Add<PhysicallyBasedSky>(true);

        _hdrpFog = profile.Add<Fog>(true);
        _hdrpFog.meanFreePath.value = 850f;
        _hdrpFog.baseHeight.value = 0f;
        _hdrpFog.maximumHeight.value = 3200f;
        _hdrpFog.albedo.value = new Color(0.75f, 0.78f, 0.76f);
        _hdrpFog.anisotropy.value = 0.35f;
        _hdrpFog.enableVolumetricFog.value = true;
    }
}