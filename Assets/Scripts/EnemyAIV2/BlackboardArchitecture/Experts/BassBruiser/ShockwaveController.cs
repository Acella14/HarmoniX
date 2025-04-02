using UnityEngine;

public class ShockwaveController : MonoBehaviour {
    [Header("Shader Material")]
    public Material shockwaveMaterial;

    [Header("Effect Settings")]
    public float maxRadius = 10f;
    public float duration = 1.5f;
    public float strength = 0.5f;
    public float thickness = 1.0f;
    public Color emissionColor = Color.cyan;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip shockwaveSFX;

    private float timer = 0f;
    private float originalStrength;
    private Vector3 origin;
    private bool isPlaying = false;

    public void TriggerShockwave(Vector3 center) {
        origin = center;
        timer = 0f;
        isPlaying = true;
        originalStrength = strength; // Store original strength
        if (audioSource != null && shockwaveSFX != null) {
            audioSource.PlayOneShot(shockwaveSFX, 1f);
        }


        shockwaveMaterial.SetVector("_ShockwaveOrigin", origin);
        shockwaveMaterial.SetFloat("_ShockwaveStrength", originalStrength);
        shockwaveMaterial.SetFloat("_ShockwaveThickness", thickness);
        shockwaveMaterial.SetColor("_ShockwaveEmissionColor", emissionColor);
        shockwaveMaterial.SetFloat("_ShockwaveEnabled", 1f);
    }

    void Update() {
        if (!isPlaying) return;

        timer += Time.deltaTime;
        float progress = timer / duration;

        // Update radius
        float currentRadius = Mathf.Lerp(0f, maxRadius, progress);
        shockwaveMaterial.SetFloat("_ShockwaveRadius", currentRadius);

        // Linearly fade out strength
        float currentStrength = Mathf.Lerp(originalStrength, 0f, progress);
        shockwaveMaterial.SetFloat("_ShockwaveStrength", currentStrength);

        if (progress >= 1f) {
            isPlaying = false;

            // Reset all shader values as needed
            shockwaveMaterial.SetFloat("_ShockwaveRadius", 0f);
            shockwaveMaterial.SetFloat("_ShockwaveStrength", originalStrength); // restore
            shockwaveMaterial.SetFloat("_ShockwaveEnabled", 0f);
        }
    }
}
