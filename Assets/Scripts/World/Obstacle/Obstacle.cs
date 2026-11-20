using System.Collections;
using UnityEngine;


public class Obstacle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject box;
    [SerializeField] private Transform inner;


    [Header("Inner Animation")]
    [SerializeField] private float moveHeight = 1.5f;
    [SerializeField] private float moveDuration = 1f;
    
    private Vector3 initialInnerPosition;
    private Collider pickupCollider;
    private bool hasHit;
    
    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();


        if (inner != null)
        {
            initialInnerPosition =
                inner.localPosition;
        }
    }
    
    private void OnEnable()
    {
        hasHit = false;

        if (inner != null)
        {
            inner.localPosition =
                initialInnerPosition;

            inner.gameObject.SetActive(true);
        }
        
        if (box != null)
            box.SetActive(true);

        
        if (pickupCollider != null)
            pickupCollider.enabled = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlaneInteraction plane =
            other.GetComponentInParent<PlaneInteraction>();

        if (plane == null)
            plane = other.GetComponent<PlaneInteraction>();

        if (plane == null)
            return;

        hasHit = true;

        plane.HitObstacle();
        
        if (pickupCollider != null)
            pickupCollider.enabled = false;
        
        if (box != null)
            box.SetActive(false);
        
        if (inner != null)
        {
            StartCoroutine(
                MoveInnerAndDisable()
            );
        }
        else
        {
            gameObject.SetActive(false);
        };
    }
    
    private IEnumerator MoveInnerAndDisable()
    {
        Vector3 startPosition =
            initialInnerPosition;


        Vector3 targetPosition =
            initialInnerPosition +
            Vector3.up * moveHeight;


        float elapsed = 0f;


        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed / moveDuration
                );


            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            inner.localPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );


            yield return null;
        }


        inner.localPosition =
            targetPosition;
        
        gameObject.SetActive(false);
    }
}