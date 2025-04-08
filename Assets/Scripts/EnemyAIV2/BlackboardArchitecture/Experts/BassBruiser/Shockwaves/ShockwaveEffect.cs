using UnityEngine;

public class ShockwaveEffect : MonoBehaviour {
    [Header("Physics Settings")]
    public GameObject shockwaveExplosionPrefab;
    public float explosionLifetime = 3f;
    public float radius = 5f;
    public float force = 10f;
    public int damage = 20;
    public LayerMask playerMask;
    public Vector3 explosionOffset = Vector3.zero;

    [Header("Shockwave Controller")]
    public ShockwaveController shockwaveController;

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

        if (shockwaveController != null) {
            shockwaveController.TriggerShockwave(center, settings);
        }
    }
}
