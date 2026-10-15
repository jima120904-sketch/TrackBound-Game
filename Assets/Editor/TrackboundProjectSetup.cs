#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class TrackboundProjectSetup
{
    private const string ScenePath = "Assets/Scenes/TrackboundPrototype.unity";

    static TrackboundProjectSetup()
    {
        EditorApplication.delayCall += EnsurePrototypeScene;
    }

    [MenuItem("Trackbound/Project/Prepare Prototype Scene")]
    private static void EnsurePrototypeScene()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");
        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Trackbound Runtime").AddComponent<TrackboundBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Created the Trackbound runtime-generated prototype scene.");
        }

        var scenes = EditorBuildSettings.scenes;
        var included = false;
        foreach (var scene in scenes)
        {
            if (scene.path == ScenePath)
            {
                included = true;
                break;
            }
        }
        if (!included)
        {
            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(updated, 0);
            updated[updated.Length - 1] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }

    [MenuItem("Trackbound/Diagnostics/HDRP and Ray Tracing")]
    private static void ReportGraphicsCapabilities()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        var isHdrp = pipeline != null && pipeline.GetType().Name.Contains("HDRenderPipelineAsset");
        var hdrpType = System.Type.GetType("UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset, Unity.RenderPipelines.HighDefinition.Runtime");
        Debug.Log($"HDRP package: {(hdrpType == null ? "not imported" : "imported")}; active pipeline: {(isHdrp ? "HDRP" : "not HDRP")}; graphics API: {SystemInfo.graphicsDeviceType}; ray-tracing device support: {SystemInfo.supportsRayTracing}. Device support alone does not enable HDRP ray-traced effects; configure the HDRP asset and camera Frame Settings on supported hardware.");
    }
}
#endif