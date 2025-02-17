using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HarmonixSetup : MonoBehaviour
{
    [Header("Canvas Interactions")]
    public GameObject canvas;
    public SpriteRenderer spriteRenderer;
    public Sprite finalHarmonixSprite;
    public float fadeDuration = 1.0f;

    [Header("Audio Clips")]
    public AudioSource audioSource;
    public AudioClip startUpClick;
    public AudioClip startUpThump;
    public AudioClip[] clickSoundEffects;

    [Header("Intro Fade")]
    public Volume globalVolume;
    public float vignetteLerpDuration = 4.0f;

    private Vignette vignette;
    private float currentLerpTime = 0;

    private Animator animator;
    private CanvasGroup canvasGroup;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
        }
        if (globalVolume == null)
        {
            Debug.LogError("Volume component is not assigned.");
            return;
        }

        if (globalVolume.profile.TryGet(out vignette))
        {
            StartCoroutine(LerpVignetteIntensity());
        }
        else
        {
            Debug.LogError("No Vignette override found in the Volume profile.");
        }

        if (canvas != null)
        {
            canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = canvas.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;  // Start fully transparent
            canvas.SetActive(false); // Initially deactivate the canvas
        }
        else
        {
            Debug.LogError("Canvas is not assigned.");
        }
    }

    IEnumerator LerpVignetteIntensity()
    {
        float startIntensity = 1.0f;
        float endIntensity = 0.0f;

        while (currentLerpTime < vignetteLerpDuration)
        {
            currentLerpTime += Time.deltaTime;
            float t = currentLerpTime / vignetteLerpDuration;
            vignette.intensity.value = Mathf.Lerp(startIntensity, endIntensity, t);

            yield return null; // Wait for the next frame
        }

        vignette.intensity.value = endIntensity;
    }

    public void PlayStartUpClick() {
        if (audioSource != null && startUpClick != null) {
            audioSource.PlayOneShot(startUpClick);
        }
    }

    public void PlayStartUpThump() {
        if (audioSource != null && startUpThump != null) {
            audioSource.PlayOneShot(startUpThump);
        }
    }

    public void PlayRandomClickSound() {
        if (audioSource != null && clickSoundEffects.Length > 0) {
            int randomIndex = Random.Range(0, clickSoundEffects.Length);
            AudioClip randomClip = clickSoundEffects[randomIndex];

            audioSource.PlayOneShot(randomClip);
        }
    }

    public void TurnOnWaveformEditor()
    {
        animator.enabled = false;
        spriteRenderer.sprite = finalHarmonixSprite;

        if (canvas != null)
        {
            canvas.SetActive(true);
            StartCoroutine(FadeInCanvas());
            PlayRandomClickSound();
        }
    }

    private IEnumerator FadeInCanvas()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            t = t * t * (3f - 2f * t);

            canvasGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

}
