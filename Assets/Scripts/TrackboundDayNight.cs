using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public sealed class TrackboundDayNight : MonoBehaviour
{
    private const float CycleSeconds = 48 * 60f;
    private Light _sun;
    private Fog _fog;
    private float _startTime;

    public float NormalizedTime => Mathf.Repeat((Time.time - _startTime) / CycleSeconds + 0.54f, 1f);
    public bool IsNight => NormalizedTime >= 30f / 48f;

    public void Initialize(Light sun, Fog fog)
    {
        _sun = sun;
        _fog = fog;
        _startTime = Time.time;
    }

    private void Update()
    {
        if (_sun == null)
        {
            return;
        }

        var phase = NormalizedTime;
        var dayFraction = phase / (30f / 48f);
        var daylight = phase < 30f / 48f ? Mathf.Sin(dayFraction * Mathf.PI) : 0.025f;
        var duskWarmth = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.46f, 0.62f, phase));
        var warmth = Mathf.Max(0, duskWarmth);
        var direction = phase * 360f;

        _sun.transform.rotation = Quaternion.Euler(12f + Mathf.Sin(direction * Mathf.Deg2Rad) * 55f, -31f, 0);
        _sun.color = Color.Lerp(new Color(0.76f, 0.84f, 1f), new Color(1f, 0.68f, 0.42f), warmth);
        var hdrp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        _sun.intensity = daylight * (hdrp ? 80000f : 1.25f);
        RenderSettings.ambientLight = Color.Lerp(new Color(0.035f, 0.05f, 0.075f), new Color(0.34f, 0.39f, 0.42f), daylight);
        RenderSettings.fogColor = Color.Lerp(new Color(0.025f, 0.035f, 0.07f), new Color(0.54f, 0.58f, 0.58f), daylight);
        if (_fog != null)
        {
            _fog.meanFreePath.value = Mathf.Lerp(110f, 1800f, daylight);
        }
    }
}