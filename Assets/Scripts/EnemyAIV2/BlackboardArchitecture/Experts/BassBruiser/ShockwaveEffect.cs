using UnityEngine;

public class ShockwaveEffect : MonoBehaviour {
    [Header("Shockwave Settings")]
    public GameObject shockwaveExplosionPrefab;
    public float explosionScale = 1f;
    public float explosionLifetime = 3f;
    public float radius = 5f;
    public float force = 10f;
    public LayerMask playerMask;

    [Header("Shockwave Visual")]
    public ShockwaveController shockwaveController;
    public Vector3 explosionOffset = Vector3.zero;

    public void LaunchNearbyPlayers() {
        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, radius, playerMask);
        foreach (var hit in hitPlayers) {
            if (hit.TryGetComponent<IShockwaveLaunchable>(out var launchable)) {
                launchable.LaunchFromShockwave(transform.position, force, radius);
            }
        }
    }
    
    public void TriggerVisualShockwave(Vector3 center) {
    if (shockwaveExplosionPrefab != null) {
        Vector3 spawnPos = center + transform.TransformDirection(explosionOffset);

        GameObject instance = Instantiate(shockwaveExplosionPrefab, spawnPos, Quaternion.identity);
        instance.transform.localScale = Vector3.one * explosionScale;
        Destroy(instance, explosionLifetime);
    }

    if (shockwaveController != null) {
        shockwaveController.TriggerShockwave(center);
    }
}
}
