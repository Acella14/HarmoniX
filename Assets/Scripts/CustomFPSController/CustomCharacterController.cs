using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static default_Models;
using BlackboardSystem;
using UnityServiceLocator;

public class CustomCharacterController : MonoBehaviour, IExpert
{
    #region - Variable Declarations -
    private CharacterController characterController;
    private DefaultInput defaultInput;

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

    [Header("Gravity and Jump")]
    public float gravity = -9.81f;
    public float jumpHeight = 1f;
    public AudioClip jumpSFX;

    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGrounded;

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

    private Vector3 newMovementSpeed;
    private Vector3 newMovementSpeedVelocity;

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

    private float slideTimer;
    private bool isSliding;
    private float lastSlideTime;
    private Vector3 slideDirection;
    private Vector3 lastMovementDirection;
    private float defaultCameraFOV;
    private float currentCameraFOVVelocity;
    private float cameraTilt;
    private float cameraTiltVelocity;
    private Camera mainCamera;
    private float slideCameraTiltOffset;
    private float slideCameraFOVOffset;
    public float minPitch = 0.2f;
    public float maxPitch = 1.4f;


    [Header("Blackboard Interaction")]
    private Blackboard blackboard;
    private BlackboardKey playerLastPositionKey;

    #endregion

    #region - Awake -

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        defaultInput = new DefaultInput();

        defaultInput.Character.Movement.performed += e => input_Movement = e.ReadValue<Vector2>();
        defaultInput.Character.View.performed += e => input_View = e.ReadValue<Vector2>();
        defaultInput.Character.Jump.performed += e => Jump();
        defaultInput.Character.Crouch.performed += e => Crouch();
        defaultInput.Character.Sprint.performed += e => ToggleSprint();
        defaultInput.Character.SprintReleased.performed += e => StopSprint();
        defaultInput.Character.Slide.performed += e => Slide();

        defaultInput.Enable();

        newCameraRotation = cameraHolder.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;

        characterController = GetComponent<CharacterController>();

        cameraHeight = cameraHolder.localPosition.y;

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
        blackboard = ServiceLocator.For(this).Get<BlackboardController>().GetBlackboard();
        ServiceLocator.For(this).Get<BlackboardController>().RegisterExpert(this);
        playerLastPositionKey = blackboard.GetOrRegisterKey("PlayerLastPosition");
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
        if (input_Movement.magnitude > 0.1f)
        {
            lastMovementDirection = new Vector3(input_Movement.x, 0, input_Movement.y).normalized;
            lastMovementDirection = transform.TransformDirection(lastMovementDirection);
        }

