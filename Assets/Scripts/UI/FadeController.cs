using System;
using System.Collections;
using UnityEngine;

public class FadeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnityEngine.UI.Image fadeImage;


    [Header("Settings")]
    [Min(0.05f)]
    [SerializeField] private float fadeDuration = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float sceneStartAlpha = 225f / 255f;


    private Coroutine fadeCoroutine;

    private void OnEnable()
    {
        fadeImage.gameObject.SetActive(true);
        FadeIn();
    }

    
    public void FadeIn()
    {
        fadeImage.raycastTarget = true;

        StartFade(sceneStartAlpha, 0f);
    }


    public void FadeOut()
    {
        if (fadeImage != null)
        {
            fadeImage.transform.SetAsLastSibling();
        }

        fadeImage.raycastTarget = true;

        StartFade(0f, 1f);
    }

    public void FadeOut(float duration)
    {
        fadeDuration = Mathf.Max(0.01f, duration);
        FadeOut();
    }

    public void ResetFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
            fadeImage.raycastTarget = false;
        }
    }


    private void StartFade(float from, float to)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);


        fadeCoroutine =
            StartCoroutine(
                Fade(from, to)
            );
    }


    private IEnumerator Fade(float from, float to)
    {
        Color color =
            fadeImage.color;

        color.a = from;

        fadeImage.color = color;


        float elapsed = 0f;


        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;


            float t =
                elapsed / fadeDuration;


            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            color.a =
                Mathf.Lerp(
                    from,
                    to,
                    t
                );


            fadeImage.color = color;


            yield return null;
        }


        color.a = to;

        fadeImage.color = color;


        // وقتی کاملاً شفاف شد،
        // دیگر جلوی کلیک UI را نگیرد.
        if (to <= 0f)
        {
            fadeImage.raycastTarget = false;
        }


        fadeCoroutine = null;
    }
}
