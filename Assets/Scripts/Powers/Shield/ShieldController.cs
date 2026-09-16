using System.Collections;
using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float shieldDuration = 5f;
    [SerializeField] private float visualLeadTime = 0.25f;


    [Header("Visual")]
    [SerializeField] private GameObject shieldVisual;

    [SerializeField] private float shieldScale = 1.4f;
    [SerializeField] private float scaleDuration = 0.25f;


    private float remainingTime;

    private Coroutine scaleCoroutine;

    private bool visualDeactivated;


    public bool IsActive =>
        remainingTime > 0f;


    public float RemainingTime =>
        remainingTime;


    private void Awake()
    {
        if (shieldVisual == null)
            return;


        shieldVisual.SetActive(true);

        shieldVisual.transform.localScale =
            Vector3.zero;
    }


    private void Update()
    {
        if (!IsActive)
            return;


        remainingTime -= Time.deltaTime;


        // Visual کمی زودتر از خود Shield خاموش شود
        if (!visualDeactivated &&
            remainingTime <= visualLeadTime)
        {
            visualDeactivated = true;

            DeactivateShieldVisual();
        }


        if (remainingTime <= 0f)
        {
            remainingTime = 0f;

            DeactivateShield();
        }
    }


    public void Activate()
    {
        remainingTime =
            shieldDuration;


        visualDeactivated = false;


        ActivateShieldVisual();


        GameEvents.RaiseShieldActivated();
    }


    public void Consume()
    {
        if (!IsActive)
            return;


        remainingTime = 0f;


        DeactivateShield();
    }


    private void DeactivateShield()
    {
        DeactivateShieldVisual();


        GameEvents.RaiseShieldDeactivated();
    }


    private void ActivateShieldVisual()
    {
        if (shieldVisual == null)
            return;


        shieldVisual.SetActive(true);


        StartScaleAnimation(
            Vector3.zero,
            Vector3.one * shieldScale
        );
    }


    private void DeactivateShieldVisual()
    {
        if (shieldVisual == null)
            return;


        StartScaleAnimation(
            shieldVisual.transform.localScale,
            Vector3.zero
        );
    }


    private void StartScaleAnimation(
        Vector3 from,
        Vector3 to)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);


        scaleCoroutine =
            StartCoroutine(
                ScaleAnimation(from, to)
            );
    }


    private IEnumerator ScaleAnimation(
        Vector3 from,
        Vector3 to)
    {
        shieldVisual.transform.localScale =
            from;


        float elapsed = 0f;


        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    scaleDuration
                );


            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            shieldVisual.transform.localScale =
                Vector3.Lerp(
                    from,
                    to,
                    t
                );


            yield return null;
        }


        shieldVisual.transform.localScale =
            to;


        scaleCoroutine = null;
    }
    
    public void ForceDeactivate()
    {
        if (!IsActive)
            return;

        remainingTime = 0f;
        visualDeactivated = true;

        DeactivateShieldVisual();

        GameEvents.RaiseShieldDeactivated();
    }
}