using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CinematicCameraSequence : MonoBehaviour
{
    [System.Serializable]
    public class CameraSequence
    {
        public CinemachineVirtualCamera virtualCamera;
        public Animator animationController;
    }

    [Header("Cameras in Sequence")]
    public List<CameraSequence> cameraSequences;
    public CinemachineVirtualCamera mainPlayerCamera;
    public PlayerInput playerInput;
    public List<MonoBehaviour> playerScriptsToDisable;

    private int currentSequenceIndex = 0;

    void Start()
    {
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        foreach (var script in playerScriptsToDisable)
        {
            script.enabled = false;
        }

        foreach (var sequence in cameraSequences)
        {
            if (sequence.animationController != null)
            {
                sequence.animationController.enabled = false;
            }
        }

        if (cameraSequences.Count > 0)
        {
            PlayCurrentCameraAnimation();
        }
        else
        {
            Debug.LogWarning("No cameras assigned in the sequence!");
        }
    }

    private void PlayCurrentCameraAnimation()
    {
        if (currentSequenceIndex < cameraSequences.Count)
        {
            var sequence = cameraSequences[currentSequenceIndex];

            sequence.virtualCamera.Priority = 10;

            if (sequence.animationController != null)
            {
                sequence.animationController.enabled = true;
                sequence.animationController.Play(0);
            }

            Debug.Log($"Started animation for camera {currentSequenceIndex}.");
        }
    }

    public void TransitionToNextCamera()
    {
        if (currentSequenceIndex < cameraSequences.Count - 1)
        {
            var currentSequence = cameraSequences[currentSequenceIndex];
            currentSequence.virtualCamera.Priority = 0;

            currentSequenceIndex++;

            PlayCurrentCameraAnimation();
        }
        else
        {
            Debug.LogWarning("TransitionToNextCamera called on the final camera. Use EndCinematicAndTransitionToPlayerCamera instead.");
        }
    }

    public void EndCinematicAndTransitionToPlayerCamera()
    {
        if (currentSequenceIndex < cameraSequences.Count)
        {
            var lastSequence = cameraSequences[currentSequenceIndex];
            lastSequence.virtualCamera.Priority = 0;
        }

        // Activate the main player camera
        mainPlayerCamera.Priority = 10;

        // Force Cinemachine to recognize the main player camera as active
        mainPlayerCamera.UpdateCameraState(Vector3.zero, Time.deltaTime);

        EndCinematicSequence();
    }

    private void EndCinematicSequence()
    {
        // Reactivate player input and other controls
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }

        foreach (var script in playerScriptsToDisable)
        {
            script.enabled = true;
        }

        Debug.Log("Cinematic sequence completed. Player controls enabled.");
    }
}
