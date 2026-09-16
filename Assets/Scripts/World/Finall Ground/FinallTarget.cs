using UnityEngine;

public class FinallTarget : MonoBehaviour
{
    private Vector3 initialInnerPosition;
    private Collider pickupCollider;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();

    }

    private void OnEnable()
    {
        if (pickupCollider != null)
            pickupCollider.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlaneInteraction plane =
            other.GetComponent<PlaneInteraction>();

        if (plane == null)
            return;

        plane.HitObstacle(true);

        gameObject.SetActive(false);
    }

}
