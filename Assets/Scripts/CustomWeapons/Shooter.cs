using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class Shooter : MonoBehaviour
{
    public CustomCharacterController player;
    public CinemachineVirtualCamera playerVirtualCamera;
    public Camera mainCamera;
    public float range = 1000f;  
    public LayerMask shootableLayers;
    public ComboMultiplier comboMultiplier;

    [Header("Bullet Trail Settings")]
    public TrailRenderer bulletTrailPrefab;  
    public float bulletSpeed = 300f;  
    public bool addBulletSpread = true;
    public Vector3 bulletSpreadVariance = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("Bullet Spawn Transforms")]
    public Transform leftGunSpawn;  
    public Transform rightGunSpawn; 

    [Header("FOV Settings")]
    public float zoomOutFOVOffset = 20f; // How much to zoom out during slow time

    private int lastShotBeatIndex = -1;

    private Coroutine fovZoomCoroutine;
    private float defaultFOV;

    private AudioSource[] audioSources;
    [SerializeField] private float critPitchAdjustmentFactor = 0.5f;

    private int critMultiplier = 0;


    private void Start()
    {
        if (playerVirtualCamera != null)
        {
            defaultFOV = playerVirtualCamera.m_Lens.FieldOfView;
        }

        if (player.currentWeaponL != null)
        {
            player.currentWeaponL.SetVirtualCamera(playerVirtualCamera);
        }

        if (player.currentWeaponR != null)
        {
            player.currentWeaponR.SetVirtualCamera(playerVirtualCamera);
        }

        audioSources = FindObjectsOfType<AudioSource>();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            float shotTime = RhythmManager.Instance.GetCurrentSongTime();

            critMultiplier = comboMultiplier.ConsumeCritMultiplier();
            bool isCritical = critMultiplier > 0;

            if (isCritical)
            {
                SlowTimeEffect();
            }

            float leftButtonValue = Mouse.current.leftButton.ReadValue();
            float rightButtonValue = Mouse.current.rightButton.ReadValue();

            if (leftButtonValue > 0)
            {
                Shoot(player.currentWeaponL, leftGunSpawn, shotTime, isCritical);
                player.SFXSource.PlayOneShot(player.currentWeaponL.shotSFX, 1f);
                player.currentWeaponL.TriggerMuzzleFlash();
                player.currentWeaponL.weaponAnimator.SetTrigger("Shoot");

                if (!isCritical)
                {
                    float targetFOV = Mathf.Clamp(defaultFOV - player.currentWeaponL.zoomIntensity, 10f, 179f); // Prevent overly small or large FOV values
                    AdjustFOV(targetFOV, player.currentWeaponL.zoomDuration, true);
                }
            }
            else if (rightButtonValue > 0)
            {
                Shoot(player.currentWeaponR, rightGunSpawn, shotTime, isCritical);
                player.SFXSource.PlayOneShot(player.currentWeaponR.shotSFX, 1f);
                player.currentWeaponR.TriggerMuzzleFlash();
                player.currentWeaponR.weaponAnimator.SetTrigger("Shoot");

                if (!isCritical)
                {
                    float targetFOV = Mathf.Clamp(defaultFOV - player.currentWeaponR.zoomIntensity, 10f, 179f);
                    AdjustFOV(targetFOV, player.currentWeaponR.zoomDuration, true);
                }
            }
        }
    }

    private void Shoot(CustomWeaponController weaponController, Transform gunSpawnPoint, float shotTime, bool isCritical)
    {
        int currentBeatIndex = RhythmManager.Instance != null ? RhythmManager.Instance.CurrentBeatIndex : -1;
        bool isOnBeat = RhythmManager.Instance != null && RhythmManager.Instance.IsOnBeatNow(shotTime);

        if (currentBeatIndex == lastShotBeatIndex)
        {
            isOnBeat = false;
        }

        Transform virtualCameraTransform = playerVirtualCamera.transform;
        Vector3 direction = virtualCameraTransform.forward;

        if (addBulletSpread)
        {
            direction += new Vector3(
                Random.Range(-bulletSpreadVariance.x, bulletSpreadVariance.x),
                Random.Range(-bulletSpreadVariance.y, bulletSpreadVariance.y),
                Random.Range(-bulletSpreadVariance.z, bulletSpreadVariance.z)
            );
            direction.Normalize();
        }

        Vector3 offset = CalculateSpawnOffset(gunSpawnPoint);
        Vector3 spawnPosition = gunSpawnPoint.position + offset;

        RaycastHit hit;
        if (Physics.Raycast(virtualCameraTransform.position, direction, out hit, range, shootableLayers))
        {
            Enemy enemy = hit.transform.GetComponent<Enemy>();
            if (enemy != null)
            {
                float damage = weaponController.weaponDamage;

                if (critMultiplier > 0)
                {
                    damage *= Mathf.Pow(2, critMultiplier); // Double damage for each stack
                }

                if (isOnBeat)
                {
                    comboMultiplier.RegisterSuccessfulShot();
                    lastShotBeatIndex = currentBeatIndex;
                }
                else
                {
                    comboMultiplier.RegisterMissedShot();
                }

                enemy.TakeDamage(damage);
            }

            TrailRenderer trail = Instantiate(bulletTrailPrefab, spawnPosition, Quaternion.identity);
            ApplyTrailFading(trail);
            StartCoroutine(SpawnBulletTrail(trail, spawnPosition, hit.point));
        }
        else
        {
            comboMultiplier.RegisterMissedShot();
            TrailRenderer trail = Instantiate(bulletTrailPrefab, spawnPosition, Quaternion.identity);
            ApplyTrailFading(trail);
            StartCoroutine(SpawnBulletTrail(trail, spawnPosition, gunSpawnPoint.position + direction * range));
        }
    }


    // Slow-motion effect
    private void SlowTimeEffect()
    {
        float targetTimeScale = 0.2f;
        float timeDownDuration = 0.01f;
        float timeHoldDuration = 0.4f;
        float timeUpDuration = 0.3f;

        StartCoroutine(InterpolateTimeScale(targetTimeScale, timeDownDuration, timeHoldDuration, timeUpDuration));
        StartCoroutine(SlowDownRoutine());
    }

    private IEnumerator SlowDownRoutine()
    {
        float targetFOV = defaultFOV + zoomOutFOVOffset;
        // Lerp to the target FOV
        yield return StartCoroutine(LerpFOV(targetFOV, 0.1f, false));

        // Hold at the target FOV
        yield return new WaitForSecondsRealtime(0.3f);

        // Lerp back to the default FOV
        yield return StartCoroutine(LerpFOV(defaultFOV, 0.4f, false));
    }


    private IEnumerator InterpolateTimeScale(float targetScale, float downDuration, float holdDuration, float upDuration)
    {
        float initialTimeScale = Time.timeScale;
        float elapsed = 0f;

        while (elapsed < downDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / downDuration;
            Time.timeScale = Mathf.Lerp(initialTimeScale, targetScale, t);
            AdjustAudioPitch(Time.timeScale); // Update audio pitch
            Time.fixedDeltaTime = 0.02f * Time.timeScale; // Adjust physics step
            yield return null;
        }

        // Hold at target scale for the specified duration
        Time.timeScale = targetScale;
        AdjustAudioPitch(Time.timeScale);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yield return new WaitForSecondsRealtime(holdDuration);

        // Interpolate back up to 1.0 (default)
        elapsed = 0f;
        while (elapsed < upDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / upDuration;
            Time.timeScale = Mathf.Lerp(targetScale, 1f, t);
            AdjustAudioPitch(Time.timeScale);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        // Ensure values are reset precisely
        Time.timeScale = 1f;
        AdjustAudioPitch(1f);
        Time.fixedDeltaTime = 0.02f;
    }

    private void AdjustAudioPitch(float timeScale)
    {
        foreach (var audioSource in audioSources)
        {
            float adjustedPitch = Mathf.Lerp(1f, timeScale, critPitchAdjustmentFactor);
            audioSource.pitch = adjustedPitch;
        }
    }


    private Vector3 CalculateSpawnOffset(Transform gunSpawnPoint)
    {
        Vector3 playerVelocity = player.GetComponent<CharacterController>().velocity;

        float playerSpeed = playerVelocity.magnitude;

        Vector3 localVelocity = player.transform.InverseTransformDirection(playerVelocity);

        // Calculate lateral movement (left/right) in local space
        float lateralMovement = localVelocity.x;

        bool isLeftGun = gunSpawnPoint == leftGunSpawn;

        // Initialize the offset
        float offsetX = 0f;
        float offsetZ = 0f;

        // Apply offset based on the gun being used and player movement in local space
        if (isLeftGun && lateralMovement > 0.1f)
        {
            // If shooting with the left gun while moving right, avoid offset
            offsetX = 0f;
        }
        else if (!isLeftGun && lateralMovement < -0.1f)
        {
            // If shooting with the right gun while moving left, avoid offset
            offsetX = 0f;
        }
        else
        {
            // Apply the regular offset based on movement
            offsetX = lateralMovement * playerSpeed * 0.02f;
        }

        // Forward/backward movement in local space
        if (Mathf.Abs(localVelocity.z) > 0.1f)
        {
            offsetZ = localVelocity.z * playerSpeed * 0.02f;
        }

        // Log the velocity and offset for debugging
        Debug.Log("Player Speed: " + playerSpeed + ", Local Velocity: " + localVelocity);

        Vector3 localOffset = new Vector3(offsetX, 0f, offsetZ);

        Vector3 worldOffset = player.transform.TransformDirection(localOffset);

        return worldOffset;
    }


    private void ApplyTrailFading(TrailRenderer trail)
    {
        trail.time = 0.2f;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(Color.clear, 1.0f) },
            new GradientAlphaKey[] 
            { 
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );

        trail.colorGradient = gradient;
    }


    private IEnumerator SpawnBulletTrail(TrailRenderer trail, Vector3 startPosition, Vector3 hitPoint)
    {
        float distance = Vector3.Distance(startPosition, hitPoint);
        float remainingDistance = distance;

        float elapsedTime = 0f;
        float followDuration = 0.1f;

        Vector3 currentStartPosition = startPosition;

        while (remainingDistance > 0)
        {
            if (elapsedTime < followDuration)
            {
                currentStartPosition = trail.transform.position;
            }

            trail.transform.position = Vector3.Lerp(currentStartPosition, hitPoint, 1 - (remainingDistance / distance));
            remainingDistance -= bulletSpeed * Time.deltaTime;
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        trail.transform.position = hitPoint;
        Destroy(trail.gameObject, trail.time);
    }

    private void AdjustFOV(float targetFOV, float duration, bool resetToDefault = false)
    {
        if (fovZoomCoroutine != null)
            StopCoroutine(fovZoomCoroutine);

        fovZoomCoroutine = StartCoroutine(LerpFOV(targetFOV, duration, resetToDefault));
    }

    private IEnumerator LerpFOV(float targetFOV, float duration, bool resetToDefault)
    {
        float startFOV = playerVirtualCamera.m_Lens.FieldOfView;
        float elapsed = 0f;

        // Smoothly zoom to the target FOV
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            playerVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
            yield return null;
        }

        playerVirtualCamera.m_Lens.FieldOfView = targetFOV;

        // If resetToDefault is true, return to the default FOV
        if (resetToDefault)
        {
            yield return new WaitForSeconds(0.02f);
            ResetFOV();
        }
    }

    private void ResetFOV()
    {
        AdjustFOV(defaultFOV, 0.15f);
    }



    /* Abilities */
    public void OnUseCritAbility(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (comboMultiplier.UseCritAbility())
            {
                Debug.Log("Crit ability activated!");
            }
            else
            {
                Debug.Log("Not enough combo score or max stacks reached!");
            }
        }
    }


}