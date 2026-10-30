using UnityEngine;

/// <summary>
/// Controls a dynamic blinking / flashing light on power-up boxes.
/// Provides authentic rhythmic beacon flashing with configurable rate, duty cycle, and intensity.
/// </summary>
public class BlinkingLight : MonoBehaviour
{
    public enum BlinkStyle
    {
        Flash,      // Distinct on/off blinking
        Pulse       // Smooth pulsing glow
    }

    [Header("Target Light")]
    [SerializeField] private Light targetLight;

    [Header("Blink Settings")]
    [SerializeField] private BlinkStyle style = BlinkStyle.Flash;

    [Tooltip("Flashes per second")]
    [SerializeField] private float blinkRate = 2.5f;

    [Tooltip("Peak light intensity when ON")]
    [SerializeField] private float maxIntensity = 6f;

    [Tooltip("Light intensity when OFF/dim")]
    [SerializeField] private float minIntensity = 0f;

    [Header("Flash Timing")]
    [Range(0.1f, 0.9f)]
    [Tooltip("Percentage of the cycle the light stays lit")]
    [SerializeField] private float onDutyCycle = 0.45f;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();
    }

    private void Update()
    {
        if (targetLight == null)
            return;

        if (style == BlinkStyle.Flash)
        {
            float cycle = (Time.time * blinkRate) % 1f;
            bool isOn = cycle < onDutyCycle;
            targetLight.intensity = isOn ? maxIntensity : minIntensity;
            targetLight.enabled = isOn || minIntensity > 0.01f;
        }
        else
        {
            float t = (Mathf.Sin(Time.time * blinkRate * Mathf.PI * 2f) + 1f) * 0.5f;
            targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            targetLight.enabled = targetLight.intensity > 0.01f;
        }
    }

    private void OnDisable()
    {
        if (targetLight != null)
            targetLight.enabled = false;
    }

    private void OnEnable()
    {
        if (targetLight != null)
            targetLight.enabled = true;
    }
}
