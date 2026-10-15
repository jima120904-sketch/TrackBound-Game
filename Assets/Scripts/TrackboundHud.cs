using UnityEngine;
using UnityEngine.Rendering;

public sealed class TrackboundHud : MonoBehaviour
{
    private TrainDrive _train;
    private PlayerMotor _player;
    private GUIStyle _panel;
    private GUIStyle _hint;
    private GUIStyle _center;

    public void Initialize(TrainDrive train, PlayerMotor player)
    {
        _train = train;
        _player = player;
    }

    private void OnGUI()
    {
        if (_train == null || _player == null)
        {
            return;
        }

        EnsureStyles();
        var speed = _train.SpeedKilometersPerHour.ToString("00");
        var fuel = _train.FuelChunks.ToString("0");
        GUI.Box(new Rect(22, 22, 300, 128), GUIContent.none);
        GUI.Label(new Rect(38, 30, 250, 32), "TRACKBOUND  /  E1", _panel);
        GUI.Label(new Rect(38, 68, 250, 32), $"{speed} km/h     {fuel} coal", _panel);
        GUI.Label(new Rect(38, 105, 265, 32), $"{_train.DistanceTravelled / 1000:0.00} km     {_player.CoalChunks:0} carried", _panel);

        GUI.Label(new Rect(Screen.width / 2 - 14, Screen.height / 2 - 14, 28, 28), "+", _center);
        if (!string.IsNullOrEmpty(_player.TargetHint))
        {
            GUI.Box(new Rect(Screen.width / 2 - 140, Screen.height / 2 + 30, 280, 34), _player.TargetHint, _hint);
        }

        var message = string.IsNullOrEmpty(_player.Message) ? "" : _player.Message;
        if (!string.IsNullOrEmpty(message))
        {
            GUI.Box(new Rect(Screen.width / 2 - 180, Screen.height - 112, 360, 38), message, _hint);
        }

        var pipeline = GraphicsSettings.currentRenderPipeline;
        var hdrpActive = pipeline != null && pipeline.GetType().Name.Contains("HDRenderPipelineAsset");
        var rayTracingDevice = SystemInfo.supportsRayTracing;
        var rtText = hdrpActive && rayTracingDevice ? "RT device detected; HDRP effects need editor configuration" : "RT hardware/pipeline not verified; raster fallback";
        GUI.Label(new Rect(22, Screen.height - 52, 720, 26), rtText, _hint);
        GUI.Label(new Rect(Screen.width - 410, 26, 388, 114), "WASD move   Mouse look   Shift sprint   Space jump\nUp/Down: throttle   Left/Right: brake\nE: interact   Esc: release mouse", _hint);
    }

    private void EnsureStyles()
    {
        if (_panel != null)
        {
            return;
        }

        _panel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.88f, 0.91f, 0.84f) }
        };
        _hint = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        _center = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.84f, 0.57f) }
        };
    }
}