using UnityEngine;
using static default_Models;
using System.Collections;

public class ViewController
{
    private readonly Transform playerTransform;
    private readonly Transform cameraHolder;
    private readonly PlayerSettingsModel settings;

    private Vector3 characterRotation;
    private Vector3 cameraRotation;
    private float cameraTilt;
    private float cameraTiltVelocity;

    private readonly float minPitch;
    private readonly float maxPitch;
    private readonly float sensitivityX;
    private readonly float sensitivityY;
    private readonly bool invertX;
    private readonly bool invertY;
    private readonly float tiltSmoothing;

    private float jumpTiltXCurrent;
    private float jumpTiltZCurrent;

    private float jumpTiltTimer;
    private float jumpTiltDuration;
    private float jumpTiltZStart;
    private float jumpTiltXStart;

    private bool jumpTiltActive;

    public ViewController(Transform playerTransform, Transform cameraHolder, PlayerSettingsModel settings, float viewClampYMin, float viewClampYMax, float tiltSmoothing)
    {
        this.playerTransform = playerTransform;
        this.cameraHolder = cameraHolder;

        this.sensitivityX = settings.ViewXSensitivity;
        this.sensitivityY = settings.ViewYSensitivity;
        this.invertX = settings.ViewXInverted;
        this.invertY = settings.ViewYInverted;
        this.minPitch = viewClampYMin;
        this.maxPitch = viewClampYMax;
        this.tiltSmoothing = tiltSmoothing;
        this.settings = settings;

        cameraRotation = cameraHolder.localRotation.eulerAngles;
        characterRotation = playerTransform.localRotation.eulerAngles;
    }

    public void UpdateView(Vector2 inputView, bool isSliding, float slideTiltOffset)
    {
        characterRotation.y += sensitivityX * (invertX ? -inputView.x : inputView.x) * Time.deltaTime;
        playerTransform.localRotation = Quaternion.Euler(characterRotation);

        cameraRotation.x += sensitivityY * (invertY ? inputView.y : -inputView.y) * Time.deltaTime;
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, minPitch, maxPitch);

        float targetTilt = isSliding ? slideTiltOffset : 0f;
        cameraTilt = Mathf.SmoothDamp(cameraTilt, targetTilt, ref cameraTiltVelocity, tiltSmoothing);

        if (jumpTiltActive && jumpTiltDuration > 0f)
        {
            jumpTiltTimer += Time.deltaTime;
            float t = Mathf.Clamp01(jumpTiltTimer / jumpTiltDuration);
            float eased = Mathf.Sin(t * Mathf.PI); // ease-in-out with a smooth peak at t = 0.5

            jumpTiltXCurrent = jumpTiltXStart * eased;
            jumpTiltZCurrent = jumpTiltZStart * eased;

            if (t >= 1f)
            {
                jumpTiltActive = false;
            }
        }

        cameraHolder.localRotation = Quaternion.Euler(cameraRotation.x + jumpTiltXCurrent, 0f, cameraTilt + jumpTiltZCurrent);
    }


    public void AnimateJumpTilt(Vector3 jumpDir, float timeToApex)
    {
        if (jumpDir.sqrMagnitude < 0.01f) return;

        Vector3 local = playerTransform.InverseTransformDirection(jumpDir.normalized);

        float boostScale = Mathf.Clamp01(settings.JumpTiltIntensity / 2f);

        float xTilt = Mathf.Clamp(-local.z * 6f, -4f, 4f) * boostScale;
        float zTilt = Mathf.Clamp(-local.x * 8f, -5f, 5f) * boostScale;

        xTilt += Random.Range(-0.5f, 0.5f);
        zTilt += Random.Range(-0.4f, 0.4f);

        jumpTiltXStart = xTilt;
        jumpTiltZStart = zTilt;

        jumpTiltTimer = 0f;
        jumpTiltDuration = timeToApex * 0.4f;
        jumpTiltActive = true;
    }


}
