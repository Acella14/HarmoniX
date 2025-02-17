using UnityEngine;

public class HandMovementController : MonoBehaviour
{
    public Transform LeftHand;
    public Transform RightHand;
    public Vector3 LeftHandRaiseAmount = Vector3.up * 0.1f; // Raise amounts for the left hand
    public Vector3 RightHandRaiseAmount = Vector3.up * 0.1f; // Raise amounts for the right hand

    public float RaiseDuration = 0.5f; // Time for raising hands
    public float HoldDuration = 0.5f; // Time to hold hands up
    public float ReturnDuration = 0.5f; // Time to return hands to original position

    public Transform LeftHandWeaponSocket;
    public Transform RightHandWeaponSocket;
    public Vector3 LeftHandSocketRotationChange = Vector3.zero;
    public Vector3 RightHandSocketRotationChange = Vector3.zero;

    private Vector3 leftHandOriginalPosition;
    private Vector3 rightHandOriginalPosition;
    private Quaternion leftHandSocketOriginalRotation;
    private Quaternion rightHandSocketOriginalRotation;
    private bool isAnimating = false;
    private float animationStartTime;

    void Start()
    {
        if (LeftHand != null) leftHandOriginalPosition = LeftHand.localPosition;
        if (RightHand != null) rightHandOriginalPosition = RightHand.localPosition;
        if (LeftHandWeaponSocket != null) leftHandSocketOriginalRotation = LeftHandWeaponSocket.localRotation;
        if (RightHandWeaponSocket != null) rightHandSocketOriginalRotation = RightHandWeaponSocket.localRotation;
    }

    public void TriggerHandMovement()
    {
        if (!isAnimating)
        {
            animationStartTime = Time.time;
            isAnimating = true;
        }
    }

    void Update()
    {
        if (isAnimating)
        {
            float elapsedTime = Time.time - animationStartTime;
            float totalDuration = RaiseDuration + HoldDuration + ReturnDuration;

            if (elapsedTime < RaiseDuration)
            {
                float t = elapsedTime / RaiseDuration;
                if (LeftHand != null)
                {
                    Vector3 leftHandTargetPosition = leftHandOriginalPosition + LeftHandRaiseAmount;
                    LeftHand.localPosition = Vector3.Lerp(leftHandOriginalPosition, leftHandTargetPosition, t);
                }
                if (RightHand != null)
                {
                    Vector3 rightHandTargetPosition = rightHandOriginalPosition + RightHandRaiseAmount;
                    RightHand.localPosition = Vector3.Lerp(rightHandOriginalPosition, rightHandTargetPosition, t);
                }

                if (LeftHandWeaponSocket != null)
                    LeftHandWeaponSocket.localRotation = Quaternion.Slerp(leftHandSocketOriginalRotation, leftHandSocketOriginalRotation * Quaternion.Euler(LeftHandSocketRotationChange), t);
                if (RightHandWeaponSocket != null)
                    RightHandWeaponSocket.localRotation = Quaternion.Slerp(rightHandSocketOriginalRotation, rightHandSocketOriginalRotation * Quaternion.Euler(RightHandSocketRotationChange), t);
            }
            else if (elapsedTime < RaiseDuration + HoldDuration)
            {
                // Hands are held in the raised position
                if (LeftHand != null)
                    LeftHand.localPosition = leftHandOriginalPosition + LeftHandRaiseAmount;
                if (RightHand != null)
                    RightHand.localPosition = rightHandOriginalPosition + RightHandRaiseAmount;

                if (LeftHandWeaponSocket != null)
                    LeftHandWeaponSocket.localRotation = leftHandSocketOriginalRotation * Quaternion.Euler(LeftHandSocketRotationChange);
                if (RightHandWeaponSocket != null)
                    RightHandWeaponSocket.localRotation = rightHandSocketOriginalRotation * Quaternion.Euler(RightHandSocketRotationChange);
            }
            else if (elapsedTime < totalDuration)
            {
                float t = (elapsedTime - RaiseDuration - HoldDuration) / ReturnDuration;
                if (LeftHand != null)
                {
                    Vector3 leftHandTargetPosition = leftHandOriginalPosition + LeftHandRaiseAmount;
                    LeftHand.localPosition = Vector3.Lerp(leftHandTargetPosition, leftHandOriginalPosition, t);
                }
                if (RightHand != null)
                {
                    Vector3 rightHandTargetPosition = rightHandOriginalPosition + RightHandRaiseAmount;
                    RightHand.localPosition = Vector3.Lerp(rightHandTargetPosition, rightHandOriginalPosition, t);
                }

                if (LeftHandWeaponSocket != null)
                    LeftHandWeaponSocket.localRotation = Quaternion.Slerp(leftHandSocketOriginalRotation * Quaternion.Euler(LeftHandSocketRotationChange), leftHandSocketOriginalRotation, t);
                if (RightHandWeaponSocket != null)
                    RightHandWeaponSocket.localRotation = Quaternion.Slerp(rightHandSocketOriginalRotation * Quaternion.Euler(RightHandSocketRotationChange), rightHandSocketOriginalRotation, t);
            }
            else
            {
                if (LeftHand != null)
                    LeftHand.localPosition = leftHandOriginalPosition;
                if (RightHand != null)
                    RightHand.localPosition = rightHandOriginalPosition;

                if (LeftHandWeaponSocket != null)
                    LeftHandWeaponSocket.localRotation = leftHandSocketOriginalRotation;
                if (RightHandWeaponSocket != null)
                    RightHandWeaponSocket.localRotation = rightHandSocketOriginalRotation;

                isAnimating = false;
            }
        }
    }
}
