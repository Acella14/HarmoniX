using UnityEngine;
using static default_Models;
using System.Collections;
using Cinemachine;

public class CustomWeaponController : MonoBehaviour
{
    #region - Variable Declarations -
    private CustomCharacterController characterController;

    [Header("References")]
    public Animator weaponAnimator;
    public GameObject muzzleFlashVFX;

    [Header("Settings")]
    public WeaponSettingsModel settings;
    public float weaponDamage;

    bool isInitialized;
    Vector3 newWeaponRotation;
    Vector3 newWeaponRotationVelocity;

    Vector3 targetWeaponRotation;
    Vector3 targetWeaponRotationVelocity;

    Vector3 newWeaponMovementRotation;
    Vector3 newWeaponMovementRotationVelocity;

    Vector3 targetWeaponMovementRotation;
    Vector3 targetWeaponMovementRotationVelocity;

    private CinemachineVirtualCamera playerVirtualCamera;

    [Header("Audio")]
    public AudioClip shotSFX;

    [Header("Zoom Settings")]
    public float zoomIntensity = 10f;
    public float zoomDuration = 0.1f;

    private Camera mainCamera;
    private float defaultFOV;

    #endregion

    private void Start()
    {
        newWeaponRotation = transform.localRotation.eulerAngles;
        mainCamera = Camera.main;
        defaultFOV = mainCamera.fieldOfView;

        if (RhythmManager.Instance != null)
        {
            RhythmManager.Instance.OnBeat.AddListener(OnBeat);
        }
    }

    private void OnDestroy()
    {
        if (RhythmManager.Instance != null)
        {
            RhythmManager.Instance.OnBeat.RemoveListener(OnBeat);
        }
    }

    public void Initialize(CustomCharacterController CharacterController)
    {
        characterController = CharacterController;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        CalculateWeaponRotation();
        SetWeaponAnimations();
    }

    private void CalculateWeaponRotation()
    {
        targetWeaponRotation.y += settings.SwayAmount * (settings.SwayXInverted ? -characterController.input_View.x
         : characterController.input_View.x) * Time.deltaTime;
        targetWeaponRotation.x += settings.SwayAmount * (settings.SwayYInverted ? characterController.input_View.y
         : -characterController.input_View.y) * Time.deltaTime;

        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -settings.SwayClampX, settings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -settings.SwayClampY, settings.SwayClampY);
        targetWeaponRotation.z = targetWeaponRotation.y;

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, 
         ref targetWeaponRotationVelocity, settings.SwayResetSmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, 
         ref newWeaponRotationVelocity, settings.SwaySmoothing);

        targetWeaponMovementRotation.z = settings.MovementSwayX * (settings.MovementSwayXInverted
         ? -characterController.input_Movement.x : characterController.input_Movement.x);
        targetWeaponMovementRotation.x = settings.MovementSwayY * (settings.MovementSwayYInverted
         ? -characterController.input_Movement.y : characterController.input_Movement.y);

        targetWeaponMovementRotation = Vector3.SmoothDamp(targetWeaponMovementRotation, Vector3.zero, 
         ref targetWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);
        newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponMovementRotation, targetWeaponMovementRotation, 
         ref newWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);

        transform.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);
    }

    public void SetWeaponAnimations()
    {
        weaponAnimator.SetBool("isSprinting", characterController.isSprinting);
        weaponAnimator.SetFloat("WeaponAnimationSpeed", characterController.weaponAnimationSpeed);
    }

    public void OnBeat()
    {
        if (characterController.GetIsGrounded())
        {
            weaponAnimator.SetTrigger("BeatThrust");
        }
    }

    public void TriggerMuzzleFlash()
    {
        if (muzzleFlashVFX != null)
        {
            StartCoroutine(PlayMuzzleFlash());
        }
    }

    private IEnumerator PlayMuzzleFlash()
    {
        muzzleFlashVFX.SetActive(true);

        yield return new WaitForSeconds(0.4f);

        muzzleFlashVFX.SetActive(false);
    }

    public void HandleZoom(float targetFOV = 50f, float duration = 0.2f)
    {
        if (playerVirtualCamera != null)
        {
            playerVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(playerVirtualCamera.m_Lens.FieldOfView, targetFOV, duration);
        }
        else
        {
            Debug.LogWarning("PlayerVirtualCamera is not assigned in CustomWeaponController.");
        }
    }

    public void SetVirtualCamera(CinemachineVirtualCamera virtualCamera)
    {
        playerVirtualCamera = virtualCamera;
    }
}
