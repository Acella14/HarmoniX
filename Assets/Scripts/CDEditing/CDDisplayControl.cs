using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CDDisplayControl : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public GameObject cdObject;

    private bool isSpinning = true;
    private bool isRotating = false;
    private float targetYRotation;
    private float smoothRotationSpeed = 10f;

    private void Update()
    {
        if (isSpinning)
        {
            SpinCD();
        }
    }

    private void SpinCD()
    {
        cdObject.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }

    public void ToggleSpin()
    {
        isSpinning = !isSpinning;
    }

    public void RotateToFrontFace()
    {
        if (!isRotating)
        {
            targetYRotation = 0f;
            StartCoroutine(RotateToTarget());
        }
    }

    public void RotateToBackFace()
    {
        if (!isRotating)
        {
            targetYRotation = 180f;
            StartCoroutine(RotateToTarget());
        }
    }

    private IEnumerator RotateToTarget()
    {
        isSpinning = false;
        isRotating = true;
        
        Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f);
        while (Quaternion.Angle(cdObject.transform.rotation, targetRotation) > 0.1f)
        {
            cdObject.transform.rotation = Quaternion.Slerp(cdObject.transform.rotation, targetRotation, Time.deltaTime * smoothRotationSpeed);
            yield return null;
        }

        cdObject.transform.rotation = targetRotation;
        isRotating = false;
    }
}
