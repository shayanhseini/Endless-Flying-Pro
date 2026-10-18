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

    private Collider pickupCollider;
    private Transform activeVisual;
    private Vector3 rotationAxis;
    private float rotationSpeed;
    private static int nextVariantIndex = -1;
    
    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();
        CacheChildVariantsIfNeeded();
    }
    
    private void OnEnable()
    {
        SelectRandomVariant();

        float randomScale = Random.Range(minScale, maxScale);
        transform.localScale = Vector3.one * randomScale;

        
        if (pickupCollider != null)
            pickupCollider.enabled = true;

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

        CacheChildVariantsIfNeeded();

        if (asteroidVariants == null || asteroidVariants.Length == 0)
        {
            if (box != null)
            {
                box.SetActive(true);
                activeVisual = box.transform;
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
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlaneInteraction plane =
            other.GetComponent<PlaneInteraction>();

        if (plane == null)
            return;

        plane.HitObstacle();
        
        if (pickupCollider != null)
            pickupCollider.enabled = false;
        
        if (box != null)
            box.SetActive(false);
        
        gameObject.SetActive(false);

    }
}
