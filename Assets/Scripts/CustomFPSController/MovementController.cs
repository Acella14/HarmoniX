using UnityEngine;
using static default_Models;

public enum MovementMode
{
    Grounded,
    Jumping,
    Falling,
    Sliding,
    WallRunning,
    GroundPounding
}

public class MovementController
{
    private CharacterController characterController;
    private MovementMode currentMode;
    private MovementMode lastMode;
    public MovementMode CurrentMode => currentMode;
    private bool isGrounded;
    private int groundStableCount = 0;
    private int airStableCount    = 0;
    private const int stableFramesThreshold = 3;
    public bool IsGrounded => isGrounded;
    public bool IsSliding => currentMode == MovementMode.Sliding;
    public bool IsJumping => currentMode == MovementMode.Jumping;
    public bool IsFalling => currentMode == MovementMode.Falling;
    private Transform transform;

    private PlayerSettingsModel settings;

    private Vector3 velocity;
    private Vector3 smoothedMovement;
    private Vector3 smoothedVelocity;
    private float gravity;

    private LayerMask groundMask;
    private Transform groundCheck;
    private float groundDistance;

    private float jumpStartTime;
    private Vector3 jumpHorizontalMomentum;
    private float estimatedTimeToApex;

    private int airJumpsRemaining;

    // Slide
    private float slideTimer;
    private float lastSlideTime;
    private Vector3 slideDirection;
    private float slideDuration;
    private AnimationCurve slideCurve;
    private float slideSpeed;
    public bool SlideJustEnded => lastMode == MovementMode.Sliding && currentMode != MovementMode.Sliding;
    public Vector3 Velocity => velocity;

    // wall-run fields
    private bool    canWallRun;
    private Vector3 wallNormal;
    private float   wallRunTimer;
    private float   maxWallRunTime = 1.5f;
    private float   wallRunGravityScale = 0.05f;
    private Vector3 wallRunDirection;
    private float   maxCornerAngle       = 45f;
    private int wallRunMissedFrames = 0;
    private const int maxWallRunMisses = 3;
    private float discourageWallRunUntil = 0f;

    public Vector3 WallNormal => wallNormal;
    public Vector3 WallRunDirection => wallRunDirection;
    public bool     IsWallRunning => currentMode == MovementMode.WallRunning;

    public float WeaponAnimSpeed { get; private set; }

    public MovementController(
        CharacterController controller,
        Transform transform,
        PlayerSettingsModel settings,
        Transform groundCheck,
        LayerMask groundMask,
        float groundDistance,
        float slideDuration,
        AnimationCurve slideCurve,
        float slideSpeed,
        float gravity)
    {
        this.characterController = controller;
        this.transform = transform;
        this.settings = settings;

        this.groundCheck = groundCheck;
        this.groundMask = groundMask;
        this.groundDistance = groundDistance;
        this.slideDuration = slideDuration;
        this.slideCurve = slideCurve;
        this.slideSpeed = slideSpeed;
        this.gravity = gravity;
    }


    public void ResetAirJumps() => airJumpsRemaining = settings.MaxAirJumps;

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -1f;

        float t = Mathf.Clamp01((Time.time - jumpStartTime) / estimatedTimeToApex);
        float apexEase = 1f + (1f - Mathf.Cos(t * Mathf.PI)) * 2f; // starts at 1, peaks at 3, smooth ease-out

        float gravityMultiplier = velocity.y > 0f ? apexEase : settings.GravityFallMultiplier;

