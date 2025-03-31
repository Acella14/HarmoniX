using UnityEngine;

public class CDAnimationEventRelay : MonoBehaviour
{
    public CDAnimator cdAnimator;

    public void OnFlipStart()
    {
        cdAnimator.isAnimating = true;
    }

    public void OnFlipEnd()
    {
        cdAnimator.isAnimating = false;
    }
}

