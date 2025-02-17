using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    public CinematicCameraSequence sequenceManager;

    public void TransitionToNextCamera()
    {
        if (sequenceManager != null)
        {
            sequenceManager.TransitionToNextCamera();
        }
    }

    public void TransitionToFinalCamera()
    {
        if (sequenceManager != null)
        {
            sequenceManager.EndCinematicAndTransitionToPlayerCamera();
        }
    }
}
