using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingFeedback : MonoBehaviour
{
    [Header("Post-Processing Profile")]
    public Volume postProcessingVolume;

    [Header("Vignette Settings")]
    public Color damageColor = Color.red;
    public float vignetteIntensityBoost = 0.4f;

    [Header("Chromatic Aberration")]
    public float chromaticBoost = 1f;

    [Header("Effect Timing")]
    public float duration = 0.5f;

    private Vignette vignette;
    private ChromaticAberration chromatic;

    private Color originalVignetteColor;
    private float originalVignetteIntensity;
    private float originalChromatic;

    public float InitialVignetteIntensity  { get; private set; }
    public float InitialChromaticIntensity { get; private set; }

    private void Start()
    {
        if (postProcessingVolume != null && postProcessingVolume.profile != null)
        {
            postProcessingVolume.profile.TryGet(out vignette);
            postProcessingVolume.profile.TryGet(out chromatic);

            if (vignette != null)
            {
                originalVignetteIntensity = vignette.intensity.value;
                InitialVignetteIntensity = originalVignetteIntensity;
            }

            if (chromatic != null)
            {
                originalChromatic = chromatic.intensity.value;
                InitialChromaticIntensity = originalChromatic;
            }
        }
    }

    public void PlayDamageEffect()
    {
        StopAllCoroutines();
        StartCoroutine(DamageEffectRoutine());
    }

    private IEnumerator DamageEffectRoutine()
    {
        float half = duration / 2f;
        float timer = 0f;

        while (timer < half)
        {
            float t = timer / half;

            if (vignette != null)
            {
                vignette.color.value = Color.Lerp(originalVignetteColor, damageColor, t);
                vignette.intensity.value = Mathf.Lerp(originalVignetteIntensity, originalVignetteIntensity + vignetteIntensityBoost, t);
            }

            if (chromatic != null)
            {
                chromatic.intensity.value = Mathf.Lerp(originalChromatic, chromaticBoost, t);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;
        while (timer < half)
        {
            float t = timer / half;

            if (vignette != null)
            {
                vignette.color.value = Color.Lerp(damageColor, originalVignetteColor, t);
                vignette.intensity.value = Mathf.Lerp(originalVignetteIntensity + vignetteIntensityBoost, originalVignetteIntensity, t);
            }

            if (chromatic != null)
            {
                chromatic.intensity.value = Mathf.Lerp(chromaticBoost, originalChromatic, t);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Final snap-back
        if (vignette != null)
        {
            vignette.color.value = originalVignetteColor;
            vignette.intensity.value = originalVignetteIntensity;
        }

        if (chromatic != null)
        {
            chromatic.intensity.value = originalChromatic;
        }
    }

    public void SetEffectIntensity(float vignetteIntensity, float chromaticIntensity)
    {
        if (vignette != null)
            vignette.intensity.value = vignetteIntensity;
        if (chromatic != null)
            chromatic.intensity.value = chromaticIntensity;
    }
}
