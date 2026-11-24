using UnityEngine;
using UnityEngine.Events;

public class WorldChunk : MonoBehaviour
{
    [SerializeField]
    private float length = 20f;

    [SerializeField]
    private ChunkContent content;


    [Header("Environment Variants")]
    [SerializeField]
    private GameObject[] desertVariants;

    [SerializeField]
    private GameObject[] dirtVariants;


    [Header("Final Ground")]
    [SerializeField]
    private GameObject finalGround;

    [SerializeField]
    private Transform finalTarget;

    private bool finalGroundOptimized;

    private void Awake()
    {
        OptimizeFinalGround();
    }

    private void OptimizeFinalGround()
    {
        if (finalGroundOptimized || finalGround == null)
            return;

        try
        {
            // Do NOT touch SetActive on any child object!
            // Retain the exact original prefab active/inactive hierarchy arrangement.

            // Disable shadow casting and expensive shader features on background props (including while finalGround is inactive).
            // This cuts massive shadow-caster draw calls in WebGL while keeping the visual fidelity 100% intact.
            Renderer[] renderers = finalGround.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;

                string n = r.gameObject.name.ToLowerInvariant();
                bool isGroundPlane = n.Contains("ground") || n.Contains("plane") || n.Contains("runway") || n.Contains("terrain");

                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // Only the flat ground/runway needs to receive shadows; background props skip shadow map sampling
                r.receiveShadows = isGroundPlane;
                r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }

            // Disable static colliders on props in finalGround that the player plane never hits.
            // When chunk.Move() moves finalGround every frame, active static colliders force PhysX BVH rebuild!
            Collider[] colliders = finalGround.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                if (c == null) continue;
                string n = c.gameObject.name.ToLowerInvariant();
                // Keep triggers (like FinalGroundTrigger) and Target collider; disable background prop colliders
                if (!c.isTrigger && !n.Contains("target") && !n.Contains("missile"))
                {
                    c.enabled = false;
                }
            }

            finalGroundOptimized = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[WorldChunk] OptimizeFinalGround warning: {ex.Message}");
        }
    }

    public void WarmupFinalGround()
    {
        if (finalGround == null)
            return;

        OptimizeFinalGround();

        bool wasActive = finalGround.activeSelf;
        AudioSource[] audioSources = finalGround.GetComponentsInChildren<AudioSource>(true);
        bool[] prevMutes = new bool[audioSources.Length];
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                prevMutes[i] = audioSources[i].mute;
                audioSources[i].mute = true;
            }
        }

        RenderTexture rt = null;
        GameObject camObj = null;
        try
        {
            if (!wasActive)
            {
                finalGround.SetActive(true);
            }

            Renderer[] activeRenderers = finalGround.GetComponentsInChildren<Renderer>(false);
            if (activeRenderers.Length > 0)
            {
                Bounds bounds = activeRenderers[0].bounds;
                for (int i = 1; i < activeRenderers.Length; i++)
                {
                    if (activeRenderers[i] != null)
                    {
                        bounds.Encapsulate(activeRenderers[i].bounds);
                    }
                }

                rt = new RenderTexture(16, 16, 16, RenderTextureFormat.Default);
                rt.Create();

                camObj = new GameObject("FinalGroundWarmupCam");
                camObj.hideFlags = HideFlags.HideAndDontSave;
                Camera warmupCam = camObj.AddComponent<Camera>();
                warmupCam.enabled = false;
                warmupCam.targetTexture = rt;
                warmupCam.clearFlags = CameraClearFlags.SolidColor;
                warmupCam.backgroundColor = Color.black;
                warmupCam.cullingMask = ~0;
                warmupCam.farClipPlane = 1200f;
                warmupCam.allowHDR = false;
                warmupCam.allowMSAA = false;

                // Pass 1: Far view to warm LOD1 (Mirage Impostor shader + texture atlases)
                camObj.transform.position = bounds.center + new Vector3(0f, 30f, -450f);
                camObj.transform.LookAt(bounds.center);
                warmupCam.Render();

                // Pass 2: Close view to warm LOD0 (3D meshes + ground materials)
                camObj.transform.position = bounds.center + new Vector3(0f, 12f, -35f);
                camObj.transform.LookAt(bounds.center);
                warmupCam.Render();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[WorldChunk] WarmupFinalGround warning: {ex.Message}");
        }
        finally
        {
            if (camObj != null)
            {
                Destroy(camObj);
            }
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
            }

            if (!wasActive && finalGround != null)
            {
                finalGround.SetActive(false);
            }

            for (int i = 0; i < audioSources.Length; i++)
            {
                if (audioSources[i] != null)
                {
                    audioSources[i].Stop();
                    audioSources[i].mute = prevMutes[i];
                }
            }
        }
    }


    [Header("Event")]
    public UnityEvent onResetContent;


    public float Length =>
        length;


    public Transform FinalTarget =>
        finalTarget;


    public void ResetContent(
        DifficultyManager difficultyManager,
        GroundType groundType)
    {
        onResetContent?.Invoke();

        DisableFinalGround();

        RandomizeEnvironment(
            groundType,
            difficultyManager
        );


        if (content != null && difficultyManager != null)
        {
            content.ApplyDifficulty(
                difficultyManager.CoinCount,
                difficultyManager.ObstacleCount,
                difficultyManager.GetShieldCountForChunk(),
                difficultyManager.HasSpeedPowerUp
            );
        }
    }


    public void ActivateFinalGround()
    {
        // تمام Content خاموش
        if (content != null)
        {
            content.DisableContent();
        }


        // Groundهای معمولی خاموش
        DisableAllEnvironments();


        // Final Ground روشن
        if (finalGround != null)
        {
            OptimizeFinalGround();
            finalGround.SetActive(true);
            OptimizeFinalGround();
        }
    }


    private void DisableFinalGround()
    {
        if (finalGround != null)
        {
            finalGround.SetActive(false);
        }
    }


    private void RandomizeEnvironment(
        GroundType groundType,
        DifficultyManager difficultyManager = null)
    {
        DisableAllEnvironments();


        GameObject[] variants =
            GetVariants(
                groundType
            );


        if (variants == null ||
            variants.Length == 0)
            return;


        int randomIndex =
            Random.Range(
                0,
                variants.Length
            );


        GameObject selectedEnvironment =
            variants[randomIndex];


        if (selectedEnvironment == null)
            return;


        selectedEnvironment.SetActive(true);


        GroundDefenceHolder holder =
            selectedEnvironment.GetComponent<
                GroundDefenceHolder
            >();


        if (holder != null)
        {
            holder.ResetMissile(difficultyManager);
        }
    }


    private GameObject[] GetVariants(
        GroundType groundType)
    {
        switch (groundType)
        {
            case GroundType.Desert:
                return desertVariants;

            case GroundType.Dirt:
                return dirtVariants;

            default:
                return null;
        }
    }


    private void DisableAllEnvironments()
    {
        DisableVariants(
            desertVariants
        );

        DisableVariants(
            dirtVariants
        );
    }


    private void DisableVariants(
        GameObject[] variants)
    {
        if (variants == null)
            return;


        foreach (GameObject environment
                 in variants)
        {
            if (environment != null)
            {
                environment.SetActive(false);
            }
        }
    }


    public void Move(
        Vector3 movement)
    {
        transform.position += movement;
    }


    public void MoveTo(
        Vector3 position)
    {
        transform.position = position;
    }
}