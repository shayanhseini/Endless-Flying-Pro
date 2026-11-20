// WebGL only: keeps the game smooth on weak PCs and phones. Strong machines keep the authored
// look (level 0). When frames stay slow, the rendering cost is lowered one step at a time:
//   1  render scale <= 0.7, TAA -> FXAA (TAA keeps history buffers and blurs at low resolution)
//   2  no post-processing, no real-time shadows
//   3  render scale 0.55
// Phones start at level 1. Steps only go down (no flicker between levels), like the
// DroneVision model's own full -> lite fallback.
// It creates itself after the first scene loads: nothing to add to a scene. In the Editor it
// does nothing, so the project's URP assets are never changed on disk.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WebGLAdaptiveQuality : MonoBehaviour
{
    private const float SlowFrameMs = 25f;   // slower than ~40 FPS on average counts as slow
    private const float SlowForSeconds = 3f; // that long slow -> one step down
    private const float WarmUpSeconds = 5f;  // ignore loading hitches and shader warm-up
    private const float SettleSeconds = 2f;  // after a step, let the new level settle
    private const int MaxLevel = 3;

    public static int Level { get; private set; }

    private UniversalRenderPipelineAsset urp;
    private float authoredScale = 1f;
    private float averageMs;
    private float slowFor;
    private float waitFor = WarmUpSeconds;
    private Camera appliedCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var go = new GameObject(nameof(WebGLAdaptiveQuality));
        DontDestroyOnLoad(go);
        go.AddComponent<WebGLAdaptiveQuality>();
#endif
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
            authoredScale = urp.renderScale;
        Level = 0;
        if (Application.isMobilePlatform)
            SetLevel(1, "phone");
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // the camera can be replaced (scene changes, camera transitions): give it the current level
        Camera cam = Camera.main;
        if (cam != null && cam != appliedCamera)
            ApplyCamera(cam);

        // a frame this long is a tab switch or a load, not the cost of rendering
        if (dt > 0.25f)
            return;

        float ms = dt * 1000f;
        averageMs = averageMs <= 0f ? ms : averageMs + (ms - averageMs) * 0.05f;

        if (waitFor > 0f)
        {
            waitFor -= dt;
            return;
        }
        if (Level >= MaxLevel)
            return;

        if (averageMs > SlowFrameMs)
        {
            slowFor += dt;
            if (slowFor >= SlowForSeconds)
                SetLevel(Level + 1, $"{averageMs:F0} ms per frame");
        }
        else
        {
            slowFor = 0f;
        }
    }

    private void SetLevel(int level, string reason)
    {
        Level = Mathf.Clamp(level, 0, MaxLevel);
        slowFor = 0f;
        waitFor = SettleSeconds;

        if (urp != null)
        {
            float scale = authoredScale;
            if (Level >= 1) scale = Mathf.Min(scale, 0.7f);
            if (Level >= 3) scale = Mathf.Min(scale, 0.55f);
            urp.renderScale = scale;
            if (Level >= 2)
                urp.shadowDistance = 0f;
        }

        appliedCamera = null;   // re-applied to the current camera on the next frame
        Debug.Log($"[AdaptiveQuality] level {Level} ({reason})");
    }

    private void ApplyCamera(Camera cam)
    {
        appliedCamera = cam;
        if (Level == 0)
            return;
        var data = cam.GetUniversalAdditionalCameraData();
        if (data == null)
            return;
        if (data.antialiasing == AntialiasingMode.TemporalAntiAliasing)
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        if (Level >= 2)
        {
            data.renderPostProcessing = false;
            data.renderShadows = false;
        }
    }
}
