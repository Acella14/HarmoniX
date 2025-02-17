using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;

public class InitialCutsceneManager : MonoBehaviour
{
    [Header("Vignette Settings")]
    public Volume globalVolume;
    private VolumeProfile volumeProfile;
    private Vignette vignette;
    private Coroutine currentLerp;

    [Header("Camera Settings")]
    public List<CinemachineVirtualCamera> virtualCameras;
    private int currentCameraIndex = 0;

    [Header("Camera Shake")]
    public CinemachineShake cinemachineShakeScript;


    void Start()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            volumeProfile = globalVolume.profile;
            if (!volumeProfile.TryGet(out vignette))
            {
                Debug.LogError("No Vignette override found in the Volume Profile.");
            }
        }
        else
        {
            Debug.LogError("Global Volume or Volume Profile is null.");
        }

        SetCameraPriorities();
    }

    private void SetCameraPriorities()
    {
        for (int i = 0; i < virtualCameras.Count; i++)
        {
            virtualCameras[i].Priority = (i == 0) ? 10 : 0;
        }
    }

    public void SwitchToNextCamera()
    {
        if (virtualCameras.Count == 0) return;

        virtualCameras[currentCameraIndex].Priority = 0;

        currentCameraIndex++;
        if (currentCameraIndex >= virtualCameras.Count)
        {
            currentCameraIndex = 0;
        }

        virtualCameras[currentCameraIndex].Priority = 10;
    }

    public void LerpVignetteIntensity(string parameters)
    {
        if (vignette == null)
        {
            Debug.LogError("Vignette not initialized. Ensure the Volume Profile has a Vignette override.");
            return;
        }

        // Parse parameters (e.g., "0.5,2" -> targetIntensity = 0.5, duration = 2 seconds)
        string[] paramArray = parameters.Split(',');
        if (paramArray.Length != 2)
        {
            Debug.LogError("Invalid parameters for LerpVignetteIntensity. Expected format: 'targetIntensity,duration'.");
            return;
        }

        if (float.TryParse(paramArray[0], out float targetIntensity) && float.TryParse(paramArray[1], out float duration))
        {
            if (currentLerp != null) StopCoroutine(currentLerp);
            currentLerp = StartCoroutine(LerpIntensity(targetIntensity, duration));
        }
        else
        {
            Debug.LogError("Failed to parse parameters for LerpVignetteIntensity.");
        }
    }

    private IEnumerator LerpIntensity(float targetIntensity, float duration)
    {
        float startIntensity = vignette.intensity.value;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            vignette.intensity.Override(Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration));
            yield return null;
        }

        vignette.intensity.Override(targetIntensity);
    }

    public void ShakeCamera(string parameters)
    {
        string[] paramArray = parameters.Split(',');
        if (paramArray.Length != 2)
        {
            Debug.LogError("Invalid parameters for ShakeCam");
            return;
        }

        if (float.TryParse(paramArray[0], out float intensity) && float.TryParse(paramArray[1], out float time))
        {
            cinemachineShakeScript.ShakeCamera(intensity, time);
        }
        else
        {
            Debug.LogError("Failed to parse parameters for ShakeCam");
        }
    }
}
