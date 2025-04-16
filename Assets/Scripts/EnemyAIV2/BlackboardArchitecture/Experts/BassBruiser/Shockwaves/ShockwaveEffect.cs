using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShockwaveEffect : MonoBehaviour {
    [Header("Shockwave Controller")]
    public ShockwaveGPUController shockwaveGPUController;

    [Header("Shockwave Settings")]
    [SerializeReference]
    public ShockwaveSettings shockwaveSettings;

    [Header("Physics Settings")]
    public GameObject shockwaveExplosionPrefab;
    public float explosionLifetime = 3f;
    public float radius = 5f;
    public float force = 10f;
    public int damage = 20;
    public LayerMask playerMask;
    public Vector3 explosionOffset = Vector3.zero;

    [Header("Audio")]
    public AudioClip shockwaveSFX;


    public void LaunchNearbyPlayers() {
        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, radius, playerMask);
        foreach (var hit in hitPlayers) {
            if (hit.TryGetComponent<IShockwaveLaunchable>(out var launchable)) {
                launchable.LaunchFromShockwave(transform.position, force, radius, damage);
            }
        }
    }

    public void TriggerShockwave(Vector3 center, ShockwaveSettings settings) {
        if (shockwaveExplosionPrefab != null) {
            Vector3 spawnPos = center + transform.TransformDirection(explosionOffset);
            GameObject instance = Instantiate(shockwaveExplosionPrefab, spawnPos, Quaternion.identity);
            instance.transform.localScale = Vector3.one * settings.particleScale;
            Destroy(instance, explosionLifetime);
        }

        if (shockwaveGPUController != null) {
            shockwaveGPUController.AddShockwave(center, settings);
        }

        shockwaveGPUController.sfxSource.PlayOneShot(shockwaveSFX);
    }


    #if UNITY_EDITOR
    [ContextMenu("Set Radial Shockwave Settings")]
    private void SetRadialShockwaveSettings() {
        shockwaveSettings = new RadialShockwaveSettings {
            maxRadius = 8f,
            duration = 1f,
            strength = 1f,
            thickness = 0.75f,
            emissionColorStart = Color.white,
            emissionColorEnd = Color.yellow,
            particleScale = 1f
        };
    }

    [ContextMenu("Set Line Shockwave Settings")]
    private void SetLineShockwaveSettings() {
        shockwaveSettings = new LineShockwaveSettings {
            duration = 1.5f,
            strength = 0.8f,
            emissionColorStart = Color.blue,
            emissionColorEnd = Color.cyan,
            particleScale = 1f,
            target = transform.position + transform.forward * 5f,
            width = 0.3f
        };
    }

    #endif


}
