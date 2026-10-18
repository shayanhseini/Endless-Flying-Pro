using UnityEngine;
using System.Linq;


public class Obstacle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject box;
    [SerializeField] private GameObject[] asteroidVariants;

    [Header("Asteroid Rotation")]
    [SerializeField] private float minRotationSpeed = 20f;
    [SerializeField] private float maxRotationSpeed = 55f;

    [Header("Random Size")]
    [SerializeField] private float minScale = 3f;
    [SerializeField] private float maxScale = 4f;

    private Collider[] activeColliders = System.Array.Empty<Collider>();
    private Transform activeVisual;
    private Vector3 rotationAxis;
    private float rotationSpeed;
    private bool hitProcessed;
    private static int nextVariantIndex = -1;
    
    private void Awake()
    {
        CacheChildVariantsIfNeeded();
        InstallCollisionRelays();
    }
    
    private void OnEnable()
    {
        hitProcessed = false;
        SelectRandomVariant();

        float randomScale = Random.Range(minScale, maxScale);
        transform.localScale = Vector3.one * randomScale;

        
        SetActiveVariantColliders(true);

        rotationAxis = Random.onUnitSphere;
        rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);

        if (activeVisual != null)
            activeVisual.localRotation = Random.rotation;
    }

    private void Update()
    {
        if (activeVisual == null)
            return;

        activeVisual.Rotate(
            rotationAxis,
            rotationSpeed * Time.deltaTime,
            Space.Self
        );
    }

    private void SelectRandomVariant()
    {
        activeVisual = null;
        activeColliders = System.Array.Empty<Collider>();

        CacheChildVariantsIfNeeded();

        // Pooled obstacles can retain collider state from their previous use.
        // Disable every variant collider before choosing the next visual.
        foreach (GameObject variant in asteroidVariants)
        {
            if (variant == null)
                continue;

            foreach (Collider variantCollider in
                     variant.GetComponentsInChildren<Collider>(true))
            {
                variantCollider.enabled = false;
            }
        }

        if (asteroidVariants == null || asteroidVariants.Length == 0)
        {
            if (box != null)
            {
                box.SetActive(true);
                activeVisual = box.transform;
                activeColliders = box.GetComponentsInChildren<Collider>(true);
            }

            return;
        }

        foreach (GameObject variant in asteroidVariants)
        {
            if (variant != null)
                variant.SetActive(false);
        }

        if (nextVariantIndex < 0)
            nextVariantIndex = Random.Range(0, asteroidVariants.Length);

        int selectedIndex = nextVariantIndex % asteroidVariants.Length;
        nextVariantIndex = (nextVariantIndex + 1) % asteroidVariants.Length;
        GameObject selected = asteroidVariants[selectedIndex];
        if (selected == null)
            return;

        selected.SetActive(true);
        box = selected;
        activeVisual = selected.transform;
        activeColliders = selected.GetComponentsInChildren<Collider>(true);
    }

    private void CacheChildVariantsIfNeeded()
    {
        if (asteroidVariants != null && asteroidVariants.Length > 0)
            return;

        asteroidVariants = System.Array.FindAll(
            GetComponentsInChildren<Transform>(true),
            child => child != transform && child.name.StartsWith("AsteroidVariant_"))
            .Select(child => child.gameObject)
            .ToArray();
    }

    private void InstallCollisionRelays()
    {
        foreach (GameObject variant in asteroidVariants)
        {
            if (variant == null)
                continue;

            Collider[] colliders = variant.GetComponentsInChildren<Collider>(true);
            foreach (Collider variantCollider in colliders)
            {
                ObstacleCollisionRelay relay =
                    variantCollider.GetComponent<ObstacleCollisionRelay>();

                if (relay == null)
                    relay = variantCollider.gameObject.AddComponent<ObstacleCollisionRelay>();

                relay.Initialize(this);
            }
        }
    }

    private void SetActiveVariantColliders(bool enabled)
    {
        foreach (Collider variantCollider in activeColliders)
        {
            if (variantCollider != null)
                variantCollider.enabled = enabled;
        }
    }

    public void HandleVariantTrigger(Collider sourceCollider, Collider other)
    {
        if (hitProcessed)
            return;

        if (!IsActiveVariantCollider(sourceCollider) || !HasActiveVisual())
            return;

        PlaneInteraction plane = other.GetComponentInParent<PlaneInteraction>();

        if (plane == null)
            return;

        hitProcessed = true;
        plane.HitObstacle();
        SetActiveVariantColliders(false);
        gameObject.SetActive(false);
    }

    private bool IsActiveVariantCollider(Collider sourceCollider)
    {
        if (sourceCollider == null || !sourceCollider.enabled || activeVisual == null)
            return false;

        Transform sourceTransform = sourceCollider.transform;
        return sourceTransform == activeVisual || sourceTransform.IsChildOf(activeVisual);
    }

    private bool HasActiveVisual()
    {
        if (activeVisual == null || !activeVisual.gameObject.activeInHierarchy)
            return false;

        Renderer[] renderers = activeVisual.GetComponentsInChildren<Renderer>(false);
        return System.Array.Exists(
            renderers,
            renderer => renderer != null && renderer.enabled);
    }
}
