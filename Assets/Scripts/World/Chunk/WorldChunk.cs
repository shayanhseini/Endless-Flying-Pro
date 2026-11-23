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

    private void Awake()
    {
        OptimizeFinalGround();
    }

    private void OptimizeFinalGround()
    {
        if (finalGround == null)
            return;

        try
        {
            // 1. Disable shadow casting on ALL objects in finalGround (background props, AWACS, humvees, buildings, towers).
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
            }

            // 2. Disable static colliders in finalGround that the player plane never hits.
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

            // 3. Deactivate far lateral props that are completely outside camera FOV (X < -32 or X > 50)
            Transform[] transforms = finalGround.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t == null || t == finalGround.transform) continue;
                if (t.parent == finalGround.transform || (t.parent != null && t.parent.name.Contains("Enve_Objects")))
                {
                    if (t.localPosition.x < -32f || t.localPosition.x > 50f)
                    {
                        t.gameObject.SetActive(false);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[WorldChunk] OptimizeFinalGround warning: {ex.Message}");
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