using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CDAnimator : MonoBehaviour
{
    [System.Serializable]
    public class CDData
    {
        public GameObject cdObject;  // Store the entire GameObject instead of just the Transform
        public CanvasGroup frontCanvas;
        public CanvasGroup backCanvas;
        [HideInInspector] public Vector3 originalPosition;  // Stored original position
        [HideInInspector] public Quaternion originalRotation; // Stored original rotation
        [HideInInspector] public Vector3 originalScale; // Stored original scale
        [HideInInspector] public bool isFlipped; // Tracks if the CD is flipped
        [HideInInspector] public bool isHovered;
    }

    public List<CDData> cds = new List<CDData>(); // List of all CDs
    public SpriteRenderer cdPlaceholder;
    public Transform targetTransform; // Where CDs go when taken out

    public float zMoveDuration = 0.3f;  // Time to move in Z-axis first when moving out
    public float moveOutDuration = 1f; // Total time to fully move CD out
    public float returnDuration = 1f; // Total time to fully return CD
    public float returnZMoveDuration = 0.6f; // Time to move in Z-axis last when returning
    public float flipDuration = 0.6f; // Time to flip between front and back
    public float placeholderFadeDuration = 0.5f;
    public float canvasFadeDuration = 0.2f;

    public Vector3 targetScale = new Vector3(1f, 1f, 1f); // Scale when fully out
    public AnimationCurve positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Flip animation curve

    public AudioSource audioSource;
    public AudioClip pullOutSound;
    public AudioClip moveToFinalSound;
    public AudioClip cdFlipSound;

    public Vector3 hoverOffset = new Vector3(0.2f, 0, 0);
    public float hoverMoveSpeed = 5f;

    private CDData activeCD = null; // The currently active CD
    private CDData newCD = null; // The new CD that was clicked
    [HideInInspector] public bool isAnimating = false;
    private bool hasFadedPlaceholder = false;

    void Start()
    {
        foreach (CDData cd in cds)
        {
            cd.originalPosition = cd.cdObject.transform.position;
            cd.originalRotation = cd.cdObject.transform.rotation;
            cd.originalScale = cd.cdObject.transform.localScale;
            cd.isFlipped = false; // Ensure all CDs start facing the front
            cd.isHovered = false;

            if (cd.frontCanvas != null && cd.backCanvas != null)
            {
                cd.frontCanvas.alpha = 0;
                cd.backCanvas.alpha = 0;
                cd.frontCanvas.gameObject.SetActive(false);
                cd.backCanvas.gameObject.SetActive(false);
            }
        }
    }


    private IEnumerator FadeOutPlaceholder()
    {
        if (cdPlaceholder == null || hasFadedPlaceholder) yield break;

        hasFadedPlaceholder = true;
        Color startColor = cdPlaceholder.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0);
        float elapsedTime = 0f;

        while (elapsedTime < placeholderFadeDuration)
        {
            cdPlaceholder.color = Color.Lerp(startColor, targetColor, elapsedTime / placeholderFadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cdPlaceholder.color = targetColor;
    }

    void Update()
    {
        HandleHover();

        if (Input.GetMouseButtonDown(0)) // Detect left mouse click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                foreach (CDData cd in cds)
                {
                    if (hit.transform == cd.cdObject.transform)
                    {
                        if (!isAnimating)
                        {
                            if (activeCD == null | activeCD != cd) {
                                StartCoroutine(SwapCD(cd));
                                if (audioSource && pullOutSound)
                                    audioSource.PlayOneShot(pullOutSound);
                            }
                        }
                        return;
                    }
                }
            }
        }
    }

    void HandleHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        CDData hoveredCD = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            foreach (CDData cd in cds)
            {
                if (hit.transform == cd.cdObject.transform)
                {
                    hoveredCD = cd;
                    break;
                }
            }
        }

        foreach (CDData cd in cds)
        {
            if (cd != activeCD && !isAnimating)
            {
                Vector3 desiredPos = cd.originalPosition + (cd == hoveredCD ? hoverOffset : Vector3.zero);
                cd.cdObject.transform.position = Vector3.Lerp(cd.cdObject.transform.position, desiredPos, Time.deltaTime * hoverMoveSpeed);
                cd.isHovered = (cd == hoveredCD);
            }
        }
    }

    private IEnumerator SwapCD(CDData clickedCD)
    {
        isAnimating = true;

        if (activeCD == null)
        {
            activeCD = clickedCD;
            StartCoroutine(FadeOutPlaceholder());
            yield return StartCoroutine(MoveCDOut(activeCD.cdObject.transform, targetTransform.position, targetTransform.rotation, targetScale));
        }
        else if (activeCD != clickedCD)
        {
            newCD = clickedCD;

            // Fade out the current CD's active canvas before returning it
            CanvasGroup activeCanvas = activeCD.isFlipped ? activeCD.backCanvas : activeCD.frontCanvas;
            yield return StartCoroutine(FadeCanvas(activeCanvas, false));

            yield return StartCoroutine(ReturnCDWithOverlap(activeCD, newCD));
        }

        activeCD = clickedCD;

        // Activate and fade in the correct canvas
        if (activeCD.frontCanvas != null && activeCD.backCanvas != null)
        {
            activeCD.frontCanvas.gameObject.SetActive(true);
            activeCD.backCanvas.gameObject.SetActive(true);

            // Ensure the new CD is set to show the correct side (default: front)
            CanvasGroup newCanvas = activeCD.isFlipped ? activeCD.backCanvas : activeCD.frontCanvas;
            yield return StartCoroutine(FadeCanvas(newCanvas, true));
        }

        isAnimating = false;
    }



    private IEnumerator MoveCDOut(Transform cd, Vector3 finalPosition, Quaternion finalRotation, Vector3 finalScale)
    {
        if (audioSource && moveToFinalSound)
            audioSource.PlayOneShot(moveToFinalSound);

        Vector3 startPosition = cd.position;
        Quaternion startRotation = cd.rotation;
        Vector3 startScale = cd.localScale;

        // Phase 1: Move only in Z-axis
        float elapsedTime = 0f;
        Vector3 zOnlyTarget = new Vector3(cd.position.x, cd.position.y, finalPosition.z);

        while (elapsedTime < zMoveDuration)
        {
            float t = elapsedTime / zMoveDuration;
            cd.position = Vector3.Lerp(startPosition, zOnlyTarget, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cd.position = zOnlyTarget; // Ensure Z position is exact

        // Phase 2: Move fully to final position, rotation, and scale
        elapsedTime = 0f;
        Vector3 phase2StartPos = cd.position;
        Quaternion phase2StartRot = cd.rotation;
        Vector3 phase2StartScale = cd.localScale;

        while (elapsedTime < (moveOutDuration - zMoveDuration))
        {
            float t = elapsedTime / (moveOutDuration - zMoveDuration);
            float positionT = positionCurve.Evaluate(t);
            float rotationT = rotationCurve.Evaluate(t);
            float scaleT = scaleCurve.Evaluate(t);

            cd.position = Vector3.Lerp(phase2StartPos, finalPosition, positionT);
            cd.rotation = Quaternion.Slerp(phase2StartRot, finalRotation, rotationT);
            cd.localScale = Vector3.Lerp(phase2StartScale, finalScale, scaleT);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cd.position = finalPosition;
        cd.rotation = finalRotation;
        cd.localScale = finalScale;
    }

    private IEnumerator ReturnCDWithOverlap(CDData oldCD, CDData newCD)
    {
        Transform cdTransform = oldCD.cdObject.transform;
        Vector3 startPosition = cdTransform.position;
        Quaternion startRotation = cdTransform.rotation;
        Vector3 startScale = cdTransform.localScale;

        Vector3 originalPosition = oldCD.originalPosition;
        Quaternion originalRotation = oldCD.originalRotation;
        Vector3 originalScale = oldCD.originalScale;

        // Fade out the currently active canvas of the outgoing CD
        CanvasGroup activeCanvas = oldCD.isFlipped ? oldCD.backCanvas : oldCD.frontCanvas;
        yield return StartCoroutine(FadeCanvas(activeCanvas, false));

        // Reset the CD's flipped state
        oldCD.isFlipped = false;

        // Disable both front and back canvases to avoid incorrect visibility on reactivation
        if (oldCD.frontCanvas != null && oldCD.backCanvas != null)
        {
            oldCD.frontCanvas.gameObject.SetActive(false);
            oldCD.backCanvas.gameObject.SetActive(false);
        }

        // Phase 1: Move from target position to original position & rotation (except Z)
        float elapsedTime = 0f;
        Vector3 phase1TargetPos = new Vector3(originalPosition.x, originalPosition.y, cdTransform.position.z);

        while (elapsedTime < (returnDuration - returnZMoveDuration))
        {
            float t = elapsedTime / (returnDuration - returnZMoveDuration);
            float positionT = positionCurve.Evaluate(t);
            float rotationT = rotationCurve.Evaluate(t);
            float scaleT = scaleCurve.Evaluate(t);

            cdTransform.position = Vector3.Lerp(startPosition, phase1TargetPos, positionT);
            cdTransform.rotation = Quaternion.Slerp(startRotation, originalRotation, rotationT);
            cdTransform.localScale = Vector3.Lerp(startScale, originalScale, scaleT);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cdTransform.position = phase1TargetPos;
        cdTransform.rotation = originalRotation;
        cdTransform.localScale = originalScale;

        // Start moving the new CD out *before* the old CD fully returns!
        StartCoroutine(MoveCDOut(newCD.cdObject.transform, targetTransform.position, targetTransform.rotation, targetScale));

        // Phase 2: Move back in Z-axis
        elapsedTime = 0f;

        while (elapsedTime < returnZMoveDuration)
        {
            float t = elapsedTime / returnZMoveDuration;
            cdTransform.position = Vector3.Lerp(phase1TargetPos, originalPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cdTransform.position = originalPosition;
    }


    public void FlipActiveCD()
    {
        if (activeCD == null || isAnimating) return;

        Animator cdAnimator = activeCD.cdObject.GetComponent<Animator>();
        if (cdAnimator == null) 
        {
            Debug.Log("No animator component");
            return;
        }

        if (audioSource && cdFlipSound)
            audioSource.PlayOneShot(cdFlipSound);

        Vector3 currentPosition = activeCD.cdObject.transform.position;
        Vector3 targetPosition = targetTransform.position;
        float positionTolerance = 0.01f;

        if (Vector3.Distance(currentPosition, targetPosition) > positionTolerance)
        {
            Debug.Log("CD is not at target position yet, cannot flip.");
            return; // Prevent flipping if the CD isn't close enough
        }

        // Determine which canvas is currently active
        CanvasGroup currentCanvas = activeCD.isFlipped ? activeCD.backCanvas : activeCD.frontCanvas;
        CanvasGroup newCanvas = activeCD.isFlipped ? activeCD.frontCanvas : activeCD.backCanvas;

        StartCoroutine(FlipCDAnimation(cdAnimator, currentCanvas, newCanvas));
    }

    private IEnumerator FlipCDAnimation(Animator cdAnimator, CanvasGroup currentCanvas, CanvasGroup newCanvas)
    {

        // Fade out current face's canvas
        yield return StartCoroutine(FadeCanvas(currentCanvas, false));

        // Trigger the animation to flip the CD
        if (activeCD.isFlipped)
        {
            cdAnimator.SetTrigger("FlipBackwardTrigger");
        }
        else
        {
            cdAnimator.SetTrigger("FlipForwardTrigger");
        }

        // Update the flipped state
        activeCD.isFlipped = !activeCD.isFlipped;

        // Fade in the new face's canvas
        yield return StartCoroutine(FadeCanvas(newCanvas, true));

    }

    private IEnumerator FadeCanvas(CanvasGroup canvasGroup, bool fadeIn)
    {
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsedTime = 0f;

        if (fadeIn) canvasGroup.gameObject.SetActive(true);

        while (elapsedTime < canvasFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / canvasFadeDuration);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        if (!fadeIn) canvasGroup.gameObject.SetActive(false);
    }

}