        CalculateView();
        CalculateMovementAndGravity();
        if (blackboard != null) {
            blackboard.SetValue(playerLastPositionKey, transform.position);
        }
        CalculateStance();
        UpdateAnimatorState();
        UpdateCameraFOV();
    }

    #endregion

    #region - View / Movement -

    private void CalculateView()
    {
        // Character rotation (Yaw)
        newCharacterRotation.y += playerSettings.ViewXSensitivity *
            (playerSettings.ViewXInverted ? -input_View.x : input_View.x) * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(newCharacterRotation);

        // Camera rotation (Pitch)
        newCameraRotation.x += playerSettings.ViewYSensitivity *
            (playerSettings.ViewYInverted ? input_View.y : -input_View.y) * Time.deltaTime;
        newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, viewClampYMin, viewClampYMax);

        // Camera tilt during slide
        float targetTilt = 0f;
        if (isSliding)
        {
            targetTilt = slideCameraTiltOffset;
        }

        cameraTilt = Mathf.SmoothDamp(cameraTilt, targetTilt, ref cameraTiltVelocity, slideCameraFOVSmoothing);

        cameraHolder.localRotation = Quaternion.Euler(newCameraRotation.x, 0f, cameraTilt);
    }

    private void CalculateMovementAndGravity()
    {
        if (input_Movement.y <= 0.2f)
        {
            isSprinting = false;
        }

        var verticalSpeed = playerSettings.WalkingForwardSpeed;
        var horizontalSpeed = playerSettings.WalkingStrafeSpeed;

        if (isSprinting)
        {
            verticalSpeed = playerSettings.RunningForwardSpeed;
            horizontalSpeed = playerSettings.RunningStrafeSpeed;
        }

        if (!characterController.isGrounded)
        {
            playerSettings.SpeedEffector = playerSettings.FallingSpeedEffector;
        }
        else if (playerStance == PlayerStance.Crouch)
        {
            playerSettings.SpeedEffector = playerSettings.CrouchSpeedEffector;
        }
        else
        {
            playerSettings.SpeedEffector = 1;
        }

        verticalSpeed *= playerSettings.SpeedEffector;
        horizontalSpeed *= playerSettings.SpeedEffector;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -1f;
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 targetMovement;

        if (isSliding || slideTimer > 0f)
        {
            if (isSliding)
            {
                slideTimer += Time.deltaTime;

                if (slideTimer >= slideDuration)
                {
                    slideTimer = slideDuration;
                    EndSlide();
                }
            }
            else
            {
                slideTimer += Time.deltaTime;

                if (slideTimer >= slideDuration)
                {
                    slideTimer = 0f;
                }
            }

            float slideProgress = slideTimer / slideDuration;
            float speedMultiplier = slideDecelerationCurve.Evaluate(slideProgress);
            targetMovement = slideDirection * slideSpeed * speedMultiplier;
        }
        else
        {
            targetMovement = new Vector3(horizontalSpeed * input_Movement.x, 0, verticalSpeed * input_Movement.y);
            targetMovement = transform.TransformDirection(targetMovement);
        }

        newMovementSpeed = Vector3.SmoothDamp(
            newMovementSpeed,
            targetMovement,
            ref newMovementSpeedVelocity,
            characterController.isGrounded ? playerSettings.MovementSmoothing : playerSettings.FallingSmoothing
        );

        Vector3 movement = newMovementSpeed;
        movement.y = velocity.y;

        // Move the character
        characterController.Move(movement * Time.deltaTime);

        weaponAnimationSpeed = characterController.velocity.magnitude / (playerSettings.WalkingForwardSpeed * playerSettings.SpeedEffector);
        if (weaponAnimationSpeed > 1)
        {
            weaponAnimationSpeed = 1;
        }

        currentWeaponR?.SetWeaponAnimations();
        currentWeaponL?.SetWeaponAnimations();
    }

    #endregion

    #region - Jumping -

    private void Jump()
    {
        if (isGrounded)
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

            audioSource.PlayOneShot(jumpSFX, 0.2f);
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            currentWeaponR?.weaponAnimator.SetTrigger("Jump");
            currentWeaponL?.weaponAnimator.SetTrigger("Jump");

            if (isSliding)
            {
                EndSlide();
            }
        }
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
        if (isGrounded)
        {
            if (!wasGrounded)
            {
                currentWeaponR?.weaponAnimator.SetTrigger("Land");
                currentWeaponL?.weaponAnimator.SetTrigger("Land");

                currentWeaponR?.weaponAnimator.ResetTrigger("FallingIdle");
                currentWeaponL?.weaponAnimator.ResetTrigger("FallingIdle");
            }
        }
        else
        {
            if (velocity.y < 0 && !(currentWeaponR?.weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("FallingIdle") ?? true))
            {
                currentWeaponR?.weaponAnimator.SetTrigger("FallingIdle");
                currentWeaponL?.weaponAnimator.SetTrigger("FallingIdle");
            }
        }

        currentWeaponR?.weaponAnimator.SetBool("isGrounded", isGrounded);
        currentWeaponL?.weaponAnimator.SetBool("isGrounded", isGrounded);

        currentWeaponR?.weaponAnimator.SetFloat("verticalVelocity", velocity.y);
        currentWeaponL?.weaponAnimator.SetFloat("verticalVelocity", velocity.y);

        wasGrounded = isGrounded;
    }

    #endregion

    #region - Sliding -

    private void Slide()
    {
        if (!isSliding && Time.time - lastSlideTime >= slideCooldown && input_Movement.magnitude > 0.1f)
        {
            audioSource.pitch = 0.2f;

            audioSource.PlayOneShot(slideSFX, 0.1f);

            audioSource.pitch = 1.0f;

            isSliding = true;
            slideTimer = 0f;
            lastSlideTime = Time.time;

            if (playerStance != PlayerStance.Crouch)
            {
                playerStance = PlayerStance.Crouch;
            }

            slideDirection = lastMovementDirection;
            if (slideDirection.magnitude == 0f)
            {
                slideDirection = transform.forward;
            }

            slideCameraTiltOffset = 0f;
            slideCameraFOVOffset = 0f;

            float forwardAmount = Vector3.Dot(slideDirection.normalized, transform.forward);
            float rightAmount = Vector3.Dot(slideDirection.normalized, transform.right);

            if (Mathf.Abs(rightAmount) > Mathf.Abs(forwardAmount))
            {
                // Left/Right slide
                slideCameraTiltOffset = (Random.value < 0.5f ? -1f : 1f) * slideCameraTilt;
                slideCameraFOVOffset = 0f;
            }
            else
            {
                // Forward/Backward slide
                slideCameraTiltOffset = 0f;

                if (forwardAmount > 0)
                {
                    slideCameraFOVOffset = -slideFOVAdjustAmount;
                }
                else
                {
                    slideCameraFOVOffset = slideFOVAdjustAmount;
                }
            }
        }
    }


    private void UpdateCameraFOV()
    {
        if (mainCamera == null) return;

        float targetFOV = defaultCameraFOV;
        if (isSliding)
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

    private void EndSlide()
    {
        isSliding = false;

        if (!StanceCheck(playerStandStance.StanceCollider.height))
        {
            playerStance = PlayerStance.Stand;
        }
    }


    #endregion

    #region - Getters -

    public bool GetIsGrounded()
    {
        return isGrounded;
    }

    public AudioSource GetAudioSource()
    {
        return audioSource;
    }

    #endregion
}