        velocity.y += gravity * gravityMultiplier * Time.deltaTime;

    }

    private void DecayJumpMomentum()
    {
        float damping = settings.JumpMomentumDecayRate;
        jumpHorizontalMomentum = Vector3.Lerp(jumpHorizontalMomentum, Vector3.zero, damping * Time.deltaTime);
    }


    public void UpdateGroundedStatus()
    {
        // 1) broad sphere check
        bool sphereHit = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask);

        // 2) raycast + slope filter
        bool rayHit = Physics.Raycast(
            groundCheck.position + Vector3.up * 0.1f,
            Vector3.down,
            out RaycastHit hitInfo,
            groundDistance + 0.1f,
            groundMask);

        bool slopeOk = false;
        if (rayHit)
        {
            float angle = Vector3.Angle(hitInfo.normal, Vector3.up);
            slopeOk = angle <= characterController.slopeLimit;
        }

        // 3) combine into one “raw” grounded result
        bool rawGrounded = sphereHit || (rayHit && slopeOk);

        // 4) debounce over a few frames
        if (rawGrounded)
        {
            groundStableCount++;
            airStableCount = 0;
        }
        else
        {
            airStableCount++;
            groundStableCount = 0;
        }

        if (groundStableCount >= stableFramesThreshold)
            isGrounded = true;
        else if (airStableCount >= stableFramesThreshold)
            isGrounded = false;
    }

    private bool CheckForWall(out RaycastHit outHit)
    {
        Vector3 origin = characterController.transform.position;
        float dist     = 0.6f;

        // directions to test
        Vector3[] dirs = { transform.forward, transform.right, -transform.right };
        foreach (var dir in dirs)
        {
            if (Physics.Raycast(origin, dir, out var hit, dist))
            {
                if (hit.collider.GetComponent<WallRunnable>() != null)
                {
                    outHit = hit;
                    return true;
                }
            }
        }

        outHit = default;
        return false;
    }

    public bool TryJump(PlayerStance stance, System.Func<float, bool> stanceCheck, float jumpHeight, Vector3 directionalBoost, bool allowCrouchJump = false)
    {
        if (isGrounded)
        {
            if (stance == PlayerStance.Crouch && !allowCrouchJump)
                return false;

            ApplyJump(jumpHeight, directionalBoost, 1f);
            return true;
        }
        else if (CanAirJump)
        {
            ConsumeAirJump();
            ApplyJump(jumpHeight, directionalBoost, 1f);
            return true;
        }

        return false;
    }


    public void ApplyJump(float jumpHeight, Vector3 directionalInfluence, float movementInfluenceMultiplier = 1f)
    {
        float vertical = Mathf.Sqrt(jumpHeight * -2f * gravity);
        velocity.y = vertical;

        jumpHorizontalMomentum = new Vector3(directionalInfluence.x, 0f, directionalInfluence.z)
                                * settings.JumpMovementInfluence;
                                //* movementInfluenceMultiplier;

        jumpStartTime = Time.time;
        estimatedTimeToApex = vertical / -gravity;
    }


    public bool CanAirJump => airJumpsRemaining > 0;
    public void ConsumeAirJump() => airJumpsRemaining--;

    public void ApplyKnockback(Vector3 force)
    {
        velocity += force;
    }

    public void SetVerticalVelocity(float y) {
        velocity.y = y;
    }

    public void AddVelocity(Vector3 delta) {
        velocity += delta;
    }

    public void UpdateMovement(Vector2 input, PlayerStance stance, bool isSprinting, float deltaTime)
    {
        UpdateGroundedStatus();

        if (HandleWallRun(input, deltaTime))
            return;

        ApplyGravity();

        if (!isGrounded)
        {
            DecayJumpMomentum();
            settings.SpeedEffector = settings.FallingSpeedEffector;
        }
        else
        {
            // if I'm still “going up”, hang on to my jumpMomentum
            if (velocity.y <= 0f)
                jumpHorizontalMomentum = Vector3.zero;

            settings.SpeedEffector = (stance == PlayerStance.Crouch)
                ? settings.CrouchSpeedEffector
                : 1f;
        }

        float vSpeed = isSprinting ? settings.RunningForwardSpeed : settings.WalkingForwardSpeed;
        float hSpeed = isSprinting ? settings.RunningStrafeSpeed : settings.WalkingStrafeSpeed;

        vSpeed *= settings.SpeedEffector;
        hSpeed *= settings.SpeedEffector;

        Vector3 target;

        if (CurrentMode == MovementMode.Sliding)
        {
            slideTimer += deltaTime;
            if (slideTimer >= slideDuration)
            {
                StopSlide();
            }

            float slideProgress = Mathf.Clamp01(slideTimer / slideDuration);
            float speedMultiplier = slideCurve.Evaluate(slideProgress);
            target = slideDirection * slideSpeed * speedMultiplier;
        }
        else
        {
            target = new Vector3(hSpeed * input.x, 0f, vSpeed * input.y);
            target = transform.TransformDirection(target);
        }


        smoothedMovement = Vector3.SmoothDamp(
            smoothedMovement,
            target,
            ref smoothedVelocity,
            isGrounded ? settings.MovementSmoothing : settings.FallingSmoothing
        );

        Vector3 finalMove = smoothedMovement + jumpHorizontalMomentum;
        finalMove.y += velocity.y;
        characterController.Move(finalMove * deltaTime);
        UpdateMovementMode();

        WeaponAnimSpeed = characterController.velocity.magnitude / (settings.WalkingForwardSpeed * settings.SpeedEffector);
        if (WeaponAnimSpeed > 1f) WeaponAnimSpeed = 1f;
    }

    private bool HandleWallRun(Vector2 input, float deltaTime)
    {
        // --- A) Try to start wall-run ---
        if (!isGrounded && currentMode != MovementMode.WallRunning && Time.time >= discourageWallRunUntil && input.y > 0.1f)
        {
            if (CheckForWall(out var hit))
            {
                float verticality = Vector3.Angle(hit.normal, Vector3.up);
                if (verticality > 80f && verticality < 100f)
                {
                    // pick run direction
                    var tangent = Vector3.Cross(hit.normal, Vector3.up).normalized;
                    if (Vector3.Dot(tangent, transform.forward) < 0f)
                        tangent = -tangent;

                    // commit to wall‐run
                    wallNormal       = hit.normal;
                    wallRunDirection = tangent;
                    wallRunTimer     = 0f;
                    velocity.y       = 0f;
                    currentMode      = MovementMode.WallRunning;
                }
            }
        }

        // --- B) If we’re wall-running, update and maybe exit ---
        if (currentMode == MovementMode.WallRunning)
        {
            Debug.Log("Wallrunning");
            wallRunTimer += deltaTime;
            
            // 1) Check we’re still on an acceptable wall
            if (CheckForWall(out var hit))
            {
                wallRunMissedFrames = 0;
                var cornerDelta = Vector3.Angle(hit.normal, wallNormal);
                if (cornerDelta > maxCornerAngle)
                {
                    currentMode = MovementMode.Falling;
                    return true;
                }
                wallNormal = hit.normal;
            }
            else
            {
                wallRunMissedFrames++;
                if (wallRunMissedFrames >= maxWallRunMisses)
                {
                    currentMode = MovementMode.Falling;
                    return true;
                }
            }

            // 2) Check input/time limits
            if (wallRunTimer >= maxWallRunTime || input.y <= 0f)
            {
                currentMode = MovementMode.Falling;
                return true;
            }

            // 3) Apply movement along the wall tangent + reduced gravity
            Vector3 wallMove = wallRunDirection 
                            * settings.WalkingForwardSpeed 
                            * input.y;                        // hold forward to maintain run

            velocity.y += gravity * wallRunGravityScale * deltaTime;

            Vector3 final = wallMove + Vector3.up * velocity.y;
            characterController.Move(final * deltaTime);

            return true; // skip the rest of UpdateMovement
        }

        return false;
    }

    private void UpdateMovementMode()
    {
        if (slideTimer > 0f)
        {
            SetMode(MovementMode.Sliding);
        }
        else if (!isGrounded)
        {
            SetMode(velocity.y > 0f ? MovementMode.Jumping : MovementMode.Falling);
        }
        else
        {
            SetMode(MovementMode.Grounded);
        }
    }

    private void SetMode(MovementMode newMode)
    {
        if (currentMode != newMode)
        {
            lastMode = currentMode;
            currentMode = newMode;
        }
    }

    public bool CanSlide(float currentTime, Vector2 input, float cooldown)
    {
        return currentMode != MovementMode.Sliding
            && currentTime - lastSlideTime >= cooldown
            && input.magnitude > 0.1f;
    }

    public void StartSlide(Vector3 direction, float currentTime)
    {
        slideTimer = 0f;
        lastSlideTime = currentTime;
        slideDirection = direction.normalized;
        SetMode(MovementMode.Sliding);
    }

    public void StopSlide()
    {
        slideTimer = 0f;
        SetMode(isGrounded ? MovementMode.Grounded : MovementMode.Falling);
    }

    public void StopWallRun()
    {
        SetMode(MovementMode.Falling);
        wallRunTimer = maxWallRunTime;
        discourageWallRunUntil = Time.time + 0.2f;
    }

}