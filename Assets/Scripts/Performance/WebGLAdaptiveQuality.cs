// WebGL only: keeps the game smooth on weak PCs and phones while preserving authored visual quality.
// When frames stay slow, the rendering cost is lowered gracefully:
//   1  render scale <= 0.85, TAA -> FXAA
//   2  shadow distance lowered to 20m, render scale <= 0.80 (Post-processing stays active to maintain sci-fi look)
//   3  real-time shadows disabled, render scale <= 0.75
// Phones start at level 1.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WebGLAdaptiveQuality : MonoBehaviour
{
    private const float SlowFrameMs = 28f;   // slower than ~35 FPS on average counts as slow
    private const float SlowForSeconds = 3.5f; // that long slow -> one step down
    private const float WarmUpSeconds = 5f;  // ignore loading hitches and shader warm-up
    private const float SettleSeconds = 2f;  // after a step, let the new level settle
    private const int MaxLevel = 3;

    public static int Level { get; private set; }

    private UniversalRenderPipelineAsset urp;
    private float authoredScale = 1f;
    private float authoredShadowDistance = 40f;
    private float averageMs;
    private float slowFor;
    private float waitFor = WarmUpSeconds;
    private Camera appliedCamera;
    private float camCheckTimer;

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
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
        urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            authoredScale = urp.renderScale;
            authoredShadowDistance = urp.shadowDistance;
        }
        Level = 0;
        if (Application.isMobilePlatform)
            SetLevel(1, "phone");
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        camCheckTimer += dt;
        if (camCheckTimer > 0.5f || appliedCamera == null)
        {
            camCheckTimer = 0f;
            Camera cam = Camera.main;
            if (cam != null && cam != appliedCamera)
                ApplyCamera(cam);
        }

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
            if (Level == 1) scale = Mathf.Min(scale, 0.85f);
            else if (Level == 2) scale = Mathf.Min(scale, 0.80f);
            else if (Level >= 3) scale = Mathf.Min(scale, 0.75f);

            urp.renderScale = scale;

            if (Level == 1)
                urp.shadowDistance = Mathf.Min(authoredShadowDistance, 35f);
            else if (Level == 2)
                urp.shadowDistance = 20f;
            else if (Level >= 3)
                urp.shadowDistance = 0f;
        }

        appliedCamera = null;   // re-applied to the current camera
        Debug.Log($"[AdaptiveQuality] level {Level} ({reason})");
    }

    private void ApplyCamera(Camera cam)
    {
        appliedCamera = cam;
        if (cam == null)
            return;

        var data = cam.GetUniversalAdditionalCameraData();
        if (data == null)
            return;

        if (data.antialiasing == AntialiasingMode.TemporalAntiAliasing)
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

        // Preserve Post-Processing across all quality levels to retain visual polish (bloom, tone mapping)
        data.renderPostProcessing = true;

        if (Level >= 3)
        {
            data.renderShadows = false;
        }
    }
}
