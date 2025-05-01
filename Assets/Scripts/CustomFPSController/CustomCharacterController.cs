using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static default_Models;
using BlackboardSystem;
using UnityServiceLocator;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomCharacterController : MonoBehaviour, IExpert, IShockwaveLaunchable, IKnockbackReceiver
{
    #region - Variable Declarations -
    private CharacterController characterController;
    private PlayerInputHandler inputHandler;
    private ViewController viewController;
    private MovementController movementController;

    [HideInInspector]
    public Vector2 input_Movement;
    [HideInInspector]
    public Vector2 input_View;

    private Vector3 newCameraRotation;
    private Vector3 newCharacterRotation;

    [Header("References")]
    public AudioSource audioSource;
    public AudioSource SFXSource;
    public Transform cameraHolder;
    public Transform groundCheck;
    public LayerMask groundMask;

    [Header("Settings")]
    public PlayerSettingsModel playerSettings;
    public float viewClampYMin = -70f;
    public float viewClampYMax = 80f;
    public float groundDistance = 0.4f;
    public LayerMask playerMask;
    private PlayerHealth health;
    private PostProcessingFeedback postFX;

    [Header("Gravity and Jump")]
    public float gravity = -9.81f;
    public float jumpHeight = 1f;
    public AudioClip jumpSFX;

    [Header("Footsteps")]
    public AudioClip[] footstepClips;
    public float walkFootstepInterval = 0.5f;    // seconds between steps at your ‘normal’ speed
    public float runFootstepInterval  = 0.35f;   // seconds between steps at your top speed
    public float minStepPitch = 0.9f;
    public float maxStepPitch = 1.1f;

    private float footstepTimer = 0f;

    [Header("Stance")]
    public PlayerStance playerStance;
    public float playerStanceSmoothing;

    public CharacterStance playerStandStance;
    public CharacterStance playerCrouchStance;
    private float stanceCheckErrorMargin = 0.05f;

    private float cameraHeight;
    private float cameraHeightVelocity;

    private Vector3 stanceCapsuleCenterVelocity;
    private float stanceCapsuleHeightVelocity;

    [HideInInspector]
    public bool isSprinting;

    [Header("Weapon")]
    public CustomWeaponController currentWeaponR;
    public CustomWeaponController currentWeaponL;
    public float weaponAnimationSpeed;

    [Header("Slide Settings")]
    public float slideSpeed = 10f;
    public float slideDuration = 1f;
    public float slideCooldown = 1f;
    public AnimationCurve slideDecelerationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public float slideCameraFOVSmoothing = 0.1f;
    public float slideCameraTilt = 10f;
    public float slideFOVAdjustAmount = 5f;
    public AudioClip slideSFX;
    private bool wasCrouchFromSlide;

    [Header("Wall-Jump Settings")]
    [Tooltip("How strongly you shoot away from the wall when you jump.")]
    public float wallJumpAwayForce = 8f;
    [Tooltip("How much vertical lift you get when you jump off a wall.")]
    public float wallJumpUpForce   = 6f;
    public float wallRunCameraTilt = 15f;
    private float wallRunCameraTiltOffset;


    [Header("Ground Pound (& shockwave)")]
    public ShockwaveEffect shockwaveEffect;
    public float groundPoundForce = 30f;
    public float groundPoundCooldown = 1f;
    private bool isGroundPounding = false;


    private Vector3 lastMovementDirection;
    private float defaultCameraFOV;
    private float currentCameraFOVVelocity;
    private Camera mainCamera;
    private float slideCameraTiltOffset;
    private float slideCameraFOVOffset;
    public float minPitch = 0.2f;
    public float maxPitch = 1.4f;


    [Header("Blackboard Interaction")]
    private Blackboard blackboard;
    private BlackboardKey playerLastPositionKey;
    private BlackboardKey playerGroundPointKey;

    #endregion

    #region - Awake -

    private void Awake()
    {
        newCameraRotation = cameraHolder.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;
        characterController = GetComponent<CharacterController>();
        cameraHeight = cameraHolder.localPosition.y;

        Cursor.lockState = CursorLockMode.Locked;

        inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.OnMove += val => input_Movement = val;
            inputHandler.OnView += val => input_View = val;
            inputHandler.OnJump += Jump;
            inputHandler.OnCrouch += Crouch;
            inputHandler.OnSprint += ToggleSprint;
            inputHandler.OnSprintRelease += StopSprint;
            inputHandler.OnSlide += Slide;
            inputHandler.OnGroundPound += TryGroundPound;
        }

        viewController = new ViewController(
            transform,
            cameraHolder,
            playerSettings,
            viewClampYMin,
            viewClampYMax,
            slideCameraFOVSmoothing
        );

        movementController = new MovementController(
            characterController,
            transform,
            playerSettings,
            groundCheck,
            groundMask,
            groundDistance,
            slideDuration,
            slideDecelerationCurve,
            slideSpeed,
            gravity
        );


        if (currentWeaponR)
        {
            currentWeaponR.Initialize(this);
        }

        if (currentWeaponL)
        {
            currentWeaponL.Initialize(this);
        }

        mainCamera = cameraHolder.GetComponentInChildren<Camera>();
        if (mainCamera != null)
        {
            defaultCameraFOV = mainCamera.fieldOfView;
        }
    }

    #endregion


    void Start() {
        health = GetComponent<PlayerHealth>();
        postFX = GetComponent<PostProcessingFeedback>();

        if (health != null)
        {
            UIManager.Instance.Init(health);

            health.OnDamaged += postFX.PlayDamageEffect;
        }

        blackboard = ServiceLocator.For(this).Get<BlackboardController>().GetBlackboard();
        ServiceLocator.For(this).Get<BlackboardController>().RegisterExpert(this);
        playerLastPositionKey = blackboard.GetOrRegisterKey("PlayerLastPosition");
        playerGroundPointKey = blackboard.GetOrRegisterKey("PlayerGroundPoint");
    }

    public int GetInsistence(Blackboard blackboard) {
        return 100; // Always update player's position
    }

    public void Execute(Blackboard blackboard) {
        blackboard.AddAction(() => {
            blackboard.SetValue(playerLastPositionKey, transform.position);
        });
    }



    #region - Update -

    private void Update()
    {
        movementController.UpdateMovement(input_Movement, playerStance, isSprinting, Time.deltaTime);

        bool grounded = movementController.IsGrounded;
        bool movingInput = input_Movement.sqrMagnitude > 0.01f;
        if (grounded && movingInput)
        {
            float speed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude;

            float t = Mathf.InverseLerp(
                playerSettings.WalkingForwardSpeed,
                playerSettings.RunningForwardSpeed,
                speed
            );

            float interval = Mathf.Lerp(walkFootstepInterval, runFootstepInterval, t);

            footstepTimer += Time.deltaTime;
            if (footstepTimer >= interval)
            {
                PlayFootstep();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        bool onGroundOrWall = movementController.IsGrounded || movementController.IsWallRunning;

        if (onGroundOrWall
            && movementController.CurrentMode != MovementMode.Jumping)
        {
            movementController.ResetAirJumps();
        }

        if (wasCrouchFromSlide &&
            movementController.IsGrounded &&
            playerStance == PlayerStance.Crouch &&
            !movementController.IsSliding &&
            !StanceCheck(playerStandStance.StanceCollider.height))
        {
            playerStance = PlayerStance.Stand;
            wasCrouchFromSlide = false;
        }

        if (movementController.IsWallRunning)
        {
            float sideDot = Vector3.Dot(transform.right, movementController.WallNormal);
            wallRunCameraTiltOffset = -Mathf.Sign(sideDot) * wallRunCameraTilt;
        }
        else
        {
            wallRunCameraTiltOffset = 0f;
        }

        float dynamicTilt = slideCameraTiltOffset + wallRunCameraTiltOffset;

        viewController.UpdateView(input_View, movementController.IsSliding || movementController.IsWallRunning, dynamicTilt);
        UpdateCameraFOV();

        CalculateStance();

        UpdateAnimatorState();

        if (blackboard != null)
        {
            blackboard.SetValue(playerLastPositionKey, transform.position);

            Ray ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
            {
                blackboard.SetValue(playerGroundPointKey, hit.point);
            }
            else
            {
                blackboard.SetValue(playerGroundPointKey, transform.position);
            }
        }
    }

    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        // pick a random clip
        var clip = footstepClips[ Random.Range(0, footstepClips.Length) ];
        // randomize pitch in a small range
        //audioSource.pitch = Random.Range(minStepPitch, maxStepPitch);
        audioSource.PlayOneShot(clip, 1f);
    }


    #endregion


    #region - Ground Pound -

    private void TryGroundPound()
    {
        if (movementController.IsGrounded || isGroundPounding) return;

        StartCoroutine(DoGroundPound());
    }


    private IEnumerator DoGroundPound()
    {
        isGroundPounding = true;

        movementController.SetVerticalVelocity(-Mathf.Abs(groundPoundForce));

        while (!movementController.IsGrounded)
            yield return null;

        if (shockwaveEffect == null) {
            Debug.LogError("[GP] no ShockwaveEffect assigned!");
        } else if (shockwaveEffect.shockwaveSettings == null) {
            Debug.LogError("[GP] shockwaveSettings on ShockwaveEffect is null!");
        } else {
            shockwaveEffect.LaunchNearby();

            Vector3 center = transform.position;
            RaycastHit hit;
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            if (Physics.Raycast(origin, Vector3.down, out hit, groundDistance + 1f, groundMask))
                center = hit.point;

            shockwaveEffect.TriggerShockwave(center, shockwaveEffect.shockwaveSettings);
        }

        yield return new WaitForSeconds(groundPoundCooldown);
        isGroundPounding = false;
    }


    #endregion


    #region - Jumping -

    private void Jump()
    {
        if (movementController.IsWallRunning)
        {
            Vector3 away = movementController.WallNormal * wallJumpAwayForce;
            Vector3 up   = Vector3.up               * wallJumpUpForce;
            Vector3 jumpDir = (away + up).normalized;

            movementController.ApplyJump(jumpHeight, jumpDir, 1f);

            audioSource.PlayOneShot(jumpSFX, 0.2f);
            currentWeaponR?.weaponAnimator.SetTrigger("Jump");
            currentWeaponL?.weaponAnimator.SetTrigger("Jump");

            movementController.StopWallRun();
            return;
        }

        bool wasGrounded = movementController.IsGrounded;

        Vector3 inputDir      = new Vector3(input_Movement.x, 0f, input_Movement.y).normalized;
        Vector3 directionalDir = transform.TransformDirection(inputDir);
        Vector3 jumpBoost      = wasGrounded
                                ? directionalDir
                                : Vector3.zero;

        bool jumped = movementController.TryJump(
            playerStance,
            height => StanceCheck(height),
            jumpHeight,
            jumpBoost,
            allowCrouchJump: true
        );

        if (!jumped && playerStance == PlayerStance.Crouch 
            && !StanceCheck(playerStandStance.StanceCollider.height))
        {
            playerStance = PlayerStance.Stand;
        }

        if (jumped)
        {
            audioSource.PlayOneShot(jumpSFX, 0.2f);
            currentWeaponR?.weaponAnimator.SetTrigger("Jump");
            currentWeaponL?.weaponAnimator.SetTrigger("Jump");
            if (movementController.IsSliding)
            {
                movementController.StopSlide();
            }

            if (wasGrounded)
            {
                float verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                float timeToApex       = verticalVelocity / -gravity;
                viewController.AnimateJumpTilt(directionalDir, timeToApex);
            }
        }
    }



    public void LaunchFromShockwave(Vector3 origin, float force, float radius, int damage)
    {
        Debug.Log("Launched player from shockwave");
        StartCoroutine(ShakeCamera(1f, 0.8f, 1.5f));

        // Direction from shockwave origin to player
        Vector3 direction = (transform.position - origin).normalized;

        // Apply mostly vertical force
        float verticalForce = force;
        movementController.SetVerticalVelocity(verticalForce);

        // Apply a small horizontal push
        float horizontalScale = 0.1f; // Only 10% of force goes into horizontal
        Vector3 horizontalPush = new Vector3(direction.x, 0f, direction.z) * (force * horizontalScale);
        movementController.AddVelocity(horizontalPush);

        health?.TakeDamage(damage);
    }

    public void ApplyKnockback(Vector3 force) {
        movementController.AddVelocity(force);
        
        StartCoroutine(ShakeCamera(0.3f, 0.5f, 1f));
        Debug.Log("Knockback applied to player: " + force);
    }


    public IEnumerator ShakeCamera(float duration, float amplitude, float frequency)
    {
        var virtualCam = cameraHolder.GetComponentInChildren<Cinemachine.CinemachineVirtualCamera>();
        var noise = virtualCam.GetCinemachineComponent<Cinemachine.CinemachineBasicMultiChannelPerlin>();

        float timer = 0f;
        float halfDuration = duration * 0.5f;

        // Fade in
        while (timer < halfDuration)
        {
            float t = timer / halfDuration;
            noise.m_AmplitudeGain = Mathf.Lerp(0f, amplitude, t);
            noise.m_FrequencyGain = Mathf.Lerp(0f, frequency, t);
            timer += Time.deltaTime;
            yield return null;
        }

        // Fade out
        timer = 0f;
        while (timer < halfDuration)
        {
            float t = timer / halfDuration;
            noise.m_AmplitudeGain = Mathf.Lerp(amplitude, 0f, t);
            noise.m_FrequencyGain = Mathf.Lerp(frequency, 0f, t);
            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure reset
        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;
    }


    #endregion

    #region - Stance -

    private void CalculateStance()
    {
        var currentStance = playerStance == PlayerStance.Crouch ? playerCrouchStance : playerStandStance;

        cameraHeight = Mathf.SmoothDamp(cameraHolder.localPosition.y, currentStance.CameraHeight, ref cameraHeightVelocity, playerStanceSmoothing);
        cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x, cameraHeight, cameraHolder.localPosition.z);

        characterController.height = Mathf.SmoothDamp(characterController.height, currentStance.StanceCollider.height, ref stanceCapsuleHeightVelocity, playerStanceSmoothing);
        characterController.center = Vector3.SmoothDamp(characterController.center, currentStance.StanceCollider.center, ref stanceCapsuleCenterVelocity, playerStanceSmoothing);
    }

    private void Crouch()
    {
        if (playerStance == PlayerStance.Crouch)
        {
            if (StanceCheck(playerStandStance.StanceCollider.height))
            {
                return;
            }

            playerStance = PlayerStance.Stand;
            return;
        }

        if (StanceCheck(playerCrouchStance.StanceCollider.height))
        {
            return;
        }

        playerStance = PlayerStance.Crouch;
        wasCrouchFromSlide = false;
    }

    private bool StanceCheck(float stanceCheckHeight)
    {
        Vector3 start = groundCheck.position + Vector3.up * (characterController.radius + stanceCheckErrorMargin);
        Vector3 end = groundCheck.position + Vector3.up * (-characterController.radius - stanceCheckErrorMargin + stanceCheckHeight);

        return Physics.CheckCapsule(start, end, characterController.radius, playerMask);
    }

    #endregion

    #region - Sprinting -

    private void ToggleSprint()
    {
        if (input_Movement.y <= 0.2f)
        {
            isSprinting = false;
            return;
        }

        isSprinting = !isSprinting;
    }

    private void StopSprint()
    {
        if (playerSettings.SprintingHold)
        {
            isSprinting = false;
        }
    }

    #endregion

    #region - Animator Updates -

    private void UpdateAnimatorState()
    {
        if (movementController.CurrentMode == MovementMode.Grounded)
        {
            currentWeaponR?.weaponAnimator.SetTrigger("Land");
            currentWeaponL?.weaponAnimator.SetTrigger("Land");

            currentWeaponR?.weaponAnimator.ResetTrigger("FallingIdle");
            currentWeaponL?.weaponAnimator.ResetTrigger("FallingIdle");
        }
        else if (movementController.IsFalling)
        {
            var fallingStateActive = currentWeaponR?.weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("FallingIdle") ?? false;
            if (!fallingStateActive)
            {
                currentWeaponR?.weaponAnimator.SetTrigger("FallingIdle");
                currentWeaponL?.weaponAnimator.SetTrigger("FallingIdle");
            }
        }

        currentWeaponR?.weaponAnimator.SetBool("isGrounded", movementController.IsGrounded);
        currentWeaponL?.weaponAnimator.SetBool("isGrounded", movementController.IsGrounded);

        currentWeaponR?.weaponAnimator.SetFloat("verticalVelocity", movementController.Velocity.y);
        currentWeaponL?.weaponAnimator.SetFloat("verticalVelocity", movementController.Velocity.y);
    }

    #endregion

    #region - Sliding -

    private void Slide()
    {
        if (movementController.CanSlide(Time.time, input_Movement, slideCooldown))
        {
            audioSource.pitch = 0.2f;
            audioSource.PlayOneShot(slideSFX, 0.1f);
            audioSource.pitch = 1f;

            wasCrouchFromSlide = true;
            playerStance = PlayerStance.Crouch;

            Vector3 inputDirection = new Vector3(input_Movement.x, 0, input_Movement.y).normalized;
            Vector3 worldDirection = transform.TransformDirection(inputDirection);
            Vector3 direction = input_Movement.magnitude > 0.1f ? worldDirection : transform.forward;

            movementController.StartSlide(direction, Time.time);

            float forwardAmount = Vector3.Dot(direction.normalized, transform.forward);
            float rightAmount = Vector3.Dot(direction.normalized, transform.right);

            slideCameraTiltOffset = (Mathf.Abs(rightAmount) > Mathf.Abs(forwardAmount)) 
                ? (Random.value < 0.5f ? -1f : 1f) * slideCameraTilt 
                : 0f;

            slideCameraFOVOffset = (Mathf.Abs(rightAmount) > Mathf.Abs(forwardAmount)) 
                ? 0f 
                : (forwardAmount > 0 ? -slideFOVAdjustAmount : slideFOVAdjustAmount);
        }
    }



    private void UpdateCameraFOV()
    {
        if (mainCamera == null) return;

        float targetFOV = defaultCameraFOV;
        if (movementController.IsSliding)
        {
            targetFOV += slideCameraFOVOffset;
        }

        mainCamera.fieldOfView = Mathf.SmoothDamp(
            mainCamera.fieldOfView,
            targetFOV,
            ref currentCameraFOVVelocity,
            slideCameraFOVSmoothing
        );
    }

    #endregion

    #region - Getters -

    public bool GetIsGrounded()
    {
        return movementController.IsGrounded;
    }

    public AudioSource GetAudioSource()
    {
        return audioSource;
    }

    #endregion
}