using UnityEngine;

public class DroneLightFader : MonoBehaviour
{
    [SerializeField] private Light droneLight;
    [SerializeField] private float lowIntensity = 0f;
    [SerializeField] private float highIntensity = 5f;
    [SerializeField] private float fadeDuration = 0.5f;

    private float timer;
    private float currentStartIntensity;
    private float targetIntensity;

    private void Awake()
    {
        currentStartIntensity = lowIntensity;
        targetIntensity = highIntensity;

        droneLight.intensity = currentStartIntensity;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float t = timer / fadeDuration;

        droneLight.intensity = Mathf.Lerp(
            currentStartIntensity,
            targetIntensity,
            t
        );

        if (t >= 1f)
        {
            SwitchTarget();
        }
    }

    private void SwitchTarget()
    {
        timer = 0f;

        currentStartIntensity = targetIntensity;

        targetIntensity = Mathf.Approximately(
            targetIntensity,
            highIntensity
        )
        ? lowIntensity
        : highIntensity;
    }
}