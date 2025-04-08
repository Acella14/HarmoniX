using UnityEngine;

public class ShockwaveController : MonoBehaviour {
    [Header("Shader Material")]
    public Material shockwaveMaterial;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip shockwaveSFX;

    private float timer = 0f;
    private float duration;
    private float maxRadius;
    private float originalStrength;
    private Vector3 origin;
    private bool isPlaying = false;

    public void TriggerShockwave(Vector3 center, ShockwaveSettings settings) {
        origin = center;
        timer = 0f;
        isPlaying = true;
        originalStrength = settings.strength;
        duration = settings.duration;
        maxRadius = settings.maxRadius;

        if (audioSource != null && shockwaveSFX != null) {
            audioSource.PlayOneShot(shockwaveSFX, 1f);
        }

        shockwaveMaterial.SetVector("_ShockwaveOrigin", origin);
        shockwaveMaterial.SetFloat("_ShockwaveStrength", settings.strength);
        shockwaveMaterial.SetFloat("_ShockwaveThickness", settings.thickness);
        shockwaveMaterial.SetColor("_ShockwaveEmissionColor", settings.emissionColor);
        shockwaveMaterial.SetFloat("_ShockwaveEnabled", 1f);
    }

    void Update() {
        if (!isPlaying) return;

        timer += Time.deltaTime;
        float progress = timer / duration;

        float currentRadius = Mathf.Lerp(0f, maxRadius, progress);
        shockwaveMaterial.SetFloat("_ShockwaveRadius", currentRadius);

        float currentStrength = Mathf.Lerp(originalStrength, 0f, progress);
        shockwaveMaterial.SetFloat("_ShockwaveStrength", currentStrength);

        if (progress >= 1f) {
            isPlaying = false;

            shockwaveMaterial.SetFloat("_ShockwaveRadius", 0f);
            shockwaveMaterial.SetFloat("_ShockwaveStrength", originalStrength);
            shockwaveMaterial.SetFloat("_ShockwaveEnabled", 0f);
        }
    }
}
