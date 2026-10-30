using UnityEngine;

/// <summary>
/// Adds authentic zero-gravity floating, micro-drifting, and slow axial tumbling
/// to scenic space props (satellites, space stations, probes) appearing along the route.
/// Purely visual/atmospheric with colliders automatically disabled.
/// </summary>
public class FloatingSpaceProp : MonoBehaviour
{
    [Header("Zero-G Tumbling")]
    [Tooltip("Angular speed in degrees per second.")]
    [SerializeField] private float tumbleSpeedMin = 0.8f;
    [SerializeField] private float tumbleSpeedMax = 3.5f;
    [SerializeField] private Vector3 customTumbleAxis = Vector3.zero;

    [Header("Zero-G Floating & Bobbing")]
    [SerializeField] private float floatAmpY = 1.2f;
    [SerializeField] private float floatSpeedY = 0.7f;
    [SerializeField] private float floatAmpX = 0.8f;
    [SerializeField] private float floatSpeedX = 0.5f;

    [Header("Zero-G Drift")]
    [Tooltip("Subtle relative drift velocity independent of world movement.")]
    [SerializeField] private Vector2 driftRangeX = new Vector2(-0.4f, 0.4f);
    [SerializeField] private Vector2 driftRangeY = new Vector2(-0.3f, 0.3f);

    [Header("Beacon Light (Optional)")]
    [SerializeField] private Light beaconLight;
    [SerializeField] private float strobeInterval = 2.0f;
    [SerializeField] private float strobeFlashDuration = 0.12f;
    [SerializeField] private Color beaconColor = new Color(0.4f, 0.85f, 1f, 1f);

    [Header("Lifecycle")]
    [SerializeField] private float despawnDistanceBehindPlayer = 60f;

    // Runtime state
    private WorldMovementManager worldMovement;
    private Transform playerTransform;
    private Vector3 tumbleAxis;
    private float tumbleSpeed;
    private Vector3 driftVelocity;
    private Vector3 baseAnchorPosition;
    private float phaseOffset;
    private float strobeTimer;
    private float defaultLightIntensity = 1f;
    private bool isInitialized;

    public void Initialize(Transform player, WorldMovementManager movement)
    {
        playerTransform = player;
        worldMovement = movement;
        isInitialized = true;
    }

    private void Awake()
    {
        // 1. Permanently remove any rogue Camera components so props never hijack the player viewport
        Camera[] cams = GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cams.Length; i++)
        {
            cams[i].enabled = false;
            Destroy(cams[i]);
        }

        // 2. Permanently remove any AudioListener components
        AudioListener[] listeners = GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
        {
            listeners[i].enabled = false;
            Destroy(listeners[i]);
        }

        // 3. Disable and destroy all Colliders and Rigidbodies so props never obstruct gameplay
        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < allColliders.Length; i++)
        {
            allColliders[i].enabled = false;
            Destroy(allColliders[i]);
        }
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++)
        {
            Destroy(rbs[i]);
        }

        // 4. Disable any Animator/Animation components so imported animations do not override tumbling
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].enabled = false;
        }
        Animation[] anims = GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < anims.Length; i++)
        {
            anims[i].enabled = false;
        }

        // 5. Enforce fully Opaque geometry queue (2000) and depth write on all materials
        // so props are never visible through planet bodies
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] != null)
                {
                    mats[m].renderQueue = 2000;
                    if (mats[m].HasProperty("_Surface"))
                        mats[m].SetFloat("_Surface", 0f);
                    if (mats[m].HasProperty("_ZWrite"))
                        mats[m].SetFloat("_ZWrite", 1f);
                    if (mats[m].HasProperty("_ZTest"))
                        mats[m].SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                    mats[m].SetOverrideTag("RenderType", "Opaque");
                }
            }
        }

        // Randomize tumbling axis & speed
        if (customTumbleAxis != Vector3.zero)
        {
            tumbleAxis = customTumbleAxis.normalized;
        }
        else
        {
            tumbleAxis = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
            if (tumbleAxis == Vector3.zero) tumbleAxis = Vector3.up;
        }

        tumbleSpeed = Random.Range(tumbleSpeedMin, tumbleSpeedMax);
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        // Subtle zero-g drift
        driftVelocity = new Vector3(
            Random.Range(driftRangeX.x, driftRangeX.y),
            Random.Range(driftRangeY.x, driftRangeY.y),
            0f
        );

        baseAnchorPosition = transform.position;

        // Setup beacon light if present or find child light
        if (beaconLight == null)
            beaconLight = GetComponentInChildren<Light>();

        if (beaconLight != null)
        {
            defaultLightIntensity = beaconLight.intensity > 0f ? beaconLight.intensity : 2.5f;
            beaconLight.color = beaconColor;
            beaconLight.enabled = false;
        }
    }

    private void Start()
    {
        if (!isInitialized)
        {
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    playerTransform = playerObj.transform;
            }

            if (worldMovement == null)
            {
                worldMovement = Object.FindFirstObjectByType<WorldMovementManager>();
            }

            baseAnchorPosition = transform.position;
            isInitialized = true;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. World Movement
        if (worldMovement != null && worldMovement.IsMovementEnabled)
        {
            float moveStep = worldMovement.WorldSpeed * dt;
            baseAnchorPosition += Vector3.back * moveStep;
        }

        // 2. Zero-G Drift
        baseAnchorPosition += driftVelocity * dt;

        // 3. Smooth Harmonic Bobbing / Floating
        float time = Time.time + phaseOffset;
        float bobY = Mathf.Sin(time * floatSpeedY) * floatAmpY;
        float bobX = Mathf.Cos(time * floatSpeedX) * floatAmpX;

        transform.position = baseAnchorPosition + new Vector3(bobX, bobY, 0f);

        // 4. Zero-G Slow Majestic Axial Tumbling
        transform.Rotate(tumbleAxis, tumbleSpeed * dt, Space.World);

        // 5. Strobe / Navigation Beacon
        UpdateBeaconLight(dt);

        // 6. Check Despawn behind player
        CheckDespawn();
    }

    private void UpdateBeaconLight(float dt)
    {
        if (beaconLight == null) return;

        strobeTimer += dt;
        if (strobeTimer >= strobeInterval)
        {
            strobeTimer -= strobeInterval;
        }

        // Quick double-flash pattern typical for aerospace navigation lights
        float flash1Start = 0f;
        float flash1End = strobeFlashDuration;
        float flash2Start = strobeFlashDuration * 1.8f;
        float flash2End = flash2Start + strobeFlashDuration;

        bool isFlashing = (strobeTimer >= flash1Start && strobeTimer <= flash1End) ||
                          (strobeTimer >= flash2Start && strobeTimer <= flash2End);

        beaconLight.enabled = isFlashing;
        if (isFlashing)
        {
            beaconLight.intensity = defaultLightIntensity;
        }
    }

    private void CheckDespawn()
    {
        if (playerTransform == null) return;

        if (transform.position.z < playerTransform.position.z - despawnDistanceBehindPlayer)
        {
            Destroy(gameObject);
        }
    }
}
